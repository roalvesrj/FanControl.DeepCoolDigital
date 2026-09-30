namespace FanControl.DeepCoolDigital.Core
{
    /// <summary>
    /// Carries configuration overrides for a single USB identity, parsed from a
    /// <c>[device:VID:PID]</c> section of the ini file.
    /// </summary>
    /// <remarks>
    /// Every property is optional: <see langword="null" /> means "inherit the global value". Use
    /// <see cref="PluginConfig.ForDevice"/> to obtain the effective, merged settings.
    /// </remarks>
    public sealed class DeviceOverride
    {
        internal DeviceOverride(
            int vendorId,
            int productId,
            DisplayMode? mode,
            int? autoSwitchSeconds,
            float? alarmTemperature,
            bool? alarmEnabled,
            bool? fahrenheit)
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
        /// Gets the USB vendor id this override applies to.
        /// </summary>
        /// <value>The vendor id, for example <c>0x3633</c>.</value>
        public int VendorId { get; }

        /// <summary>
        /// Gets the USB product id this override applies to.
        /// </summary>
        /// <value>The product id, for example <c>0x0002</c>.</value>
        public int ProductId { get; }

        /// <summary>
        /// Gets the overridden display mode.
        /// </summary>
        /// <value>One of the enumeration values that specifies the display mode, or <see langword="null" /> to inherit the global value.</value>
        public DisplayMode? Mode { get; }

        /// <summary>
        /// Gets the overridden alternation interval.
        /// </summary>
        /// <value>The interval in seconds, or <see langword="null" /> to inherit the global value.</value>
        public int? AutoSwitchSeconds { get; }

        /// <summary>
        /// Gets the overridden alert threshold.
        /// </summary>
        /// <value>The threshold in degrees Celsius, or <see langword="null" /> to inherit the global value.</value>
        public float? AlarmTemperature { get; }

        /// <summary>
        /// Gets the overridden alert switch.
        /// </summary>
        /// <value><see langword="true" /> or <see langword="false" /> to override, or <see langword="null" /> to inherit the global value.</value>
        public bool? AlarmEnabled { get; }

        /// <summary>
        /// Gets the overridden temperature unit setting.
        /// </summary>
        /// <value><see langword="true" /> or <see langword="false" /> to override, or <see langword="null" /> to inherit the global value.</value>
        public bool? Fahrenheit { get; }
    }
}
