using System;
using FanControl.DeepCoolDigital.Core;
using NUnit.Framework;

namespace FanControl.DeepCoolDigital.Tests
{
    /// <summary>
    /// Covers the selection of the CPU temperature sensor used for the cooler display.
    /// </summary>
    [TestFixture]
    [Category("Providers")]
    public class TemperatureSensorSelectorTests
    {
        private static readonly string[] Preferred = { "CPU Package", "Core (Tctl/Tdie)" };

        [Test]
        public void Select_PreferredSensorPresent_ReturnsItEvenWhenItIsNotTheHighest()
        {
            var samples = new[]
            {
                new SensorSample("Core #1", 75f),
                new SensorSample("CPU Package", 60f)
            };

            Assert.That(TemperatureSensorSelector.Select(samples, Preferred), Is.EqualTo(60f));
        }

        [Test]
        public void Select_PreferredMatching_IsCaseInsensitive()
        {
            var samples = new[] { new SensorSample("cpu package", 61f) };

            Assert.That(TemperatureSensorSelector.Select(samples, Preferred), Is.EqualTo(61f));
        }

        [Test]
        public void Select_MultiplePreferredSensors_RespectsPriorityOrder()
        {
            var samples = new[]
            {
                new SensorSample("Core (Tctl/Tdie)", 70f),
                new SensorSample("CPU Package", 65f)
            };

            Assert.That(TemperatureSensorSelector.Select(samples, Preferred), Is.EqualTo(65f));
        }

        [Test]
        public void Select_NoPreferredSensorPresent_ReturnsHighestValidReading()
        {
            var samples = new[]
            {
                new SensorSample("Core #1", 55f),
                new SensorSample("Core #2", 70f)
            };

            Assert.That(TemperatureSensorSelector.Select(samples, Preferred), Is.EqualTo(70f));
        }

        [Test]
        public void Select_InvalidReadings_AreIgnored()
        {
            var samples = new[]
            {
                new SensorSample("Zero", 0f),
                new SensorSample("Negative", -3f),
                new SensorSample("NaN", float.NaN),
                new SensorSample("Valid", 50f)
            };

            Assert.That(TemperatureSensorSelector.Select(samples, Preferred), Is.EqualTo(50f));
        }

        [Test]
        public void Select_PreferredSensorWithInvalidReading_FallsThroughToNextCandidate()
        {
            var samples = new[]
            {
                new SensorSample("CPU Package", float.NaN),
                new SensorSample("Core (Tctl/Tdie)", 64f)
            };

            Assert.That(TemperatureSensorSelector.Select(samples, Preferred), Is.EqualTo(64f));
        }

        [Test]
        public void Select_NoValidReadings_ReturnsNull()
        {
            var samples = new[] { new SensorSample("Zero", 0f) };

            Assert.That(TemperatureSensorSelector.Select(samples, Preferred), Is.Null);
        }

        [Test]
        public void Select_EmptySampleList_ReturnsNull()
        {
            Assert.That(TemperatureSensorSelector.Select(new SensorSample[0], Preferred), Is.Null);
        }

        [Test]
        public void Select_NullPreferredNames_FallsBackToHighestReading()
        {
            var samples = new[] { new SensorSample("Core #1", 42f) };

            Assert.That(TemperatureSensorSelector.Select(samples, null), Is.EqualTo(42f));
        }

        [Test]
        public void Select_NullSamples_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() => TemperatureSensorSelector.Select(null, Preferred));
        }

        [Test]
        public void SelectSample_PreferredSensorPresent_ReturnsSampleWithName()
        {
            var samples = new[] { new SensorSample("CPU Package", 60f) };

            SensorSample? sample = TemperatureSensorSelector.SelectSample(samples, Preferred);

            Assert.That(sample.HasValue, Is.True);
            Assert.That(sample.Value.Name, Is.EqualTo("CPU Package"));
            Assert.That(sample.Value.Value, Is.EqualTo(60f));
        }
    }
}
