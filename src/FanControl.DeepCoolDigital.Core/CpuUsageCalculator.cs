using System;

namespace FanControl.DeepCoolDigital.Core
{
    /// <summary>
    /// Computes CPU usage percentages from Windows system time counters.
    /// </summary>
    /// <remarks>
    /// The calculation matches the behavior of the plugin since its first release: the busy share of the
    /// delta between two samples is returned, clamped to the <c>0..100</c> range.
    /// </remarks>
    public static class CpuUsageCalculator
    {
        /// <summary>
        /// Calculates the CPU usage between two system time samples.
        /// </summary>
        /// <param name="previousIdle">The idle time of the previous sample, in 100 ns ticks.</param>
        /// <param name="previousKernel">The kernel time of the previous sample, in 100 ns ticks.</param>
        /// <param name="previousUser">The user time of the previous sample, in 100 ns ticks.</param>
        /// <param name="currentIdle">The idle time of the current sample, in 100 ns ticks.</param>
        /// <param name="currentKernel">The kernel time of the current sample, in 100 ns ticks.</param>
        /// <param name="currentUser">The user time of the current sample, in 100 ns ticks.</param>
        /// <param name="fallback">The value to return when the counters did not advance.</param>
        /// <returns>The CPU usage percentage, between <c>0</c> and <c>100</c>.</returns>
        public static float Calculate(
            ulong previousIdle,
            ulong previousKernel,
            ulong previousUser,
            ulong currentIdle,
            ulong currentKernel,
            ulong currentUser,
            float fallback)
        {
            ulong idleDelta = currentIdle - previousIdle;
            ulong totalDelta = currentKernel - previousKernel + currentUser - previousUser;

            if (totalDelta == 0)
            {
                return Clamp(fallback);
            }

            float usage = 100f * (totalDelta - idleDelta) / totalDelta;
            return Clamp(usage);
        }

        private static float Clamp(float value)
        {
            return Math.Max(0f, Math.Min(100f, value));
        }
    }
}
