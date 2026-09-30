using System.Collections.Generic;
using FanControl.DeepCoolDigital.Core.Protocols;

namespace FanControl.DeepCoolDigital.Core
{
    /// <summary>
    /// Provides the built-in table of known DeepCool DIGITAL devices.
    /// </summary>
    /// <remarks>
    /// The table is intentionally data-driven: supporting a new model means adding a
    /// <see cref="DeviceDefinition"/> and (when the packet layout differs) a new <see cref="IDisplayProtocol"/>.
    /// </remarks>
    public static class DeviceRegistry
    {
        /// <summary>
        /// The USB vendor id shared by most DeepCool DIGITAL devices.
        /// </summary>
        public const int DeepCoolVendorId = 0x3633;

        /// <summary>
        /// The definition of the AG DIGITAL series (AG300/AG400/AG500/AG620).
        /// </summary>
        public static readonly DeviceDefinition AgDigital = new DeviceDefinition(
            DeepCoolVendorId,
            0x0008,
            "DeepCool AG DIGITAL",
            new DeviceCapabilities(
                supportsTemperature: true,
                supportsUsage: true,
                supportsAlarm: true,
                supportsFahrenheit: false,
                defaultAlarmTemperatureCelsius: 90f),
            () => new AgProtocol());

        private static readonly DeviceDefinition[] Definitions =
        {
            AgDigital
        };

        /// <summary>
        /// Gets all known device definitions.
        /// </summary>
        /// <value>A read-only list of every entry in the registry.</value>
        public static IReadOnlyList<DeviceDefinition> All => Definitions;

        /// <summary>
        /// Finds the definition matching a USB identity.
        /// </summary>
        /// <param name="vendorId">The USB vendor id of the device.</param>
        /// <param name="productId">The USB product id of the device.</param>
        /// <returns>The matching <see cref="DeviceDefinition"/>, or <see langword="null" /> when the identity is unknown.</returns>
        public static DeviceDefinition Find(int vendorId, int productId)
        {
            foreach (DeviceDefinition definition in Definitions)
            {
                if (definition.VendorId == vendorId && definition.ProductId == productId)
                {
                    return definition;
                }
            }

            return null;
        }
    }
}
