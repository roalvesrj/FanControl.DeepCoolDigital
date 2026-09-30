using FanControl.DeepCoolDigital.Core;
using FanControl.DeepCoolDigital.Core.Protocols;
using NUnit.Framework;

namespace FanControl.DeepCoolDigital.Tests
{
    /// <summary>
    /// Covers the pure session decision logic: field selection, alternation timing, unit conversion
    /// and alert gating.
    /// </summary>
    [TestFixture]
    [Category("Policy")]
    public class DisplayPolicyTests
    {
        [Test]
        public void SelectField_ShowUsageTrue_ReturnsUsage()
        {
            Assert.That(DisplayPolicy.SelectField(true), Is.EqualTo(DisplayField.Usage));
        }

        [Test]
        public void SelectField_ShowUsageFalse_ReturnsTemperature()
        {
            Assert.That(DisplayPolicy.SelectField(false), Is.EqualTo(DisplayField.Temperature));
        }

        [TestCase(5000, 5, true)]
        [TestCase(5001, 5, true)]
        [TestCase(4999, 5, false)]
        [TestCase(0, 1, false)]
        [TestCase(1000, 1, true)]
        [TestCase(int.MaxValue, 3_000_000, false)]
        public void ShouldSwitchMode_ComparesIn64Bit(int elapsedMilliseconds, int autoSwitchSeconds, bool expected)
        {
            Assert.That(DisplayPolicy.ShouldSwitchMode(elapsedMilliseconds, autoSwitchSeconds), Is.EqualTo(expected));
        }

        [Test]
        public void ConvertTemperature_CelsiusConfigured_ReturnsCelsius()
        {
            Assert.That(DisplayPolicy.ConvertTemperature(42f, false, true), Is.EqualTo(42f));
        }

        [Test]
        public void ConvertTemperature_FahrenheitConfiguredAndSupported_Converts()
        {
            Assert.That(DisplayPolicy.ConvertTemperature(42f, true, true), Is.EqualTo(107.6f).Within(0.001f));
        }

        [Test]
        public void ConvertTemperature_FahrenheitConfiguredButUnsupported_ReturnsCelsius()
        {
            Assert.That(DisplayPolicy.ConvertTemperature(42f, true, false), Is.EqualTo(42f));
        }

        [TestCase(95f, true, true, 95f, true)]
        [TestCase(95.1f, true, true, 95f, true)]
        [TestCase(94.9f, true, true, 95f, false)]
        [TestCase(100f, false, true, 95f, false)]
        [TestCase(100f, true, false, 95f, false)]
        public void IsAlarm_GatesOnSwitchCapabilityAndThreshold(
            float temperatureCelsius,
            bool alarmEnabled,
            bool supportsAlarm,
            float thresholdCelsius,
            bool expected)
        {
            bool result = DisplayPolicy.IsAlarm(temperatureCelsius, alarmEnabled, supportsAlarm, thresholdCelsius);

            Assert.That(result, Is.EqualTo(expected));
        }
    }
}
