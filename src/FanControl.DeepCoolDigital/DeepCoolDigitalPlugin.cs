using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using FanControl.DeepCoolDigital.Core;
using FanControl.Plugins;
using HidSharp;

namespace FanControl.DeepCoolDigital
{
    /// <summary>
    /// FanControl plugin that drives the status displays of DeepCool DIGITAL coolers with live CPU data.
    /// </summary>
    /// <remarks>
    /// The plugin reads the CPU temperature through LibreHardwareMonitor and the CPU usage through the
    /// Windows kernel once per cycle, then streams the configured value to every supported display found
    /// on the system. All errors are contained: the plugin degrades gracefully and never lets an exception
    /// escape into FanControl.
    /// </remarks>
    public class DeepCoolDigitalPlugin : IPlugin2
    {
        private readonly PluginConfig _config = PluginConfig.Load(ConfigFilePath);
        private readonly List<DeepCoolDisplaySession> _sessions = new List<DeepCoolDisplaySession>();
        private readonly List<DeepCoolDisplaySensor> _sensors = new List<DeepCoolDisplaySensor>();
        private CpuTemperatureSource _temperatureSource;
        private CpuUsage _cpuUsage;
        private IReadOnlyList<string> _temperatureSensorNames;
        private int _configuredVendorId;
        private int _configuredProductId;

        /// <summary>
        /// Gets the name shown in the FanControl UI.
        /// </summary>
        /// <value>The plugin name.</value>
        public string Name => "DeepCool DIGITAL Display";

        /// <summary>
        /// Gets the full path of the configuration file located next to the plugin dll.
        /// </summary>
        /// <value>The absolute path of <c>DeepCoolDigital.ini</c>.</value>
        public static string ConfigFilePath
        {
            get
            {
                string directory = Path.GetDirectoryName(typeof(DeepCoolDigitalPlugin).Assembly.Location) ?? string.Empty;
                return Path.Combine(directory, "DeepCoolDigital.ini");
            }
        }

        /// <inheritdoc />
        public void Initialize()
        {
            try
            {
                InitializeComponents();
            }
            catch (Exception ex)
            {
                Log.Event("Initialize failed: " + ex);
            }
        }

        /// <inheritdoc />
        public void Load(IPluginSensorsContainer container)
        {
            try
            {
                if (container == null)
                {
                    Log.Event("Load called without a sensor container; skipping sensor registration.");
                    return;
                }

                if (_sensors.Count > 0)
                {
                    return;
                }

                foreach (DeepCoolDisplaySession session in _sessions)
                {
                    var sensor = new DeepCoolDisplaySensor(session.SensorId, session.SensorName);
                    session.Sensor = sensor;
                    _sensors.Add(sensor);
                    container.TempSensors.Add(sensor);
                }
            }
            catch (Exception ex)
            {
                Log.Event("Load failed: " + ex);
            }
        }

        /// <inheritdoc />
        public void Update()
        {
            try
            {
                UpdateCore();
            }
            catch (Exception ex)
            {
                Log.Event("Update failed: " + ex);
            }
        }

        /// <inheritdoc />
        public void Close()
        {
            try
            {
                Log.Event("Close");

                foreach (DeepCoolDisplaySensor sensor in _sensors)
                {
                    sensor.Value = null;
                }

                _sensors.Clear();

                foreach (DeepCoolDisplaySession session in _sessions)
                {
                    session.Sensor = null;
                    session.Dispose();
                }

                _sessions.Clear();

                _temperatureSource?.Dispose();
                _temperatureSource = null;

                _cpuUsage = null;
            }
            catch (Exception ex)
            {
                Log.Event("Close failed: " + ex);
            }
        }

        private static string PluginVersion
        {
            get
            {
                try
                {
                    return typeof(DeepCoolDigitalPlugin).Assembly.GetName().Version?.ToString() ?? "unknown";
                }
                catch
                {
                    return "unknown";
                }
            }
        }

        private static string HostVersion
        {
            get
            {
                try
                {
                    return Assembly.GetEntryAssembly()?.GetName()?.Version?.ToString() ?? "unknown";
                }
                catch
                {
                    return "unknown";
                }
            }
        }

        private void InitializeComponents()
        {
            Log.Init(_config);
            Log.Event($"Plugin v{PluginVersion} starting (FanControl v{HostVersion}, config={_config.FilePath})");

            try
            {
                _temperatureSource = new CpuTemperatureSource(_config.PreferredTemperatureSensors);
            }
            catch (Exception ex)
            {
                Log.Event("CPU temperature source unavailable: " + ex.Message);
            }

            _cpuUsage = new CpuUsage();
            _temperatureSensorNames = _config.PreferredTemperatureSensors;
            _configuredVendorId = _config.VendorId;
            _configuredProductId = _config.ProductId;

            CreateSessions();
        }

        private void CreateSessions()
        {
            _sessions.Clear();

            _sessions.Add(new DeepCoolDisplaySession(
                _config.VendorId,
                _config.ProductId,
                _config.ForDevice(_config.VendorId, _config.ProductId),
                isPrimary: true));

            try
            {
                foreach (HidDevice device in DeviceList.Local.GetHidDevices())
                {
                    if (DeviceRegistry.Find(device.VendorID, device.ProductID) == null)
                    {
                        continue;
                    }

                    bool alreadyTracked = _sessions.Any(
                        session => session.VendorId == device.VendorID && session.ProductId == device.ProductID);

                    if (alreadyTracked)
                    {
                        continue;
                    }

                    _sessions.Add(new DeepCoolDisplaySession(
                        device.VendorID,
                        device.ProductID,
                        _config.ForDevice(device.VendorID, device.ProductID),
                        isPrimary: false));
                }
            }
            catch (Exception ex)
            {
                Log.Event("Device discovery failed: " + ex.Message);
            }

            Log.Event($"Tracking {_sessions.Count} display session(s).");
        }

        private void UpdateCore()
        {
            if (_config.TryReload())
            {
                Log.Init(_config);
                Log.Event($"Config reloaded: mode={_config.Mode}, autoSwitchSeconds={_config.AutoSwitchSeconds}, alarmTemperature={_config.AlarmTemperature}, alarmEnabled={_config.AlarmEnabled}, fahrenheit={_config.Fahrenheit}");

                if (_config.VendorId != _configuredVendorId || _config.ProductId != _configuredProductId)
                {
                    Log.Event("vendorId/productId changes require a FanControl restart; keeping the current devices.");
                }

                if (_temperatureSensorNames == null || !_config.PreferredTemperatureSensors.SequenceEqual(_temperatureSensorNames))
                {
                    _temperatureSensorNames = _config.PreferredTemperatureSensors;
                    _temperatureSource?.Dispose();

                    try
                    {
                        _temperatureSource = new CpuTemperatureSource(_temperatureSensorNames);
                    }
                    catch (Exception ex)
                    {
                        Log.Event("CPU temperature source unavailable after reload: " + ex.Message);
                        _temperatureSource = null;
                    }
                }

                foreach (DeepCoolDisplaySession session in _sessions)
                {
                    session.ApplySettings(_config.ForDevice(session.VendorId, session.ProductId));
                }
            }

            float? temperature = _temperatureSource?.Read();

            if (temperature == null)
            {
                foreach (DeepCoolDisplaySensor sensor in _sensors)
                {
                    sensor.Value = null;
                }

                return;
            }

            float usage = _cpuUsage.Read();

            foreach (DeepCoolDisplaySession session in _sessions)
            {
                session.Update(temperature.Value, usage);
            }
        }
    }
}
