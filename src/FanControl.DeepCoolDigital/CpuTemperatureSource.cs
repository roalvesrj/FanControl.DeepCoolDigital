using System;
using LibreHardwareMonitor.Hardware;

namespace FanControl.DeepCoolDigital
{
    internal sealed class CpuTemperatureSource : IDisposable
    {
        private static readonly string[] PreferredSensorNames =
        {
            "CPU Package",
            "Core (Tctl/Tdie)",
            "Core (Tctl)",
            "Core (Tdie)",
            "CPU (Tctl/Tdie)",
            "Core Average"
        };

        private readonly Computer _computer;
        private bool _failed;

        public CpuTemperatureSource()
        {
            _computer = new Computer { IsCpuEnabled = true };
            _computer.Open();
        }

        public float? Read()
        {
            if (_failed) return null;

            try
            {
                _computer.Accept(new UpdateVisitor());

                foreach (string preferredName in PreferredSensorNames)
                {
                    float? value = FindByName(preferredName);
                    if (value.HasValue) return value;
                }

                return FindMax();
            }
            catch (Exception ex)
            {
                Log.Write("CPU temperature read failed: " + ex.Message);
                _failed = true;
                return null;
            }
        }

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

        private float? FindByName(string name)
        {
            foreach (IHardware hardware in _computer.Hardware)
            {
                if (hardware.HardwareType != HardwareType.Cpu) continue;

                foreach (ISensor sensor in hardware.Sensors)
                {
                    if (sensor.SensorType != SensorType.Temperature) continue;
                    if (!sensor.Value.HasValue) continue;
                    if (!string.Equals(sensor.Name, name, StringComparison.OrdinalIgnoreCase)) continue;

                    return sensor.Value.Value;
                }
            }

            return null;
        }

        private float? FindMax()
        {
            float? max = null;

            foreach (IHardware hardware in _computer.Hardware)
            {
                if (hardware.HardwareType != HardwareType.Cpu) continue;

                foreach (ISensor sensor in hardware.Sensors)
                {
                    if (sensor.SensorType != SensorType.Temperature) continue;
                    if (!sensor.Value.HasValue) continue;

                    float value = sensor.Value.Value;
                    if (value <= 0f) continue;

                    if (!max.HasValue || value > max.Value)
                    {
                        max = value;
                    }
                }
            }

            return max;
        }
    }

    internal sealed class UpdateVisitor : IVisitor
    {
        public void VisitComputer(IComputer computer)
        {
            computer.Traverse(this);
        }

        public void VisitHardware(IHardware hardware)
        {
            hardware.Update();
        }

        public void VisitSensor(ISensor sensor)
        {
        }

        public void VisitParameter(IParameter parameter)
        {
        }
    }
}
