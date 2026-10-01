namespace FanControl.DeepCoolDigital.Core
{
    /// <summary>
    /// Defines a source of CPU temperature and usage readings.
    /// </summary>
    /// <remarks>
    /// Implementations must never let an exception escape from <see cref="TryRead"/>; failures are
    /// reported through the return value so the caller can apply its fallback policy.
    /// </remarks>
    public interface ISensorSource
    {
        /// <summary>
        /// Reads the current CPU temperature and usage.
        /// </summary>
        /// <param name="temperatureCelsius">When this method returns, contains the CPU temperature in degrees Celsius.</param>
        /// <param name="usage">When this method returns, contains the CPU usage percentage.</param>
        /// <returns><see langword="true" /> when both values were read; otherwise, <see langword="false" />.</returns>
        bool TryRead(out float temperatureCelsius, out float usage);
    }
}
