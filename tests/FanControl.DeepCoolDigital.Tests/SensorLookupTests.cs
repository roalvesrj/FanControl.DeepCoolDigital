using System;
using FanControl.DeepCoolDigital.Core;
using NUnit.Framework;

namespace FanControl.DeepCoolDigital.Tests
{
    /// <summary>
    /// Covers the identifier/name matching used by the FanControl IPC source to read values.
    /// </summary>
    [TestFixture]
    [Category("Providers")]
    public class SensorLookupTests
    {
        [Test]
        public void TryGetValue_IdentifierMatch_ReturnsValue()
        {
            var samples = new[]
            {
                new SensorSample("Core (Tctl/Tdie)", 44f, "/amdcpu/0/temperature/2"),
                new SensorSample("CPU Total", 3.1f, "/amdcpu/0/load/0")
            };

            bool found = SensorLookup.TryGetValue(samples, "/amdcpu/0/temperature/2", out float value);

            Assert.That(found, Is.True);
            Assert.That(value, Is.EqualTo(44f));
        }

        [Test]
        public void TryGetValue_EmptyIdentifier_FallsBackToNameMatch()
        {
            var samples = new[]
            {
                new SensorSample("CPU Total", 12f, string.Empty)
            };

            bool found = SensorLookup.TryGetValue(samples, "CPU Total", out float value);

            Assert.That(found, Is.True);
            Assert.That(value, Is.EqualTo(12f));
        }

        [Test]
        public void TryGetValue_NameMatch_IsCaseInsensitive()
        {
            var samples = new[] { new SensorSample("CPU Total", 12f) };

            Assert.That(SensorLookup.TryGetValue(samples, "cpu total", out _), Is.True);
        }

        [Test]
        public void TryGetValue_IdentifierMatch_IsCaseSensitive()
        {
            var samples = new[] { new SensorSample("Core", 44f, "/amdcpu/0/temperature/2") };

            Assert.That(SensorLookup.TryGetValue(samples, "/AMDCPU/0/temperature/2", out _), Is.False);
        }

        [Test]
        public void TryGetValue_IdentifierAndNameMatch_IdentifierWins()
        {
            var samples = new[]
            {
                new SensorSample("CPU Total", 1f),
                new SensorSample("Other", 99f, "/amdcpu/0/load/0")
            };

            SensorLookup.TryGetValue(samples, "/amdcpu/0/load/0", out float value);

            Assert.That(value, Is.EqualTo(99f));
        }

        [Test]
        public void TryGetValue_MissingSensor_ReturnsFalse()
        {
            var samples = new[] { new SensorSample("CPU Total", 12f, "/amdcpu/0/load/0") };

            Assert.That(SensorLookup.TryGetValue(samples, "/amdcpu/0/temperature/2", out float value), Is.False);
            Assert.That(value, Is.EqualTo(0f));
        }

        [TestCase(null)]
        [TestCase("")]
        public void TryGetValue_EmptyNameOrIdentifier_ReturnsFalse(string nameOrIdentifier)
        {
            var samples = new[] { new SensorSample("CPU Total", 12f) };

            Assert.That(SensorLookup.TryGetValue(samples, nameOrIdentifier, out _), Is.False);
        }

        [Test]
        public void TryGetValue_DuplicateIdentifiers_ReturnsFirstMatch()
        {
            var samples = new[]
            {
                new SensorSample("CPU Total", 12f, "/amdcpu/0/load/0"),
                new SensorSample("CPU Total Copy", 99f, "/amdcpu/0/load/0")
            };

            bool found = SensorLookup.TryGetValue(samples, "/amdcpu/0/load/0", out float value);

            Assert.That(found, Is.True);
            Assert.That(value, Is.EqualTo(12f));
        }

        [Test]
        public void TryGetValue_NullSamples_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() => SensorLookup.TryGetValue(null, "CPU Total", out _));
        }

        [TestCase("Core (Tctl/Tdie)", "/amdcpu/0/temperature/2", "/amdcpu/0/temperature/2")]
        [TestCase("CPU Total", "", "CPU Total")]
        [TestCase("CPU Total", null, "CPU Total")]
        public void ResolveKey_UsesIdentifierWhenPresentOtherwiseName(string name, string identifier, string expected)
        {
            string key = SensorLookup.ResolveKey(new SensorSample(name, 1f, identifier));

            Assert.That(key, Is.EqualTo(expected));
        }
    }
}
