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
    /// CPU temperature and usage are read from FanControl's own sensors through its IPC channel when
    /// available (avoiding a second LibreHardwareMonitor instance), falling back to a local
    /// LibreHardwareMonitor instance and Windows kernel counters. The selected readings are streamed to
    /// every supported display found on the system. All errors are contained: the plugin degrades
    /// gracefully and never lets an exception escape into FanControl.
    /// </remarks>
    public class DeepCoolDigitalPlugin : IPlugin2
    {
        private const int IpcRetryDelayMilliseconds = 30000;
        private const int RetireLocalSourceAfterSuccesses = 30;

        private readonly PluginConfig _config = PluginConfig.Load(ConfigFilePath);
        private readonly List<DeepCoolDisplaySession> _sessions = new List<DeepCoolDisplaySession>();
        private readonly List<DeepCoolDisplaySensor> _sensors = new List<DeepCoolDisplaySensor>();
        private SensorSourceManager _sensorManager;
        private FanControlIpcSource _ipcSource;
        private LocalSensorSource _localSource;
        private IReadOnlyList<string> _temperatureSensorNames;
        private SensorSource _configuredSource;
        private string _configuredUsageSensor;
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

                _sensorManager = null;

                _ipcSource?.Dispose();
                _ipcSource = null;

                _localSource?.Dispose();
                _localSource = null;
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

            _temperatureSensorNames = _config.PreferredTemperatureSensors;
            _configuredVendorId = _config.VendorId;
            _configuredProductId = _config.ProductId;
            _configuredSource = _config.Source;
            _configuredUsageSensor = _config.UsageSensor;

            ConfigureSensorSources();

            CreateSessions();
        }

        private void ConfigureSensorSources()
        {
            _sensorManager = null;

            _ipcSource?.Dispose();
            _ipcSource = null;

            _localSource?.Dispose();
            _localSource = null;

            _ipcSource = _config.Source == SensorSource.Local
                ? null
                : new FanControlIpcSource(_config.PreferredTemperatureSensors, _config.UsageSensor);

            _localSource = _config.Source == SensorSource.FanControl
                ? null
                : new LocalSensorSource(_config.PreferredTemperatureSensors);

            _sensorManager = new SensorSourceManager(
                _config.Source,
                _ipcSource,
                _localSource,
                new RetryBackoff(IpcRetryDelayMilliseconds),
                RetireLocalSourceAfterSuccesses,
                Log.Event,
                description => Log.Event("Sensor source: " + description),
                RetireLocalSource);

            if (_config.Source != SensorSource.Local)
            {
                string mode = _config.Source == SensorSource.FanControl
                    ? "FanControl IPC only"
                    : "auto (FanControl IPC with local fallback)";
                Log.Event("Sensor source mode: " + mode);
            }
        }

        private void RetireLocalSource()
        {
            if (_localSource == null)
            {
                return;
            }

            _localSource.Dispose();
            Log.Event("Local sensor source released; FanControl IPC is healthy.");
        }

        private void CreateSessions()
        {
            foreach (DeepCoolDisplaySession session in _sessions)
            {
                session.Sensor = null;
                session.Dispose();
            }

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
                Log.Event($"Config reloaded: mode={_config.Mode}, autoSwitchSeconds={_config.AutoSwitchSeconds}, alarmTemperature={_config.AlarmTemperature}, alarmEnabled={_config.AlarmEnabled}, fahrenheit={_config.Fahrenheit}, source={_config.Source}");

                if (_config.VendorId != _configuredVendorId || _config.ProductId != _configuredProductId)
                {
                    Log.Event("vendorId/productId changes require a FanControl restart; keeping the current devices.");
                    _configuredVendorId = _config.VendorId;
                    _configuredProductId = _config.ProductId;
                }

                ApplyReloadedSensorConfiguration();

                foreach (DeepCoolDisplaySession session in _sessions)
                {
                    session.ApplySettings(_config.ForDevice(session.VendorId, session.ProductId));
                }
            }

            if (_sensorManager == null
                || !_sensorManager.TryRead(Environment.TickCount, out float temperature, out float usage))
            {
                foreach (DeepCoolDisplaySensor sensor in _sensors)
                {
                    sensor.Value = null;
                }

                return;
            }

            foreach (DeepCoolDisplaySession session in _sessions)
            {
                session.Update(temperature, usage);
            }
        }

        private void ApplyReloadedSensorConfiguration()
        {
            bool changed =
                _temperatureSensorNames == null
                || !_config.PreferredTemperatureSensors.SequenceEqual(_temperatureSensorNames)
                || !string.Equals(_config.UsageSensor, _configuredUsageSensor, StringComparison.OrdinalIgnoreCase)
                || _config.Source != _configuredSource;

            if (!changed)
            {
                return;
            }

            _temperatureSensorNames = _config.PreferredTemperatureSensors;
            _configuredUsageSensor = _config.UsageSensor;
            _configuredSource = _config.Source;

            ConfigureSensorSources();
        }
    }
}
