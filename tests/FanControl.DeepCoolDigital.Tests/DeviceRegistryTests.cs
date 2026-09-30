using FanControl.DeepCoolDigital.Core;
using FanControl.DeepCoolDigital.Core.Protocols;
using NUnit.Framework;

namespace FanControl.DeepCoolDigital.Tests
{
    /// <summary>
    /// Covers the device registry lookups used to map USB identities to protocols.
    /// </summary>
    [TestFixture]
    [Category("Registry")]
    public class DeviceRegistryTests
    {
        [Test]
        public void Find_AgDigitalIdentity_ReturnsDefinitionWithExpectedCapabilities()
        {
            DeviceDefinition definition = DeviceRegistry.Find(0x3633, 0x0008);

            Assert.That(definition, Is.Not.Null);
            Assert.That(definition.Model, Is.EqualTo("DeepCool AG DIGITAL"));
            Assert.That(definition.Capabilities.SupportsTemperature, Is.True);
            Assert.That(definition.Capabilities.SupportsUsage, Is.True);
            Assert.That(definition.Capabilities.SupportsAlarm, Is.True);
            Assert.That(definition.Capabilities.SupportsFahrenheit, Is.False);
            Assert.That(definition.Capabilities.DefaultAlarmTemperatureCelsius, Is.EqualTo(90f));
        }

        [Test]
        public void Find_UnknownProductId_ReturnsNull()
        {
            Assert.That(DeviceRegistry.Find(DeviceRegistry.DeepCoolVendorId, 0x9999), Is.Null);
        }

        [Test]
        public void Find_UnknownVendorId_ReturnsNull()
        {
            Assert.That(DeviceRegistry.Find(0x1234, 0x0008), Is.Null);
        }

        [Test]
        public void AgDigital_CreateProtocol_ReturnsAgProtocol()
        {
            Assert.That(DeviceRegistry.AgDigital.CreateProtocol(), Is.InstanceOf<AgProtocol>());
        }

        [Test]
        public void AgDigital_CreateProtocol_ReturnsNewInstanceEachTime()
        {
            IDisplayProtocol first = DeviceRegistry.AgDigital.CreateProtocol();
            IDisplayProtocol second = DeviceRegistry.AgDigital.CreateProtocol();

            Assert.That(first, Is.Not.SameAs(second));
        }

        [Test]
        public void All_ContainsTheAgDigitalDefinition()
        {
            Assert.That(DeviceRegistry.All, Does.Contain(DeviceRegistry.AgDigital));
        }
    }
}
