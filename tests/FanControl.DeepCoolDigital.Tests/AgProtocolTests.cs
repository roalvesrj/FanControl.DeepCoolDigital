using FanControl.DeepCoolDigital.Core.Protocols;
using NUnit.Framework;

namespace FanControl.DeepCoolDigital.Tests
{
    /// <summary>
    /// Covers the AG DIGITAL packet layout, including the golden bytes shipped in v0.1.0.
    /// </summary>
    [TestFixture]
    [Category("Protocols")]
    public class AgProtocolTests
    {
        private AgProtocol _protocol;

        [SetUp]
        public void SetUp()
        {
            _protocol = new AgProtocol();
        }

        [Test]
        public void PacketLength_IsAlways64()
        {
            Assert.That(_protocol.PacketLength, Is.EqualTo(64));
        }

        [Test]
        public void IncludesReportId_IsTrue()
        {
            Assert.That(_protocol.IncludesReportId, Is.True);
        }

        [Test]
        public void CreateInitializationPackets_ReturnsEmptyArray()
        {
            Assert.That(_protocol.CreateInitializationPackets(), Is.Empty);
        }

        [Test]
        public void BuildPacket_Temperature42_ProducesGoldenBytes()
        {
            byte[] packet = _protocol.BuildPacket(Frame(DisplayField.Temperature, 42f));

            var expected = new byte[64];
            expected[0] = 0x10;
            expected[1] = 19;
            expected[3] = 4;
            expected[4] = 2;

            Assert.That(packet, Is.EqualTo(expected));
        }

        [Test]
        public void BuildPacket_Usage37_ProducesGoldenBytes()
        {
            byte[] packet = _protocol.BuildPacket(Frame(DisplayField.Usage, 37f));

            var expected = new byte[64];
            expected[0] = 0x10;
            expected[1] = 76;
            expected[3] = 3;
            expected[4] = 7;

            Assert.That(packet, Is.EqualTo(expected));
        }

        [TestCase(0f, 0, 0)]
        [TestCase(9f, 0, 9)]
        [TestCase(99f, 9, 9)]
        [TestCase(100f, 9, 9)]
        [TestCase(105f, 9, 9)]
        [TestCase(-5f, 0, 0)]
        [TestCase(-0.5f, 0, 0)]
        [TestCase(float.NaN, 0, 0)]
        [TestCase(float.PositiveInfinity, 9, 9)]
        [TestCase(float.NegativeInfinity, 0, 0)]
        [TestCase(1e9f, 9, 9)]
        public void BuildPacket_TemperatureDigits_AreClamped(float value, byte expectedTens, byte expectedOnes)
        {
            byte[] packet = _protocol.BuildPacket(Frame(DisplayField.Temperature, value));

            Assert.That(packet[3], Is.EqualTo(expectedTens));
            Assert.That(packet[4], Is.EqualTo(expectedOnes));
        }

        [TestCase(100f, 9, 9)]
        [TestCase(1000f, 9, 9)]
        public void BuildPacket_UsageDigits_AreClamped(float value, byte expectedTens, byte expectedOnes)
        {
            byte[] packet = _protocol.BuildPacket(Frame(DisplayField.Usage, value));

            Assert.That(packet[3], Is.EqualTo(expectedTens));
            Assert.That(packet[4], Is.EqualTo(expectedOnes));
        }

        [TestCase(true, 1)]
        [TestCase(false, 0)]
        public void BuildPacket_AlarmFlag_IsWrittenToByte5(bool alarm, byte expected)
        {
            byte[] packet = _protocol.BuildPacket(Frame(DisplayField.Temperature, 42f, alarm));

            Assert.That(packet[5], Is.EqualTo(expected));
        }

        [Test]
        public void BuildPacket_UsageWithAlarm_StillCarriesAlert()
        {
            byte[] packet = _protocol.BuildPacket(Frame(DisplayField.Usage, 37f, alarm: true));

            Assert.That(packet[5], Is.EqualTo(1));
        }

        [Test]
        public void BuildPacket_TemperatureFloat_IsTruncated()
        {
            byte[] packet = _protocol.BuildPacket(Frame(DisplayField.Temperature, 42.9f));

            Assert.That(packet[3], Is.EqualTo(4));
            Assert.That(packet[4], Is.EqualTo(2));
        }

        [Test]
        public void ApplyTransportQuirks_ReturnsPacketUnchanged()
        {
            byte[] packet = _protocol.BuildPacket(Frame(DisplayField.Temperature, 42f));

            byte[] result = _protocol.ApplyTransportQuirks(packet, "AG-DIGITAL");

            Assert.That(result, Is.SameAs(packet));
        }

        private static DisplayFrame Frame(DisplayField field, float value, bool alarm = false)
        {
            return new DisplayFrame(
                field,
                value,
                field == DisplayField.Temperature ? value : 0f,
                field == DisplayField.Usage ? value : 0f,
                alarm);
        }
    }
}
