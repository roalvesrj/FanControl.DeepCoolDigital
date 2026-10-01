using FanControl.DeepCoolDigital.Core;
using NUnit.Framework;

namespace FanControl.DeepCoolDigital.Tests
{
    /// <summary>
    /// Covers the plausibility bounds applied to values received from external sources.
    /// </summary>
    [TestFixture]
    [Category("Providers")]
    public class SensorValueValidatorTests
    {
        [TestCase(-20f, true)]
        [TestCase(0f, true)]
        [TestCase(42f, true)]
        [TestCase(150f, true)]
        [TestCase(-20.1f, false)]
        [TestCase(150.1f, false)]
        [TestCase(float.NaN, false)]
        [TestCase(float.PositiveInfinity, false)]
        [TestCase(float.NegativeInfinity, false)]
        public void IsPlausibleTemperature_ChecksRange(float celsius, bool expected)
        {
            Assert.That(SensorValueValidator.IsPlausibleTemperature(celsius), Is.EqualTo(expected));
        }

        [TestCase(0f, true)]
        [TestCase(3.1f, true)]
        [TestCase(100f, true)]
        [TestCase(-0.1f, false)]
        [TestCase(100.1f, false)]
        [TestCase(float.NaN, false)]
        [TestCase(float.PositiveInfinity, false)]
        [TestCase(float.NegativeInfinity, false)]
        public void IsPlausibleUsage_ChecksRange(float percent, bool expected)
        {
            Assert.That(SensorValueValidator.IsPlausibleUsage(percent), Is.EqualTo(expected));
        }
    }
}
