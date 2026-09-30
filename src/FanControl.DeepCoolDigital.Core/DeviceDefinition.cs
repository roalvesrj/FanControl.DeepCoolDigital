using System;
using FanControl.DeepCoolDigital.Core.Protocols;

namespace FanControl.DeepCoolDigital.Core
{
    /// <summary>
    /// Binds a USB device identity to its model, capabilities and display protocol.
    /// </summary>
    public sealed class DeviceDefinition
    {
        private readonly Func<IDisplayProtocol> _createProtocol;

        /// <summary>
        /// Initializes a new instance of the <see cref="DeviceDefinition"/> class.
        /// </summary>
        /// <param name="vendorId">The USB vendor id of the device.</param>
        /// <param name="productId">The USB product id of the device.</param>
        /// <param name="model">The human-readable model name.</param>
        /// <param name="capabilities">What the device is able to render.</param>
        /// <param name="createProtocol">A factory that creates a fresh protocol instance for each connection.</param>
        /// <exception cref="ArgumentNullException"><paramref name="model"/> or <paramref name="capabilities"/> or <paramref name="createProtocol"/> is <see langword="null" />.</exception>
        public DeviceDefinition(
            int vendorId,
            int productId,
            string model,
            DeviceCapabilities capabilities,
            Func<IDisplayProtocol> createProtocol)
        {
            Model = model ?? throw new ArgumentNullException(nameof(model));
            Capabilities = capabilities ?? throw new ArgumentNullException(nameof(capabilities));
            _createProtocol = createProtocol ?? throw new ArgumentNullException(nameof(createProtocol));
            VendorId = vendorId;
            ProductId = productId;
        }

        /// <summary>
        /// Gets the USB vendor id of the device.
        /// </summary>
        /// <value>The vendor id, for example <c>0x3633</c>.</value>
        public int VendorId { get; }

        /// <summary>
        /// Gets the USB product id of the device.
        /// </summary>
        /// <value>The product id, for example <c>0x0008</c>.</value>
        public int ProductId { get; }

        /// <summary>
        /// Gets the human-readable model name.
        /// </summary>
        /// <value>The model name, for example "DeepCool AG DIGITAL".</value>
        public string Model { get; }

        /// <summary>
        /// Gets what the device is able to render.
        /// </summary>
        /// <value>The capability flags of the device.</value>
        public DeviceCapabilities Capabilities { get; }

        /// <summary>
        /// Creates a fresh protocol instance for a connection.
        /// </summary>
        /// <returns>A new <see cref="IDisplayProtocol"/> for the device.</returns>
        public IDisplayProtocol CreateProtocol()
        {
            return _createProtocol();
        }
    }
}
