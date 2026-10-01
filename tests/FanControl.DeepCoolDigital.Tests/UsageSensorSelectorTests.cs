using System;
using System.Collections.Generic;
using FanControl.DeepCoolDigital.Core;
using NUnit.Framework;

namespace FanControl.DeepCoolDigital.Tests
{
    /// <summary>
    /// Covers the selection of the CPU usage sensor from FanControl's sensor list.
    /// </summary>
    [TestFixture]
    [Category("Providers")]
    public class UsageSensorSelectorTests
    {
        private static readonly SensorSample[] TypicalSensors =
        {
            new SensorSample("CPU Core #1", 14f, "/amdcpu/0/load/2"),
            new SensorSample("CPU Core Max", 15f, "/amdcpu/0/load/1"),
            new SensorSample("CPU Total", 3.1f, "/amdcpu/0/load/0")
        };

        [Test]
        public void Select_PreferredName_ReturnsIt()
        {
            SensorSample? sample = UsageSensorSelector.Select(TypicalSensors, "CPU Core Max");

            Assert.That(sample.HasValue, Is.True);
            Assert.That(sample.Value.Name, Is.EqualTo("CPU Core Max"));
        }

        [Test]
        public void Select_PreferredIdentifier_ReturnsIt()
        {
            SensorSample? sample = UsageSensorSelector.Select(TypicalSensors, "/amdcpu/0/load/2");

            Assert.That(sample.HasValue, Is.True);
            Assert.That(sample.Value.Name, Is.EqualTo("CPU Core #1"));
        }

        [Test]
        public void Select_NoPreference_ReturnsWellKnownCpuTotal()
        {
            SensorSample? sample = UsageSensorSelector.Select(TypicalSensors, null);

            Assert.That(sample.HasValue, Is.True);
            Assert.That(sample.Value.Name, Is.EqualTo("CPU Total"));
            Assert.That(sample.Value.Value, Is.EqualTo(3.1f).Within(0.001f));
        }

        [Test]
        public void Select_UnknownPreference_FallsBackToCpuTotal()
        {
            SensorSample? sample = UsageSensorSelector.Select(TypicalSensors, "Does Not Exist");

            Assert.That(sample.HasValue, Is.True);
            Assert.That(sample.Value.Name, Is.EqualTo("CPU Total"));
        }

        [Test]
        public void Select_NoCpuTotal_ReturnsFirstTotalContainingSensor()
        {
            var samples = new[]
            {
                new SensorSample("CPU Core #1", 14f),
                new SensorSample("GPU Total", 30f)
            };

            SensorSample? sample = UsageSensorSelector.Select(samples, null);

            Assert.That(sample.HasValue, Is.True);
            Assert.That(sample.Value.Name, Is.EqualTo("GPU Total"));
        }

        [TestCase(120f)]
        [TestCase(-1f)]
        [TestCase(float.NaN)]
        public void Select_InvalidPreferredReading_IsIgnored(float invalid)
        {
            var samples = new[]
            {
                new SensorSample("CPU Total", invalid),
                new SensorSample("CPU Package Total", 15f)
            };

            SensorSample? sample = UsageSensorSelector.Select(samples, "CPU Total");

            Assert.That(sample.HasValue, Is.True);
            Assert.That(sample.Value.Name, Is.EqualTo("CPU Package Total"));
        }

        [Test]
        public void Select_OnlyNonTotalSensors_ReturnsNull()
        {
            var samples = new[] { new SensorSample("CPU Core Max", 15f) };

            Assert.That(UsageSensorSelector.Select(samples, null), Is.Null);
        }

        [Test]
        public void Select_NoValidReading_ReturnsNull()
        {
            var samples = new[] { new SensorSample("CPU Total", 150f) };

            Assert.That(UsageSensorSelector.Select(samples, null), Is.Null);
        }

        [Test]
        public void Select_EmptyList_ReturnsNull()
        {
            Assert.That(UsageSensorSelector.Select(new List<SensorSample>(), "CPU Total"), Is.Null);
        }

        [Test]
        public void Select_NullSamples_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() => UsageSensorSelector.Select(null, "CPU Total"));
        }
    }
}
