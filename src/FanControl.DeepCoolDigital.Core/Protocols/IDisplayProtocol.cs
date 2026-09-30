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
        /// <see langword="true" /> when the first byte is a report id; otherwise, <see langword="false" />.
        /// Some "SE" device variants expect the payload without the report id.
        /// </value>
        bool IncludesReportId { get; }

        /// <summary>
        /// Creates the packets that must be sent once before streaming status data.
        /// </summary>
        /// <returns>An array of initialization packets, or an empty array when the device needs none.</returns>
        byte[][] CreateInitializationPackets();

        /// <summary>
        /// Builds a display packet for the specified field and value.
        /// </summary>
        /// <param name="field">One of the enumeration values that specifies which value is being displayed.</param>
        /// <param name="value">The value to display.</param>
        /// <param name="alarm"><see langword="true" /> to raise the high-temperature alert; otherwise, <see langword="false" />.</param>
        /// <returns>The packet bytes to write to the device.</returns>
        byte[] BuildPacket(DisplayField field, float value, bool alarm);
    }
}
