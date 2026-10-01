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
            Assert.That(config.AlarmEnabled, Is.True);
            Assert.That(config.Fahrenheit, Is.False);
            Assert.That(config.VendorId, Is.EqualTo(0x3633));
            Assert.That(config.ProductId, Is.EqualTo(0x0008));
            Assert.That(config.LogLevel, Is.EqualTo(LogLevel.Off));
            Assert.That(config.PreferredTemperatureSensors, Is.EqualTo(PluginConfig.DefaultPreferredTemperatureSensors));
            Assert.That(config.DeviceOverrides, Is.Empty);
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
        public void Load_GlobalAlarmAndUnitKeys_ParseValues()
        {
            PluginConfig config = LoadWith("alarmEnabled=false\nfahrenheit=true");

            Assert.That(config.AlarmEnabled, Is.False);
            Assert.That(config.Fahrenheit, Is.True);
        }

        [Test]
        public void Load_Defaults_UseAutoSourceAndCpuTotalUsageSensor()
        {
            PluginConfig config = PluginConfig.Load(_path);

            Assert.That(config.Source, Is.EqualTo(SensorSource.Auto));
            Assert.That(config.UsageSensor, Is.EqualTo("CPU Total"));
        }

        [TestCase("auto", SensorSource.Auto)]
        [TestCase("fancontrol", SensorSource.FanControl)]
        [TestCase("ipc", SensorSource.FanControl)]
        [TestCase("local", SensorSource.Local)]
        [TestCase("lhm", SensorSource.Local)]
        public void Load_SensorSourceValue_MapsToExpectedSource(string value, SensorSource expected)
        {
            PluginConfig config = LoadWith($"sensorSource={value}");

            Assert.That(config.Source, Is.EqualTo(expected));
        }

        [Test]
        public void Load_InvalidSensorSource_FallsBackToAuto()
        {
            PluginConfig config = LoadWith("sensorSource=banana");

            Assert.That(config.Source, Is.EqualTo(SensorSource.Auto));
        }

        [Test]
        public void Load_UsageSensor_ParsesValue()
        {
            PluginConfig config = LoadWith("usageSensor=CPU Core Max");

            Assert.That(config.UsageSensor, Is.EqualTo("CPU Core Max"));
        }

        [Test]
        public void TryReload_SensorSourceChange_IsReportedAndApplied()
        {
            PluginConfig config = LoadWith("sensorSource=auto");

            File.WriteAllText(_path, "sensorSource=local");
            File.SetLastWriteTimeUtc(_path, DateTime.UtcNow.AddSeconds(10));

            Assert.That(config.TryReload(), Is.True);
            Assert.That(config.Source, Is.EqualTo(SensorSource.Local));
        }

        [Test]
        public void ForDevice_NoOverride_ReturnsGlobals()
        {
            PluginConfig config = LoadWith("mode=usage\nalarmEnabled=false");

            DeviceSettings settings = config.ForDevice(0x3633, 0x0002);

            Assert.That(settings.Mode, Is.EqualTo(DisplayMode.Usage));
            Assert.That(settings.AlarmEnabled, Is.False);
            Assert.That(settings.AlarmTemperature, Is.EqualTo(90f));
            Assert.That(settings.Fahrenheit, Is.False);
            Assert.That(settings.VendorId, Is.EqualTo(0x3633));
            Assert.That(settings.ProductId, Is.EqualTo(0x0002));
        }

        [Test]
        public void ForDevice_DeviceSection_MergesOverridesWithGlobals()
        {
            PluginConfig config = LoadWith(
                "mode=temp\nalarmTemperature=90\n\n[device:0x3633:0x0002]\nmode=usage\nalarmTemperature=85\n");

            DeviceSettings overridden = config.ForDevice(0x3633, 0x0002);
            DeviceSettings untouched = config.ForDevice(0x3633, 0x0008);

            Assert.That(overridden.Mode, Is.EqualTo(DisplayMode.Usage));
            Assert.That(overridden.AlarmTemperature, Is.EqualTo(85f));
            Assert.That(untouched.Mode, Is.EqualTo(DisplayMode.Temperature));
            Assert.That(untouched.AlarmTemperature, Is.EqualTo(90f));
        }

        [Test]
        public void ForDevice_PartialSection_InheritsRemainingGlobals()
        {
            PluginConfig config = LoadWith("fahrenheit=true\n\n[device:0x3633:0x0002]\nmode=dynamic\n");

            DeviceSettings settings = config.ForDevice(0x3633, 0x0002);

            Assert.That(settings.Mode, Is.EqualTo(DisplayMode.Auto));
            Assert.That(settings.Fahrenheit, Is.True);
            Assert.That(settings.AlarmEnabled, Is.True);
        }

        [Test]
        public void ForDevice_DecimalSectionIds_AreParsed()
        {
            PluginConfig config = LoadWith("[device:13875:2]\nmode=usage\n");

            Assert.That(config.ForDevice(13875, 2).Mode, Is.EqualTo(DisplayMode.Usage));
        }

        [Test]
        public void Load_MalformedSection_SkipsItsKeys()
        {
            PluginConfig config = LoadWith("[device:zzz:2]\nmode=usage\n");

            Assert.That(config.Mode, Is.EqualTo(DisplayMode.Temperature));
            Assert.That(config.DeviceOverrides, Is.Empty);
        }

        [Test]
        public void Load_SectionKeys_ArePartOfTheOverrideList()
        {
            PluginConfig config = LoadWith("[device:0x3633:0x0001]\nalarmEnabled=false\nfahrenheit=true\n");

            Assert.That(config.DeviceOverrides, Has.Count.EqualTo(1));
            Assert.That(config.DeviceOverrides[0].VendorId, Is.EqualTo(0x3633));
            Assert.That(config.DeviceOverrides[0].ProductId, Is.EqualTo(0x0001));
            Assert.That(config.DeviceOverrides[0].AlarmEnabled, Is.False);
            Assert.That(config.DeviceOverrides[0].Fahrenheit, Is.True);
            Assert.That(config.DeviceOverrides[0].Mode, Is.Null);
        }

        [Test]
        public void Load_DeviceSectionInvalidValue_InheritsGlobal()
        {
            PluginConfig config = LoadWith("mode=usage\n\n[device:0x3633:0x0002]\nmode=banana\nalarmTemperature=abc\n");

            DeviceSettings settings = config.ForDevice(0x3633, 0x0002);

            Assert.That(settings.Mode, Is.EqualTo(DisplayMode.Usage));
            Assert.That(settings.AlarmTemperature, Is.EqualTo(90f));
            Assert.That(config.DeviceOverrides[0].Mode, Is.Null);
        }

        [Test]
        public void Load_SectionHeaderWithTrailingComment_IsParsed()
        {
            PluginConfig config = LoadWith("[device:0x3633:0x0002] # AK620 DIGITAL\nmode=usage\n");

            Assert.That(config.ForDevice(0x3633, 0x0002).Mode, Is.EqualTo(DisplayMode.Usage));
        }

        [Test]
        public void TryReload_SectionChange_AppliesNewOverrides()
        {
            PluginConfig config = LoadWith("mode=temp");

            File.WriteAllText(_path, "mode=temp\n\n[device:0x3633:0x0002]\nmode=usage\n");
            File.SetLastWriteTimeUtc(_path, DateTime.UtcNow.AddSeconds(10));

            Assert.That(config.TryReload(), Is.True);
            Assert.That(config.ForDevice(0x3633, 0x0002).Mode, Is.EqualTo(DisplayMode.Usage));
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

        [Test]
        public void TryReload_FileLocked_KeepsPreviousValuesAndRetriesLater()
        {
            PluginConfig config = LoadWith("mode=usage");

            File.WriteAllText(_path, "mode=temp");
            File.SetLastWriteTimeUtc(_path, DateTime.UtcNow.AddSeconds(10));

            using (new FileStream(_path, FileMode.Open, FileAccess.Read, FileShare.None))
            {
                Assert.That(config.TryReload(), Is.False);
            }

            Assert.That(config.Mode, Is.EqualTo(DisplayMode.Usage));

            Assert.That(config.TryReload(), Is.True);
            Assert.That(config.Mode, Is.EqualTo(DisplayMode.Temperature));
        }

        private PluginConfig LoadWith(string content)
        {
            File.WriteAllText(_path, content);
            return PluginConfig.Load(_path);
        }
    }
}
