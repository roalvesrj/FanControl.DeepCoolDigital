using FanControl.DeepCoolDigital.Core.Protocols;

namespace FanControl.DeepCoolDigital.Core
{
    /// <summary>
    /// Provides the pure decision logic shared by display sessions.
    /// </summary>
    /// <remarks>
    /// Keeping these decisions in the core makes mode alternation, unit conversion and alert gating
    /// unit-testable without HID hardware or FanControl.
    /// </remarks>
    public static class DisplayPolicy
    {
        /// <summary>
        /// Selects the value shown on the main display for the current toggle state.
        /// </summary>
        /// <param name="showUsage"><see langword="true" /> to show the usage; otherwise, <see langword="false" /> to show the temperature.</param>
        /// <returns>The field to render.</returns>
        public static DisplayField SelectField(bool showUsage)
        {
            return showUsage ? DisplayField.Usage : DisplayField.Temperature;
        }

        /// <summary>
        /// Determines whether dynamic mode should switch between temperature and usage.
        /// </summary>
        /// <param name="elapsedMilliseconds">The time elapsed since the last switch, in milliseconds.</param>
        /// <param name="autoSwitchSeconds">The configured alternation interval, in seconds.</param>
        /// <returns><see langword="true" /> when the interval elapsed; otherwise, <see langword="false" />.</returns>
        /// <remarks>
        /// The comparison is done in 64-bit arithmetic so large intervals cannot overflow.
        /// </remarks>
        public static bool ShouldSwitchMode(int elapsedMilliseconds, int autoSwitchSeconds)
        {
            return elapsedMilliseconds >= (long)autoSwitchSeconds * 1000L;
        }

        /// <summary>
        /// Converts a temperature for display, honoring the configuration and the device capability.
        /// </summary>
        /// <param name="celsius">The temperature, in degrees Celsius.</param>
        /// <param name="fahrenheit"><see langword="true" /> when Fahrenheit is configured.</param>
        /// <param name="supportsFahrenheit"><see langword="true" /> when the device accepts Fahrenheit.</param>
        /// <returns>The value to send to the display.</returns>
        public static float ConvertTemperature(float celsius, bool fahrenheit, bool supportsFahrenheit)
        {
            if (!fahrenheit || !supportsFahrenheit)
            {
                return celsius;
            }

            return celsius * 9f / 5f + 32f;
        }

        /// <summary>
        /// Determines whether the high-temperature alert should be raised.
        /// </summary>
        /// <param name="temperatureCelsius">The current CPU temperature, in degrees Celsius.</param>
        /// <param name="alarmEnabled"><see langword="true" /> when the alert is enabled in the configuration.</param>
        /// <param name="supportsAlarm"><see langword="true" /> when the device has a high-temperature alert.</param>
        /// <param name="thresholdCelsius">The configured threshold, in degrees Celsius.</param>
        /// <returns><see langword="true" /> when the alert should be raised; otherwise, <see langword="false" />.</returns>
        /// <remarks>
        /// The alert triggers at or above the threshold, matching the reference implementations.
        /// </remarks>
        public static bool IsAlarm(float temperatureCelsius, bool alarmEnabled, bool supportsAlarm, float thresholdCelsius)
        {
            return alarmEnabled && supportsAlarm && temperatureCelsius >= thresholdCelsius;
        }
    }
}
