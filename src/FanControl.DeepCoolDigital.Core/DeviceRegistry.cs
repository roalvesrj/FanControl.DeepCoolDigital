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
            _ => new AgProtocol());

        /// <summary>
        /// The definition of the AK400 DIGITAL.
        /// </summary>
        public static readonly DeviceDefinition Ak400Digital = CreateAkDefinition(0x0001, "DeepCool AK400 DIGITAL");

        /// <summary>
        /// The definition of the AK620 DIGITAL.
        /// </summary>
        public static readonly DeviceDefinition Ak620Digital = CreateAkDefinition(0x0002, "DeepCool AK620 DIGITAL");

        /// <summary>
        /// The definition of the AK500 DIGITAL.
        /// </summary>
        public static readonly DeviceDefinition Ak500Digital = CreateAkDefinition(0x0003, "DeepCool AK500 DIGITAL");

        /// <summary>
        /// The definition of the AK500S DIGITAL.
        /// </summary>
        public static readonly DeviceDefinition Ak500SDigital = CreateAkDefinition(0x0004, "DeepCool AK500S DIGITAL");

        private static readonly DeviceDefinition[] Definitions =
        {
            AgDigital,
            Ak400Digital,
            Ak620Digital,
            Ak500Digital,
            Ak500SDigital
        };

        private static readonly IReadOnlyList<DeviceDefinition> ReadOnlyDefinitions = System.Array.AsReadOnly(Definitions);

        /// <summary>
        /// Gets all known device definitions.
        /// </summary>
        /// <value>A read-only list of every entry in the registry.</value>
        public static IReadOnlyList<DeviceDefinition> All => ReadOnlyDefinitions;

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

        /// <summary>
        /// Resolves the definition to use for a USB identity, applying the built-in fallback policy.
        /// </summary>
        /// <param name="vendorId">The USB vendor id of the device.</param>
        /// <param name="productId">The USB product id of the device.</param>
        /// <param name="usedFallback">When this method returns, contains <see langword="true" /> when the identity was not registered and the AG protocol was assumed; otherwise, <see langword="false" />. This parameter is treated as uninitialized.</param>
        /// <returns>
        /// The matching <see cref="DeviceDefinition"/>; the AG DIGITAL definition when the vendor is DeepCool
        /// but the product id is unknown (with <paramref name="usedFallback"/> set to <see langword="true" />);
        /// otherwise, <see langword="null" />.
        /// </returns>
        public static DeviceDefinition Resolve(int vendorId, int productId, out bool usedFallback)
        {
            usedFallback = false;

            DeviceDefinition definition = Find(vendorId, productId);
            if (definition != null)
            {
                return definition;
            }

            if (vendorId == DeepCoolVendorId)
            {
                usedFallback = true;
                return AgDigital;
            }

            return null;
        }

        private static DeviceDefinition CreateAkDefinition(int productId, string model)
        {
            return new DeviceDefinition(
                DeepCoolVendorId,
                productId,
                model,
                new DeviceCapabilities(
                    supportsTemperature: true,
                    supportsUsage: true,
                    supportsAlarm: true,
                    supportsFahrenheit: true,
                    defaultAlarmTemperatureCelsius: 90f),
                options => new AkProtocol(options));
        }
    }
}
