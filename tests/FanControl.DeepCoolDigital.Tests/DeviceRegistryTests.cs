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
        private static readonly DeviceOptions CelsiusOptions = new DeviceOptions(false);
        private static readonly DeviceOptions FahrenheitOptions = new DeviceOptions(true);

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

        [TestCase(0x0001, "DeepCool AK400 DIGITAL")]
        [TestCase(0x0002, "DeepCool AK620 DIGITAL")]
        [TestCase(0x0003, "DeepCool AK500 DIGITAL")]
        [TestCase(0x0004, "DeepCool AK500S DIGITAL")]
        public void Find_AkDigitalIdentities_ReturnDefinitionsWithFahrenheitSupport(int productId, string model)
        {
            DeviceDefinition definition = DeviceRegistry.Find(0x3633, productId);

            Assert.That(definition, Is.Not.Null);
            Assert.That(definition.Model, Is.EqualTo(model));
            Assert.That(definition.Capabilities.SupportsFahrenheit, Is.True);
            Assert.That(definition.Capabilities.SupportsAlarm, Is.True);
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
            Assert.That(DeviceRegistry.AgDigital.CreateProtocol(CelsiusOptions), Is.InstanceOf<AgProtocol>());
        }

        [Test]
        public void AkDigital_CreateProtocol_ReturnsAkProtocol()
        {
            Assert.That(DeviceRegistry.Ak620Digital.CreateProtocol(FahrenheitOptions), Is.InstanceOf<AkProtocol>());
        }

        [Test]
        public void CreateProtocol_ReturnsNewInstanceEachTime()
        {
            IDisplayProtocol first = DeviceRegistry.AgDigital.CreateProtocol(CelsiusOptions);
            IDisplayProtocol second = DeviceRegistry.AgDigital.CreateProtocol(CelsiusOptions);

            Assert.That(first, Is.Not.SameAs(second));
        }

        [Test]
        public void CreateProtocol_NullOptions_ThrowsArgumentNullException()
        {
            Assert.Throws<System.ArgumentNullException>(() => DeviceRegistry.AgDigital.CreateProtocol(null));
        }

        [Test]
        public void All_ContainsTheAgDigitalDefinition()
        {
            Assert.That(DeviceRegistry.All, Does.Contain(DeviceRegistry.AgDigital));
        }

        [Test]
        public void Resolve_KnownIdentity_ReturnsDefinitionWithoutFallback()
        {
            DeviceDefinition definition = DeviceRegistry.Resolve(0x3633, 0x0008, out bool usedFallback);

            Assert.That(definition, Is.SameAs(DeviceRegistry.AgDigital));
            Assert.That(usedFallback, Is.False);
        }

        [Test]
        public void Resolve_UnknownDeepCoolProductId_ReturnsAgFallback()
        {
            DeviceDefinition definition = DeviceRegistry.Resolve(DeviceRegistry.DeepCoolVendorId, 0x9999, out bool usedFallback);

            Assert.That(definition, Is.SameAs(DeviceRegistry.AgDigital));
            Assert.That(usedFallback, Is.True);
        }

        [Test]
        public void Resolve_UnknownVendor_ReturnsNullAndNoFallback()
        {
            DeviceDefinition definition = DeviceRegistry.Resolve(0x1234, 0x0008, out bool usedFallback);

            Assert.That(definition, Is.Null);
            Assert.That(usedFallback, Is.False);
        }
    }
}
