using System;

namespace FanControl.DeepCoolDigital.Core.Protocols
{
    /// <summary>
    /// Implements the display protocol of the DeepCool AG DIGITAL series (AG300/AG400/AG500/AG620).
    /// </summary>
    /// <remarks>
    /// The 64-byte output report is laid out as follows:
    /// <list type="bullet">
    /// <item><description>Byte 0: report id <c>0x10</c>.</description></item>
    /// <item><description>Byte 1: status <c>19</c> for temperature, <c>76</c> for usage.</description></item>
    /// <item><description>Byte 2: unused.</description></item>
    /// <item><description>Bytes 3 and 4: tens and ones digits; values above 99 are displayed as <c>99</c>.</description></item>
    /// <item><description>Byte 5: <c>1</c> while the high-temperature alert is active, otherwise <c>0</c>.</description></item>
    /// </list>
    /// </remarks>
    public sealed class AgProtocol : IDisplayProtocol
    {
        private const byte ReportId = 0x10;
        private const byte TemperatureStatus = 19;
        private const byte UsageStatus = 76;

        /// <inheritdoc />
        public int PacketLength => 64;

        /// <inheritdoc />
        public bool IncludesReportId => true;

        /// <inheritdoc />
        public byte[][] CreateInitializationPackets()
        {
            return Array.Empty<byte[]>();
        }

        /// <inheritdoc />
        public byte[] BuildPacket(DisplayField field, float value, bool alarm)
        {
            var packet = new byte[PacketLength];
            int digits = (int)value;

            if (digits < 0)
            {
                digits = 0;
            }

            packet[0] = ReportId;
            packet[1] = field == DisplayField.Temperature ? TemperatureStatus : UsageStatus;
            packet[2] = 0;
            packet[3] = (byte)(digits < 100 ? digits % 100 / 10 : 9);
            packet[4] = (byte)(digits < 100 ? digits % 10 : 9);
            packet[5] = (byte)(alarm ? 1 : 0);

            return packet;
        }
    }
}
