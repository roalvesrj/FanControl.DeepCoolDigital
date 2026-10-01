using System;
using System.Collections.Generic;

namespace FanControl.DeepCoolDigital.Core
{
    /// <summary>
    /// Picks the CPU usage sensor to display from a list of candidates.
    /// </summary>
    /// <remarks>
    /// The configured name or identifier is matched first (case-insensitive), then the well-known
    /// "CPU Total" sensor, then any sensor whose name contains "Total" (preferring names that also
    /// mention "CPU"). Readings outside the <c>0..100</c> range and not-a-number values are ignored.
    /// </remarks>
    public static class UsageSensorSelector
    {
        /// <summary>
        /// The well-known name of the aggregate CPU usage sensor.
        /// </summary>
        public const string DefaultSensorName = "CPU Total";

        /// <summary>
        /// Selects the usage sensor sample.
        /// </summary>
        /// <param name="samples">The available usage sensors.</param>
        /// <param name="preferredNameOrIdentifier">The configured sensor name or identifier; may be <see langword="null" /> or empty.</param>
        /// <returns>The selected <see cref="SensorSample"/>, or <see langword="null" /> when no valid reading is available.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="samples"/> is <see langword="null" />.</exception>
        public static SensorSample? Select(IReadOnlyList<SensorSample> samples, string preferredNameOrIdentifier)
        {
            if (samples == null)
            {
                throw new ArgumentNullException(nameof(samples));
            }

            SensorSample? preferred = FindMatch(samples, preferredNameOrIdentifier);
            if (preferred.HasValue)
            {
                return preferred;
            }

            SensorSample? wellKnown = FindMatch(samples, DefaultSensorName);
            if (wellKnown.HasValue)
            {
                return wellKnown;
            }

            SensorSample? anyTotal = null;
            SensorSample? cpuTotal = null;

            foreach (SensorSample sample in samples)
            {
                if (!IsValid(sample.Value)
                    || string.IsNullOrEmpty(sample.Name)
                    || sample.Name.IndexOf("Total", StringComparison.OrdinalIgnoreCase) < 0)
                {
                    continue;
                }

                if (anyTotal == null)
                {
                    anyTotal = sample;
                }

                if (cpuTotal == null && sample.Name.IndexOf("CPU", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    cpuTotal = sample;
                }
            }

            return cpuTotal ?? anyTotal;
        }

        private static SensorSample? FindMatch(IReadOnlyList<SensorSample> samples, string nameOrIdentifier)
        {
            if (string.IsNullOrWhiteSpace(nameOrIdentifier))
            {
                return null;
            }

            foreach (SensorSample sample in samples)
            {
                if (!IsValid(sample.Value))
                {
                    continue;
                }

                if (string.Equals(sample.Name, nameOrIdentifier, StringComparison.OrdinalIgnoreCase))
                {
                    return sample;
                }

                if (!string.IsNullOrEmpty(sample.Identifier)
                    && string.Equals(sample.Identifier, nameOrIdentifier, StringComparison.OrdinalIgnoreCase))
                {
                    return sample;
                }
            }

            return null;
        }

        private static bool IsValid(float value)
        {
            return !float.IsNaN(value) && value >= 0f && value <= 100f;
        }
    }
}
