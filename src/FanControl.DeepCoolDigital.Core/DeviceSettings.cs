namespace FanControl.DeepCoolDigital.Core
{
    /// <summary>
    /// Represents the effective settings of one device after merging global and per-device configuration.
    /// </summary>
    public sealed class DeviceSettings
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="DeviceSettings"/> class.
        /// </summary>
        /// <param name="vendorId">The USB vendor id of the device.</param>
        /// <param name="productId">The USB product id of the device.</param>
        /// <param name="mode">The display mode of the device.</param>
        /// <param name="autoSwitchSeconds">The alternation interval, in seconds, used when <paramref name="mode"/> is <see cref="DisplayMode.Auto"/>.</param>
        /// <param name="alarmTemperature">The alert threshold, in degrees Celsius.</param>
        /// <param name="alarmEnabled"><see langword="true" /> to allow the high-temperature alert; otherwise, <see langword="false" />.</param>
        /// <param name="fahrenheit"><see langword="true" /> to display temperatures in Fahrenheit; otherwise, <see langword="false" />.</param>
        public DeviceSettings(
            int vendorId,
            int productId,
            DisplayMode mode,
            int autoSwitchSeconds,
            float alarmTemperature,
            bool alarmEnabled,
            bool fahrenheit)
        {
            VendorId = vendorId;
            ProductId = productId;
            Mode = mode;
            AutoSwitchSeconds = autoSwitchSeconds;
            AlarmTemperature = alarmTemperature;
            AlarmEnabled = alarmEnabled;
            Fahrenheit = fahrenheit;
        }

        /// <summary>
        /// Gets the USB vendor id of the device.
        /// </summary>
        /// <value>The vendor id.</value>
        public int VendorId { get; }

        /// <summary>
        /// Gets the USB product id of the device.
        /// </summary>
        /// <value>The product id.</value>
        public int ProductId { get; }

        /// <summary>
        /// Gets the display mode of the device.
        /// </summary>
        /// <value>One of the enumeration values that specifies the display mode.</value>
        public DisplayMode Mode { get; }

        /// <summary>
        /// Gets the alternation interval used when <see cref="Mode"/> is <see cref="DisplayMode.Auto"/>.
        /// </summary>
        /// <value>The interval, in seconds.</value>
        public int AutoSwitchSeconds { get; }

        /// <summary>
        /// Gets the alert threshold.
        /// </summary>
        /// <value>The temperature at or above which the alert is raised, in degrees Celsius.</value>
        public float AlarmTemperature { get; }

        /// <summary>
        /// Gets a value indicating whether the high-temperature alert is allowed.
        /// </summary>
        /// <value><see langword="true" /> when the alert is allowed; otherwise, <see langword="false" />.</value>
        public bool AlarmEnabled { get; }

        /// <summary>
        /// Gets a value indicating whether temperatures are displayed in Fahrenheit.
        /// </summary>
        /// <value><see langword="true" /> when Fahrenheit is used; otherwise, <see langword="false" />.</value>
        public bool Fahrenheit { get; }
    }
}
