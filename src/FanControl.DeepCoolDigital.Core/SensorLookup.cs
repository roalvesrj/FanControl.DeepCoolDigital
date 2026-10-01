using System;
using System.Collections.Generic;

namespace FanControl.DeepCoolDigital.Core
{
    /// <summary>
    /// Finds a sensor sample by identifier or name.
    /// </summary>
    /// <remarks>
    /// Identifiers are matched exactly (they are opaque keys) and are always preferred: the lookup
    /// first scans all identifiers, then falls back to case-insensitive names, which supports sources
    /// that do not provide identifiers at all.
    /// </remarks>
    public static class SensorLookup
    {
        /// <summary>
        /// Resolves the lookup key for a sample: its identifier when present, otherwise its name.
        /// </summary>
        /// <param name="sample">The sample whose key is resolved.</param>
        /// <returns>The identifier when the sample provides one; otherwise, the sample name.</returns>
        public static string ResolveKey(SensorSample sample)
        {
            return string.IsNullOrEmpty(sample.Identifier) ? sample.Name : sample.Identifier;
        }

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
                    if (!string.IsNullOrEmpty(sample.Identifier)
                        && string.Equals(sample.Identifier, nameOrIdentifier, StringComparison.Ordinal))
                    {
                        value = sample.Value;
                        return true;
                    }
                }

                foreach (SensorSample sample in samples)
                {
                    if (string.Equals(sample.Name, nameOrIdentifier, StringComparison.OrdinalIgnoreCase))
                    {
                        value = sample.Value;
                        return true;
                    }
                }
            }

            value = 0f;
            return false;
        }
    }
}
