using System;
using System.Collections.Generic;
using FanControl.DeepCoolDigital.Core;

namespace FanControl.DeepCoolDigital
{
    /// <summary>
    /// Reads CPU temperature and usage from a local LibreHardwareMonitor instance and the Windows kernel.
    /// </summary>
    /// <remarks>
    /// The LibreHardwareMonitor instance is created lazily on the first read attempt so it never exists
    /// while FanControl's IPC source is healthy, and it is released again through <see cref="Dispose"/>
    /// when the IPC has proven stable. Creation failures are latched to avoid retrying a broken driver
    /// every cycle.
    /// </remarks>
    internal sealed class LocalSensorSource : ISensorSource, IDisposable
    {
        private readonly IReadOnlyList<string> _preferredTemperatureSensors;
        private readonly CpuUsage _cpuUsage = new CpuUsage();
        private CpuTemperatureSource _temperatureSource;
        private bool _failed;

        /// <summary>
        /// Initializes a new instance of the <see cref="LocalSensorSource"/> class.
        /// </summary>
        /// <param name="preferredTemperatureSensors">The preferred CPU temperature sensor names, in priority order.</param>
        public LocalSensorSource(IReadOnlyList<string> preferredTemperatureSensors)
        {
            _preferredTemperatureSensors = preferredTemperatureSensors;
        }

        /// <inheritdoc />
        public bool TryRead(out float temperatureCelsius, out float usage)
        {
            temperatureCelsius = 0f;
            usage = 0f;

            if (_failed)
            {
                return false;
            }

            try
            {
                if (_temperatureSource == null)
                {
                    _temperatureSource = new CpuTemperatureSource(_preferredTemperatureSensors);
                }

                float? value = _temperatureSource.Read();

                if (value == null)
                {
                    return false;
                }

                temperatureCelsius = value.Value;
                usage = _cpuUsage.Read();
                return true;
            }
            catch (Exception ex)
            {
                _failed = true;
                Log.Event("Local sensor source unavailable: " + ex.Message);
                return false;
            }
        }

        /// <inheritdoc />
        public void Dispose()
        {
            _temperatureSource?.Dispose();
            _temperatureSource = null;
        }
    }
}
