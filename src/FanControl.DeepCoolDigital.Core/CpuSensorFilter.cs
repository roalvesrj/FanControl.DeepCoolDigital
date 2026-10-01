using System;
using System.Collections.Generic;

namespace FanControl.DeepCoolDigital.Core
{
    /// <summary>
    /// Selects CPU temperature candidates out of a mixed sensor list.
    /// </summary>
    /// <remarks>
    /// FanControl exposes every sensor it knows (CPU, GPU, motherboard, storage). Temperature fallback
    /// selection that simply takes the highest reading could pick a GPU hot spot, so candidates are
    /// narrowed to CPU sensors first, using the LibreHardwareMonitor identifier prefixes
    /// (<c>/amdcpu</c>, <c>/intelcpu</c>). When no identifier matches, the input list is returned
    /// unchanged so sources without identifiers keep working.
    /// </remarks>
    public static class CpuSensorFilter
    {
        private static readonly string[] CpuIdentifierPrefixes = { "/amdcpu", "/intelcpu" };

        /// <summary>
        /// Filters a sensor list down to CPU temperature candidates.
        /// </summary>
        /// <param name="samples">The available sensor samples.</param>
        /// <returns>The CPU candidates, or the original list when no CPU identifier is recognized.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="samples"/> is <see langword="null" />.</exception>
        public static IReadOnlyList<SensorSample> SelectCpuSensors(IReadOnlyList<SensorSample> samples)
        {
            if (samples == null)
            {
                throw new ArgumentNullException(nameof(samples));
            }

            var cpuSamples = new List<SensorSample>();

            foreach (SensorSample sample in samples)
            {
                if (string.IsNullOrEmpty(sample.Identifier))
                {
                    continue;
                }

                foreach (string prefix in CpuIdentifierPrefixes)
                {
                    if (sample.Identifier.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                    {
                        cpuSamples.Add(sample);
                        break;
                    }
                }
            }

            return cpuSamples.Count > 0 ? cpuSamples : samples;
        }
    }
}
