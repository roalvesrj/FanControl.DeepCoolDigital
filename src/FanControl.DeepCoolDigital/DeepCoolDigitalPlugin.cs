using System;
using FanControl.Plugins;

namespace FanControl.DeepCoolDigital
{
    public class DeepCoolDigitalPlugin : IPlugin2
    {
        private readonly PluginConfig _config = PluginConfig.Load();
        private CpuTemperatureSource _temperatureSource;
        private CpuUsage _cpuUsage;
        private DeepCoolDisplayDevice _display;
        private DeepCoolDisplaySensor _temperatureSensor;
        private int _lastModeSwitch;
        private bool _showUsage;

        public string Name => "DeepCool DIGITAL Display";

        public void Initialize()
        {
            Log.Init(_config);
            Log.Write($"Initialize: mode={_config.Mode}, autoSwitchSeconds={_config.AutoSwitchSeconds}, alarmTemperature={_config.AlarmTemperature}");

            try
            {
                _temperatureSource = new CpuTemperatureSource();
            }
            catch (Exception ex)
            {
                Log.Write("Failed to initialize CPU temperature source: " + ex);
            }

            _cpuUsage = new CpuUsage();
            _display = new DeepCoolDisplayDevice(_config);
            _lastModeSwitch = Environment.TickCount;
            _showUsage = _config.Mode == DisplayMode.Usage;
        }

        public void Load(IPluginSensorsContainer container)
        {
            if (_temperatureSensor != null) return;

            _temperatureSensor = new DeepCoolDisplaySensor();
            container.TempSensors.Add(_temperatureSensor);
        }

        public void Update()
        {
            if (_temperatureSource == null || _display == null) return;

            if (_config.TryReload())
            {
                Log.Init(_config);
                Log.Write($"Config reloaded: mode={_config.Mode}, autoSwitchSeconds={_config.AutoSwitchSeconds}, alarmTemperature={_config.AlarmTemperature}");
                _lastModeSwitch = Environment.TickCount;
                _showUsage = _config.Mode == DisplayMode.Usage;
            }

            float? temperature = _temperatureSource.Read();
            if (_temperatureSensor != null)
            {
                _temperatureSensor.Value = temperature;
            }

            if (temperature == null) return;

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

        public void Close()
        {
            Log.Write("Close");

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
    }
}
