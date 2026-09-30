namespace FanControl.DeepCoolDigital.Core
{
    /// <summary>
    /// Carries per-device behavior options into protocol instances.
    /// </summary>
    /// <remarks>
    /// Options are supplied by the device definition factory when a protocol instance is created, so
    /// protocols never need to read the configuration directly.
    /// </remarks>
    public sealed class DeviceOptions
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="DeviceOptions"/> class.
        /// </summary>
        /// <param name="fahrenheit"><see langword="true" /> if temperatures are sent in Fahrenheit; otherwise, <see langword="false" />.</param>
        public DeviceOptions(bool fahrenheit)
        {
            Fahrenheit = fahrenheit;
        }

        /// <summary>
        /// Gets a value indicating whether temperatures are sent in Fahrenheit.
        /// </summary>
        /// <value><see langword="true" /> if Fahrenheit values are used; otherwise, <see langword="false" />.</value>
        public bool Fahrenheit { get; }
    }
}
