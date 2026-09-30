using System;
using System.Collections.Generic;

namespace FanControl.DeepCoolDigital.Core
{
    /// <summary>
    /// Picks the CPU temperature sensor to display from a list of candidates.
    /// </summary>
    /// <remarks>
    /// Sensors whose names match one of the preferred names are selected first, in preference order.
    /// When no preferred sensor is present, the highest valid reading is used. Invalid readings
    /// (not-a-number, zero or negative) are ignored.
    /// </remarks>
    public static class TemperatureSensorSelector
    {
        /// <summary>
        /// Selects the temperature to display.
        /// </summary>
        /// <param name="samples">The available sensor readings.</param>
        /// <param name="preferredNames">The preferred sensor names, in priority order.</param>
        /// <returns>The selected temperature in degrees Celsius, or <see langword="null" /> when no valid reading is available.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="samples"/> is <see langword="null" />.</exception>
        public static float? Select(IReadOnlyList<SensorSample> samples, IReadOnlyList<string> preferredNames)
        {
            return SelectSample(samples, preferredNames)?.Value;
        }

        /// <summary>
        /// Selects the sensor sample to display, including its name.
        /// </summary>
        /// <param name="samples">The available sensor readings.</param>
        /// <param name="preferredNames">The preferred sensor names, in priority order.</param>
        /// <returns>The selected <see cref="SensorSample"/>, or <see langword="null" /> when no valid reading is available.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="samples"/> is <see langword="null" />.</exception>
        public static SensorSample? SelectSample(IReadOnlyList<SensorSample> samples, IReadOnlyList<string> preferredNames)
        {
            if (samples == null)
            {
                throw new ArgumentNullException(nameof(samples));
            }

            if (preferredNames != null)
            {
                foreach (string preferredName in preferredNames)
                {
                    if (string.IsNullOrWhiteSpace(preferredName))
                    {
                        continue;
                    }

                    foreach (SensorSample sample in samples)
                    {
                        if (string.Equals(sample.Name, preferredName, StringComparison.OrdinalIgnoreCase)
                            && IsValid(sample.Value))
                        {
                            return sample;
                        }
                    }
                }
            }

            SensorSample? maximum = null;

            foreach (SensorSample sample in samples)
            {
                if (!IsValid(sample.Value))
                {
                    continue;
                }

                if (!maximum.HasValue || sample.Value > maximum.Value.Value)
                {
                    maximum = sample;
                }
            }

            return maximum;
        }

        private static bool IsValid(float value)
        {
            return !float.IsNaN(value) && value > 0f;
        }
    }
}
