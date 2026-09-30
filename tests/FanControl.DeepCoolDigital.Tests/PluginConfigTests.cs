using System;
using System.IO;
using FanControl.DeepCoolDigital.Core;
using NUnit.Framework;

namespace FanControl.DeepCoolDigital.Tests
{
    /// <summary>
    /// Covers parsing, validation and hot-reload of the DeepCoolDigital.ini configuration.
    /// </summary>
    [TestFixture]
    [Category("Configuration")]
    public class PluginConfigTests
    {
        private string _path;

        [SetUp]
        public void SetUp()
        {
            _path = Path.Combine(Path.GetTempPath(), "DeepCoolDigital-" + Guid.NewGuid().ToString("N") + ".ini");
        }

        [TearDown]
        public void TearDown()
        {
            if (File.Exists(_path))
            {
                File.Delete(_path);
            }
        }

        [Test]
        public void Load_MissingFile_ReturnsDefaults()
        {
            PluginConfig config = PluginConfig.Load(_path);

            Assert.That(config.Mode, Is.EqualTo(DisplayMode.Temperature));
            Assert.That(config.AutoSwitchSeconds, Is.EqualTo(5));
            Assert.That(config.AlarmTemperature, Is.EqualTo(90f));
            Assert.That(config.VendorId, Is.EqualTo(0x3633));
            Assert.That(config.ProductId, Is.EqualTo(0x0008));
            Assert.That(config.LogLevel, Is.EqualTo(LogLevel.Off));
            Assert.That(config.PreferredTemperatureSensors, Is.EqualTo(PluginConfig.DefaultPreferredTemperatureSensors));
            Assert.That(config.FilePath, Is.EqualTo(_path));
        }

        [TestCase("temp", DisplayMode.Temperature)]
        [TestCase("temperature", DisplayMode.Temperature)]
        [TestCase("usage", DisplayMode.Usage)]
        [TestCase("load", DisplayMode.Usage)]
        [TestCase("dynamic", DisplayMode.Auto)]
        [TestCase("both", DisplayMode.Auto)]
        [TestCase("auto", DisplayMode.Auto)]
        public void Load_ModeValue_MapsToExpectedMode(string value, DisplayMode expected)
        {
            PluginConfig config = LoadWith($"mode={value}");

            Assert.That(config.Mode, Is.EqualTo(expected));
        }

        [Test]
        public void Load_InvalidValues_FallBackToDefaults()
        {
            PluginConfig config = LoadWith("mode=banana\nautoSwitchSeconds=abc\nalarmTemperature=xyz\nvendorId=zzz");

            Assert.That(config.Mode, Is.EqualTo(DisplayMode.Temperature));
            Assert.That(config.AutoSwitchSeconds, Is.EqualTo(5));
            Assert.That(config.AlarmTemperature, Is.EqualTo(90f));
            Assert.That(config.VendorId, Is.EqualTo(0x3633));
        }

        [Test]
        public void Load_HexVendorAndProduct_ParsesValues()
        {
            PluginConfig config = LoadWith("vendorId=0x3634\nproductId=0x0009");

            Assert.That(config.VendorId, Is.EqualTo(0x3634));
            Assert.That(config.ProductId, Is.EqualTo(0x0009));
        }

        [Test]
        public void Load_ValidFile_ParsesAllValues()
        {
            PluginConfig config = LoadWith("mode=dynamic\nautoSwitchSeconds=10\nalarmTemperature=85\n");

            Assert.That(config.Mode, Is.EqualTo(DisplayMode.Auto));
            Assert.That(config.AutoSwitchSeconds, Is.EqualTo(10));
            Assert.That(config.AlarmTemperature, Is.EqualTo(85f));
        }

        [Test]
        public void Load_AutoSwitchSecondsBelowOne_IsClampedToOne()
        {
            PluginConfig config = LoadWith("autoSwitchSeconds=0");

            Assert.That(config.AutoSwitchSeconds, Is.EqualTo(1));
        }

        [Test]
        public void Load_LegacyLogTrue_MapsToEventsLevel()
        {
            PluginConfig config = LoadWith("log=true");

            Assert.That(config.LogLevel, Is.EqualTo(LogLevel.Events));
        }

        [Test]
        public void Load_LogLevelVerbose_OverridesLegacyLog()
        {
            PluginConfig config = LoadWith("log=true\nlogLevel=verbose");

            Assert.That(config.LogLevel, Is.EqualTo(LogLevel.Verbose));
        }

        [Test]
        public void Load_LogLevelOff_OverridesLegacyLogTrue()
        {
            PluginConfig config = LoadWith("log=true\nlogLevel=off");

            Assert.That(config.LogLevel, Is.EqualTo(LogLevel.Off));
        }

        [Test]
        public void Load_PreferredTemperatureSensors_ParsesPipeSeparatedList()
        {
            PluginConfig config = LoadWith("preferredTempSensors=Alpha| Beta ||Gamma");

            Assert.That(config.PreferredTemperatureSensors, Is.EqualTo(new[] { "Alpha", "Beta", "Gamma" }));
        }

        [Test]
        public void Load_PreferredTemperatureSensorsEmpty_FallsBackToDefaults()
        {
            PluginConfig config = LoadWith("preferredTempSensors=||");

            Assert.That(config.PreferredTemperatureSensors, Is.EqualTo(PluginConfig.DefaultPreferredTemperatureSensors));
        }

        [Test]
        public void Load_NullPath_ThrowsArgumentException()
        {
            Assert.Throws<ArgumentException>(() => PluginConfig.Load(null));
        }

        [Test]
        public void TryReload_UnchangedFile_ReturnsFalse()
        {
            PluginConfig config = LoadWith("mode=temp");

            Assert.That(config.TryReload(), Is.False);
        }

        [Test]
        public void TryReload_ChangedFile_AppliesNewValues()
        {
            PluginConfig config = LoadWith("mode=temp");

            File.WriteAllText(_path, "mode=usage\nautoSwitchSeconds=7");
            File.SetLastWriteTimeUtc(_path, DateTime.UtcNow.AddSeconds(10));

            bool changed = config.TryReload();

            Assert.That(changed, Is.True);
            Assert.That(config.Mode, Is.EqualTo(DisplayMode.Usage));
            Assert.That(config.AutoSwitchSeconds, Is.EqualTo(7));
        }

        [Test]
        public void TryReload_FileWithOnlyUnknownKeys_ReportsNoChange()
        {
            PluginConfig config = LoadWith("mode=temp");

            File.WriteAllText(_path, "mode=temp\nunknownKey=whatever");
            File.SetLastWriteTimeUtc(_path, DateTime.UtcNow.AddSeconds(10));

            Assert.That(config.TryReload(), Is.False);
        }

        private PluginConfig LoadWith(string content)
        {
            File.WriteAllText(_path, content);
            return PluginConfig.Load(_path);
        }
    }
}
