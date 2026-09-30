using System;

namespace FanControl.DeepCoolDigital.Core.Protocols
{
    /// <summary>
    /// Implements the display protocol of the DeepCool AK DIGITAL series (AK400/AK620/AK500/AK500S).
    /// </summary>
    /// <remarks>
    /// The 64-byte output report is laid out as follows:
    /// <list type="bullet">
    /// <item><description>Byte 0: report id <c>0x10</c> (omitted on "SE" variants).</description></item>
    /// <item><description>Byte 1: status <c>19</c> for Celsius, <c>35</c> for Fahrenheit, <c>76</c> for usage.</description></item>
    /// <item><description>Byte 2: usage status bar, between <c>1</c> and <c>10</c>.</description></item>
    /// <item><description>Bytes 3 to 5: hundreds, tens and ones digits; values above 999 are displayed as <c>999</c>.</description></item>
    /// <item><description>Byte 6: <c>1</c> while the high-temperature alert is active, otherwise <c>0</c>.</description></item>
    /// </list>
    /// The device expects an initialization packet (<c>0xAA</c> in byte 1) once per connection.
    /// </remarks>
    public sealed class AkProtocol : IDisplayProtocol
    {
        private const byte ReportId = 0x10;
        private const byte TemperatureStatusCelsius = 19;
        private const byte TemperatureStatusFahrenheit = 35;
        private const byte UsageStatus = 76;

        private readonly DeviceOptions _options;

        /// <summary>
        /// Initializes a new instance of the <see cref="AkProtocol"/> class.
        /// </summary>
        /// <param name="options">The per-device behavior options, such as the temperature unit.</param>
        /// <exception cref="ArgumentNullException"><paramref name="options"/> is <see langword="null" />.</exception>
        public AkProtocol(DeviceOptions options)
        {
            _options = options ?? throw new ArgumentNullException(nameof(options));
        }

        /// <inheritdoc />
        public int PacketLength => 64;

        /// <inheritdoc />
        public bool IncludesReportId => true;

        /// <inheritdoc />
        public byte[][] CreateInitializationPackets()
        {
            var init = new byte[PacketLength];
            init[0] = ReportId;
            init[1] = 0xAA;

            return new[] { init };
        }

        /// <inheritdoc />
        public byte[] BuildPacket(DisplayFrame frame)
        {
            var packet = new byte[PacketLength];
            packet[0] = ReportId;
            packet[1] = frame.Field == DisplayField.Temperature
                ? (_options.Fahrenheit ? TemperatureStatusFahrenheit : TemperatureStatusCelsius)
                : UsageStatus;
            packet[2] = DigitPacketBuilder.UsageBar(frame.Usage);
            DigitPacketBuilder.WriteDigits(packet, 3, 3, DigitPacketBuilder.ClampDigits(frame.Value, 3));
            packet[6] = (byte)(frame.Alarm ? 1 : 0);

            return packet;
        }

        /// <inheritdoc />
        public byte[] ApplyTransportQuirks(byte[] packet, string productName)
        {
            if (packet == null)
            {
                throw new ArgumentNullException(nameof(packet));
            }

            if (string.IsNullOrEmpty(productName) || productName.StartsWith("AK", StringComparison.Ordinal))
            {
                return packet;
            }

            var shifted = new byte[packet.Length];
            Array.Copy(packet, 1, shifted, 0, packet.Length - 1);
            return shifted;
        }
    }
}
