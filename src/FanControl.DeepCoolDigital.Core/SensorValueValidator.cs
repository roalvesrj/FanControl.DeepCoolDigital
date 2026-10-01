namespace FanControl.DeepCoolDigital.Core
{
    /// <summary>
    /// Validates sensor values received from external sources against plausible ranges.
    /// </summary>
    /// <remarks>
    /// FanControl's IPC channel is local but unauthenticated: any local process that owns the pipe
    /// could feed arbitrary values. Bounds keep a spoofed or corrupt reply from being published as a
    /// sensor reading or from tripping the display's high-temperature alert.
    /// </remarks>
    public static class SensorValueValidator
    {
        /// <summary>
        /// The lowest plausible CPU temperature, in degrees Celsius.
        /// </summary>
        public const float MinimumTemperatureCelsius = -20f;

        /// <summary>
        /// The highest plausible CPU temperature, in degrees Celsius.
        /// </summary>
        public const float MaximumTemperatureCelsius = 150f;

        /// <summary>
        /// Determines whether a temperature reading is plausible.
        /// </summary>
        /// <param name="celsius">The temperature, in degrees Celsius.</param>
        /// <returns><see langword="true" /> when the value is within the plausible range; otherwise, <see langword="false" />.</returns>
        public static bool IsPlausibleTemperature(float celsius)
        {
            return !float.IsNaN(celsius)
                && celsius >= MinimumTemperatureCelsius
                && celsius <= MaximumTemperatureCelsius;
        }

        /// <summary>
        /// Determines whether a usage reading is plausible.
        /// </summary>
        /// <param name="percent">The usage percentage.</param>
        /// <returns><see langword="true" /> when the value is between <c>0</c> and <c>100</c>; otherwise, <see langword="false" />.</returns>
        public static bool IsPlausibleUsage(float percent)
        {
            return !float.IsNaN(percent) && percent >= 0f && percent <= 100f;
        }
    }
}
