using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using FanControl.DeepCoolDigital.Core;
using FanControl.Plugins;

namespace FanControl.DeepCoolDigital
{
    /// <summary>
    /// FanControl plugin that drives the status display of DeepCool DIGITAL air coolers with live CPU data.
    /// </summary>
    /// <remarks>
    /// The plugin reads the CPU temperature through LibreHardwareMonitor and the CPU usage through the
    /// Windows kernel, then streams the selected value to the cooler display. All errors are contained:
    /// the plugin degrades gracefully and never lets an exception escape into FanControl.
    /// </remarks>
    public class DeepCoolDigitalPlugin : IPlugin2
    {
        private readonly PluginConfig _config = PluginConfig.Load(ConfigFilePath);
        private CpuTemperatureSource _temperatureSource;
        private CpuUsage _cpuUsage;
        private DeepCoolDisplayDevice _display;
        private DeepCoolDisplaySensor _temperatureSensor;
        private IReadOnlyList<string> _temperatureSensorNames;
        private int _vendorId;
        private int _productId;
        private int _lastModeSwitch;
        private bool _showUsage;

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

                if (_temperatureSensor != null)
                {
                    return;
                }

                _temperatureSensor = new DeepCoolDisplaySensor();
                container.TempSensors.Add(_temperatureSensor);
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

                if (_temperatureSensor != null)
                {
                    _temperatureSensor.Value = null;
                    _temperatureSensor = null;
                }

                _display?.Dispose();
                _display = null;

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
            _display = new DeepCoolDisplayDevice(_config);
            _vendorId = _config.VendorId;
            _productId = _config.ProductId;
            _temperatureSensorNames = _config.PreferredTemperatureSensors;
            _lastModeSwitch = Environment.TickCount;
            _showUsage = _config.Mode == DisplayMode.Usage;
        }

        private void UpdateCore()
        {
            if (_display == null)
            {
                return;
            }

            if (_config.TryReload())
            {
                Log.Init(_config);
                Log.Event($"Config reloaded: mode={_config.Mode}, autoSwitchSeconds={_config.AutoSwitchSeconds}, alarmTemperature={_config.AlarmTemperature}");
                _lastModeSwitch = Environment.TickCount;
                _showUsage = _config.Mode == DisplayMode.Usage;

                if (_config.VendorId != _vendorId || _config.ProductId != _productId)
                {
                    _vendorId = _config.VendorId;
                    _productId = _config.ProductId;
                    _display?.Dispose();
                    _display = new DeepCoolDisplayDevice(_config);
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
            }

            float? temperature = _temperatureSource?.Read();
            if (_temperatureSensor != null)
            {
                _temperatureSensor.Value = temperature;
            }

            if (temperature == null)
            {
                return;
            }

            float usage = _cpuUsage.Read();

            if (_config.Mode == DisplayMode.Auto)
            {
                if (unchecked(Environment.TickCount - _lastModeSwitch) >= _config.AutoSwitchSeconds * 1000)
                {
                    _showUsage = !_showUsage;
                    _lastModeSwitch = Environment.TickCount;
                }
            }
            else
            {
                _showUsage = _config.Mode == DisplayMode.Usage;
            }

            if (_showUsage)
            {
                _display.SendUsage(usage, temperature.Value);
            }
            else
            {
                _display.SendTemperature(temperature.Value);
            }
        }
    }
}
