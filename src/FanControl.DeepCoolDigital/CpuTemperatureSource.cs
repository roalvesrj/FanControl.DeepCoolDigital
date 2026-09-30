using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using FanControl.DeepCoolDigital.Core;
using LibreHardwareMonitor.Hardware;

namespace FanControl.DeepCoolDigital
{
    /// <summary>
    /// Reads the CPU temperature from LibreHardwareMonitor, reusing the sensor driver FanControl already loaded.
    /// </summary>
    /// <remarks>
    /// A second CPU-only <see cref="Computer"/> instance is created in-process; the kernel driver is
    /// reference counted by LibreHardwareMonitor, so this is safe alongside FanControl's own instance.
    /// Sensor selection is delegated to <see cref="TemperatureSensorSelector"/>.
    /// </remarks>
    internal sealed class CpuTemperatureSource : IDisposable
    {
        private readonly Computer _computer;
        private readonly IReadOnlyList<string> _preferredNames;
        private bool _failed;
        private bool _selectionLogged;

        /// <summary>
        /// Initializes a new instance of the <see cref="CpuTemperatureSource"/> class.
        /// </summary>
        /// <param name="preferredNames">The preferred sensor names, in priority order; <see langword="null" /> uses the defaults.</param>
        public CpuTemperatureSource(IReadOnlyList<string> preferredNames)
        {
            _preferredNames = preferredNames ?? PluginConfig.DefaultPreferredTemperatureSensors;
            _computer = new Computer { IsCpuEnabled = true };
            _computer.Open();
        }

        /// <summary>
        /// Reads the selected CPU temperature.
        /// </summary>
        /// <returns>The temperature in degrees Celsius, or <see langword="null" /> when no valid reading is available.</returns>
        public float? Read()
        {
            if (_failed)
            {
                return null;
            }

            try
            {
                _computer.Accept(new UpdateVisitor());

                List<SensorSample> samples = CollectSamples();
                SensorSample? selected = TemperatureSensorSelector.SelectSample(samples, _preferredNames);

                if (!_selectionLogged && selected.HasValue)
                {
                    _selectionLogged = true;
                    Log.Event($"Temperature source: \"{selected.Value.Name}\"");
                    Log.Verbose("Temperature candidates: " + string.Join(
                        ", ",
                        samples.Select(sample => sample.Name + "=" + sample.Value.ToString("0.#", CultureInfo.InvariantCulture))));
                    Log.Verbose("Preferred order: " + string.Join(" | ", _preferredNames));
                }

                return selected?.Value;
            }
            catch (Exception ex)
            {
                Log.Event("CPU temperature read failed: " + ex.Message);
                _failed = true;
                return null;
            }
        }

        /// <summary>
        /// Releases the LibreHardwareMonitor instance.
        /// </summary>
        public void Dispose()
        {
            try
            {
                _computer.Close();
            }
            catch
            {
            }
        }

        private List<SensorSample> CollectSamples()
        {
            var samples = new List<SensorSample>();

            foreach (IHardware hardware in _computer.Hardware)
            {
                if (hardware.HardwareType != HardwareType.Cpu)
                {
                    continue;
                }

                foreach (ISensor sensor in hardware.Sensors)
                {
                    if (sensor.SensorType != SensorType.Temperature || !sensor.Value.HasValue)
                    {
                        continue;
                    }

                    samples.Add(new SensorSample(sensor.Name, sensor.Value.Value));
                }
            }

            return samples;
        }
    }

    /// <summary>
    /// Updates every hardware node of a LibreHardwareMonitor computer tree.
    /// </summary>
    internal sealed class UpdateVisitor : IVisitor
    {
        /// <inheritdoc />
        public void VisitComputer(IComputer computer)
        {
            computer.Traverse(this);
        }

        /// <inheritdoc />
        public void VisitHardware(IHardware hardware)
        {
            hardware.Update();
        }

        /// <inheritdoc />
        public void VisitSensor(ISensor sensor)
        {
        }

        /// <inheritdoc />
        public void VisitParameter(IParameter parameter)
        {
        }
    }
}
