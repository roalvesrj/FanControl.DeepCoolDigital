namespace FanControl.DeepCoolDigital.Core
{
    /// <summary>
    /// Describes what a DeepCool display device is able to render.
    /// </summary>
    public sealed class DeviceCapabilities
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="DeviceCapabilities"/> class.
        /// </summary>
        /// <param name="supportsTemperature"><see langword="true" /> if the device can show the CPU temperature; otherwise, <see langword="false" />.</param>
        /// <param name="supportsUsage"><see langword="true" /> if the device can show the CPU usage; otherwise, <see langword="false" />.</param>
        /// <param name="supportsAlarm"><see langword="true" /> if the device has a high-temperature alert; otherwise, <see langword="false" />.</param>
        /// <param name="supportsFahrenheit"><see langword="true" /> if the device accepts Fahrenheit values; otherwise, <see langword="false" />.</param>
        /// <param name="defaultAlarmTemperatureCelsius">The default alert threshold, in degrees Celsius.</param>
        public DeviceCapabilities(
            bool supportsTemperature,
            bool supportsUsage,
            bool supportsAlarm,
            bool supportsFahrenheit,
            float defaultAlarmTemperatureCelsius)
        {
            SupportsTemperature = supportsTemperature;
            SupportsUsage = supportsUsage;
            SupportsAlarm = supportsAlarm;
            SupportsFahrenheit = supportsFahrenheit;
            DefaultAlarmTemperatureCelsius = defaultAlarmTemperatureCelsius;
        }

        /// <summary>
        /// Gets a value indicating whether the device can show the CPU temperature.
        /// </summary>
        /// <value><see langword="true" /> if the CPU temperature is supported; otherwise, <see langword="false" />.</value>
        public bool SupportsTemperature { get; }

        /// <summary>
        /// Gets a value indicating whether the device can show the CPU usage.
        /// </summary>
        /// <value><see langword="true" /> if the CPU usage is supported; otherwise, <see langword="false" />.</value>
        public bool SupportsUsage { get; }

        /// <summary>
        /// Gets a value indicating whether the device has a high-temperature alert.
        /// </summary>
        /// <value><see langword="true" /> if the alert is supported; otherwise, <see langword="false" />.</value>
        public bool SupportsAlarm { get; }

        /// <summary>
        /// Gets a value indicating whether the device accepts Fahrenheit values.
        /// </summary>
        /// <value><see langword="true" /> if Fahrenheit is supported; otherwise, <see langword="false" />.</value>
        public bool SupportsFahrenheit { get; }

        /// <summary>
        /// Gets the default alert threshold, in degrees Celsius.
        /// </summary>
        /// <value>The temperature above which the alert is raised.</value>
        public float DefaultAlarmTemperatureCelsius { get; }
    }
}
