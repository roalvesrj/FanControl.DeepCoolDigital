using System;
using System.Collections.Generic;

namespace FanControl.DeepCoolDigital.Core
{
    /// <summary>
    /// Finds a sensor sample by identifier or name.
    /// </summary>
    /// <remarks>
    /// Identifiers are matched exactly (they are opaque keys); names are matched case-insensitively
    /// and only as a fallback, which supports sources that do not provide identifiers at all.
    /// </remarks>
    public static class SensorLookup
    {
        /// <summary>
        /// Tries to find the value of a sensor by its identifier or name.
        /// </summary>
        /// <param name="samples">The available sensor samples.</param>
        /// <param name="nameOrIdentifier">The identifier or name to look for.</param>
        /// <param name="value">When this method returns, contains the sensor value when found.</param>
        /// <returns><see langword="true" /> when the sensor was found; otherwise, <see langword="false" />.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="samples"/> is <see langword="null" />.</exception>
        public static bool TryGetValue(IReadOnlyList<SensorSample> samples, string nameOrIdentifier, out float value)
        {
            if (samples == null)
            {
                throw new ArgumentNullException(nameof(samples));
            }

            if (!string.IsNullOrEmpty(nameOrIdentifier))
            {
                foreach (SensorSample sample in samples)
                {
                    if (Matches(sample, nameOrIdentifier))
                    {
                        value = sample.Value;
                        return true;
                    }
                }
            }

            value = 0f;
            return false;
        }

        /// <summary>
        /// Determines whether a sample matches an identifier or name.
        /// </summary>
        /// <param name="sample">The sample to test.</param>
        /// <param name="nameOrIdentifier">The identifier or name to look for.</param>
        /// <returns><see langword="true" /> when the sample matches; otherwise, <see langword="false" />.</returns>
        public static bool Matches(SensorSample sample, string nameOrIdentifier)
        {
            if (string.IsNullOrEmpty(nameOrIdentifier))
            {
                return false;
            }

            if (!string.IsNullOrEmpty(sample.Identifier)
                && string.Equals(sample.Identifier, nameOrIdentifier, StringComparison.Ordinal))
            {
                return true;
            }

            return string.Equals(sample.Name, nameOrIdentifier, StringComparison.OrdinalIgnoreCase);
        }
    }
}
