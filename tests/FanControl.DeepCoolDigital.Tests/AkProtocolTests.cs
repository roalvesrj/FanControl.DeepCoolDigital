using FanControl.DeepCoolDigital.Core;
using FanControl.DeepCoolDigital.Core.Protocols;
using NUnit.Framework;

namespace FanControl.DeepCoolDigital.Tests
{
    /// <summary>
    /// Covers the AK DIGITAL packet layout: three digits, usage bar, Fahrenheit support, alert byte
    /// and the "SE" variant that omits the report id.
    /// </summary>
    [TestFixture]
    [Category("Protocols")]
    public class AkProtocolTests
    {
        [Test]
        public void PacketLength_IsAlways64()
        {
            Assert.That(CreateProtocol().PacketLength, Is.EqualTo(64));
        }

        [Test]
        public void IncludesReportId_IsTrue()
        {
            Assert.That(CreateProtocol().IncludesReportId, Is.True);
        }

        [Test]
        public void CreateInitializationPackets_ReturnsSingleAcknowledgedPacket()
        {
            byte[][] packets = CreateProtocol().CreateInitializationPackets();

            Assert.That(packets, Has.Length.EqualTo(1));

            var expected = new byte[64];
            expected[0] = 0x10;
            expected[1] = 0xAA;
            Assert.That(packets[0], Is.EqualTo(expected));
        }

        [Test]
        public void BuildPacket_Temperature42WithUsage5_ProducesGoldenBytes()
        {
            byte[] packet = CreateProtocol().BuildPacket(
                new DisplayFrame(DisplayField.Temperature, 42f, 42f, 5f, false));

            var expected = new byte[64];
            expected[0] = 0x10;
            expected[1] = 19;
            expected[2] = 1;
            expected[4] = 4;
            expected[5] = 2;

            Assert.That(packet, Is.EqualTo(expected));
        }

        [Test]
        public void BuildPacket_Usage87_ProducesGoldenBytes()
        {
            byte[] packet = CreateProtocol().BuildPacket(
                new DisplayFrame(DisplayField.Usage, 87f, 42f, 87f, false));

            var expected = new byte[64];
            expected[0] = 0x10;
            expected[1] = 76;
            expected[2] = 9;
            expected[4] = 8;
            expected[5] = 7;

            Assert.That(packet, Is.EqualTo(expected));
        }

        [Test]
        public void BuildPacket_FahrenheitOption_UsesFahrenheitStatusByte()
        {
            byte[] packet = CreateProtocol(fahrenheit: true).BuildPacket(
                new DisplayFrame(DisplayField.Temperature, 108f, 42f, 5f, false));

            Assert.That(packet[1], Is.EqualTo(35));
        }

        [TestCase(0f, 1)]
        [TestCase(14f, 1)]
        [TestCase(15f, 2)]
        [TestCase(50f, 5)]
        [TestCase(87f, 9)]
        [TestCase(95f, 10)]
        [TestCase(100f, 10)]
        [TestCase(float.NaN, 1)]
        public void BuildPacket_UsageBar_MapsUsageToLevel(float usage, byte expectedLevel)
        {
            byte[] packet = CreateProtocol().BuildPacket(
                new DisplayFrame(DisplayField.Temperature, 42f, 42f, usage, false));

            Assert.That(packet[2], Is.EqualTo(expectedLevel));
        }

        [TestCase(0f, 0, 0, 0)]
        [TestCase(42f, 0, 4, 2)]
        [TestCase(100f, 1, 0, 0)]
        [TestCase(999f, 9, 9, 9)]
        [TestCase(1000f, 9, 9, 9)]
        [TestCase(-5f, 0, 0, 0)]
        [TestCase(float.NaN, 0, 0, 0)]
        public void BuildPacket_Digits_UseThreeDigitFields(float value, byte hundreds, byte tens, byte ones)
        {
            byte[] packet = CreateProtocol().BuildPacket(
                new DisplayFrame(DisplayField.Temperature, value, 42f, 5f, false));

            Assert.That(packet[3], Is.EqualTo(hundreds));
            Assert.That(packet[4], Is.EqualTo(tens));
            Assert.That(packet[5], Is.EqualTo(ones));
        }

        [TestCase(true, 1)]
        [TestCase(false, 0)]
        public void BuildPacket_AlarmFlag_IsWrittenToByte6(bool alarm, byte expected)
        {
            byte[] packet = CreateProtocol().BuildPacket(
                new DisplayFrame(DisplayField.Temperature, 42f, 95f, 5f, alarm));

            Assert.That(packet[6], Is.EqualTo(expected));
        }

        [Test]
        public void ApplyTransportQuirks_AkProductName_ReturnsSameInstance()
        {
            IDisplayProtocol protocol = CreateProtocol();
            byte[] packet = protocol.BuildPacket(new DisplayFrame(DisplayField.Temperature, 42f, 42f, 5f, false));

            byte[] result = protocol.ApplyTransportQuirks(packet, "AK620 DIGITAL");

            Assert.That(result, Is.SameAs(packet));
        }

        [Test]
        public void ApplyTransportQuirks_SeProductName_StripsReportId()
        {
            IDisplayProtocol protocol = CreateProtocol();
            byte[] packet = protocol.BuildPacket(new DisplayFrame(DisplayField.Temperature, 42f, 42f, 5f, false));

            byte[] result = protocol.ApplyTransportQuirks(packet, "A400 DIGITAL");

            Assert.That(result, Has.Length.EqualTo(64));
            Assert.That(result[0], Is.EqualTo(19));
            Assert.That(result[1], Is.EqualTo(1));
            Assert.That(result[3], Is.EqualTo(4));
            Assert.That(result[4], Is.EqualTo(2));
            Assert.That(result[63], Is.EqualTo(0));
        }

        [Test]
        public void ApplyTransportQuirks_NullProductName_ReturnsSameInstance()
        {
            IDisplayProtocol protocol = CreateProtocol();
            byte[] packet = protocol.BuildPacket(new DisplayFrame(DisplayField.Temperature, 42f, 42f, 5f, false));

            Assert.That(protocol.ApplyTransportQuirks(packet, null), Is.SameAs(packet));
        }

        private static AkProtocol CreateProtocol(bool fahrenheit = false)
        {
            return new AkProtocol(new DeviceOptions(fahrenheit));
        }
    }
}
