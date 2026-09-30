namespace FanControl.DeepCoolDigital.Core.Protocols
{
    /// <summary>
    /// Defines the wire format used to drive a DeepCool DIGITAL display.
    /// </summary>
    /// <remarks>
    /// Implementations only translate values into bytes; they never touch HID APIs. This keeps the
    /// packet layout fully unit-testable without hardware.
    /// </remarks>
    public interface IDisplayProtocol
    {
        /// <summary>
        /// Gets the total length, in bytes, of a display packet.
        /// </summary>
        /// <value>The packet length, typically <c>64</c>.</value>
        int PacketLength { get; }

        /// <summary>
        /// Gets a value indicating whether packets start with a HID report id byte.
        /// </summary>
        /// <value>
        /// <see langword="true" /> when the first byte of the packet is a report id; otherwise, <see langword="false" />.
        /// </value>
        bool IncludesReportId { get; }

        /// <summary>
        /// Creates the packets that must be sent once before streaming status data.
        /// </summary>
        /// <returns>An array of initialization packets, or an empty array when the device needs none.</returns>
        byte[][] CreateInitializationPackets();

        /// <summary>
        /// Builds a display packet for the specified system state.
        /// </summary>
        /// <param name="frame">The state to render.</param>
        /// <returns>The packet bytes to write to the device, before <see cref="ApplyTransportQuirks"/>.</returns>
        byte[] BuildPacket(DisplayFrame frame);

        /// <summary>
        /// Adjusts a packet for the quirks of a specific device variant.
        /// </summary>
        /// <param name="packet">A packet produced by <see cref="BuildPacket"/> or <see cref="CreateInitializationPackets"/>.</param>
        /// <param name="productName">The HID product string of the connected device, when available.</param>
        /// <returns>The bytes that should actually be written to the device.</returns>
        /// <remarks>
        /// For example, "SE" variants of the AK series do not expect the report id byte, so the
        /// payload is shifted one byte to the left.
        /// </remarks>
        byte[] ApplyTransportQuirks(byte[] packet, string productName);
    }
}
