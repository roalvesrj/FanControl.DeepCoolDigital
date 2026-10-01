using System;
using System.Collections.Generic;
using FanControl.DeepCoolDigital.Core;
using NUnit.Framework;

namespace FanControl.DeepCoolDigital.Tests
{
    /// <summary>
    /// Covers the sensor source policy: primary preference, cooldown, fallback and retirement.
    /// </summary>
    [TestFixture]
    [Category("Providers")]
    public class SensorSourceManagerTests
    {
        private static readonly RetryBackoff Backoff = new RetryBackoff(30000);

        [Test]
        public void TryRead_AutoWithHealthyPrimary_ReturnsPrimaryValues()
        {
            var primary = new FakeSource(42f, 7f, () => true);
            var fallback = new FakeSource(55f, 9f, () => true);
            var manager = CreateManager(SensorSource.Auto, primary, fallback);

            bool result = manager.TryRead(1000, out float temperature, out float usage);

            Assert.That(result, Is.True);
            Assert.That(temperature, Is.EqualTo(42f));
            Assert.That(usage, Is.EqualTo(7f));
            Assert.That(primary.CallCount, Is.EqualTo(1));
            Assert.That(fallback.CallCount, Is.EqualTo(0));
        }

        [Test]
        public void TryRead_AutoWithFailingPrimary_ReturnsFallbackValues()
        {
            var primary = new FakeSource(42f, 7f, () => false);
            var fallback = new FakeSource(55f, 9f, () => true);
            var manager = CreateManager(SensorSource.Auto, primary, fallback);

            bool result = manager.TryRead(1000, out float temperature, out float usage);

            Assert.That(result, Is.True);
            Assert.That(temperature, Is.EqualTo(55f));
            Assert.That(usage, Is.EqualTo(9f));
            Assert.That(fallback.CallCount, Is.EqualTo(1));
        }

        [Test]
        public void TryRead_DuringCooldown_DoesNotRetryPrimary()
        {
            var primary = new FakeSource(42f, 7f, () => false);
            var fallback = new FakeSource(55f, 9f, () => true);
            var manager = CreateManager(SensorSource.Auto, primary, fallback);

            manager.TryRead(1000, out _, out _);
            manager.TryRead(20000, out _, out _);

            Assert.That(primary.CallCount, Is.EqualTo(1));
            Assert.That(fallback.CallCount, Is.EqualTo(2));
        }

        [Test]
        public void TryRead_AfterCooldown_RetriesPrimary()
        {
            var primary = new FakeSource(42f, 7f, () => false);
            var fallback = new FakeSource(55f, 9f, () => true);
            var manager = CreateManager(SensorSource.Auto, primary, fallback);

            manager.TryRead(1000, out _, out _);
            manager.TryRead(31000, out _, out _);

            Assert.That(primary.CallCount, Is.EqualTo(2));
        }

        [Test]
        public void TryRead_ThrowingPrimary_IsTreatedAsFailureAndFallsBack()
        {
            var fallback = new FakeSource(55f, 9f, () => true);
            var events = new List<string>();
            var manager = CreateManager(SensorSource.Auto, new ThrowingSource(), fallback, events: events);

            bool result = manager.TryRead(1000, out float temperature, out _);

            Assert.That(result, Is.True);
            Assert.That(temperature, Is.EqualTo(55f));
            Assert.That(events, Has.Some.Contains("Primary sensor source failed"));
        }

        [Test]
        public void TryRead_FanControlModeWithFailingPrimary_ReturnsFalseWithoutFallback()
        {
            var primary = new FakeSource(42f, 7f, () => false);
            var fallback = new FakeSource(55f, 9f, () => true);
            var manager = CreateManager(SensorSource.FanControl, primary, fallback);

            bool result = manager.TryRead(1000, out _, out _);

            Assert.That(result, Is.False);
            Assert.That(fallback.CallCount, Is.EqualTo(0));
        }

        [Test]
        public void TryRead_LocalMode_UsesFallbackOnly()
        {
            var primary = new FakeSource(42f, 7f, () => true);
            var fallback = new FakeSource(55f, 9f, () => true);
            var manager = CreateManager(SensorSource.Local, primary, fallback);

            bool result = manager.TryRead(1000, out float temperature, out _);

            Assert.That(result, Is.True);
            Assert.That(temperature, Is.EqualTo(55f));
            Assert.That(primary.CallCount, Is.EqualTo(0));
        }

        [Test]
        public void TryRead_AutoWithNullPrimary_UsesFallback()
        {
            var fallback = new FakeSource(55f, 9f, () => true);
            var manager = CreateManager(SensorSource.Auto, null, fallback);

            Assert.That(manager.TryRead(1000, out _, out _), Is.True);
            Assert.That(fallback.CallCount, Is.EqualTo(1));
        }

        [Test]
        public void TryRead_AutoWithNullFallbackAndFailingPrimary_ReturnsFalse()
        {
            var primary = new FakeSource(42f, 7f, () => false);
            var manager = CreateManager(SensorSource.Auto, primary, null);

            Assert.That(manager.TryRead(1000, out _, out _), Is.False);
        }

        [Test]
        public void TryRead_RetiresFallbackOncePerActivation()
        {
            var primary = new FakeSource(42f, 7f, () => true);
            var fallback = new FakeSource(55f, 9f, () => true);
            int retirements = 0;
            var manager = CreateManager(
                SensorSource.Auto,
                primary,
                fallback,
                retireAfterSuccesses: 3,
                retireFallback: () => retirements++);

            for (int cycle = 1; cycle <= 3; cycle++)
            {
                manager.TryRead(cycle * 1000, out _, out _);
            }

            Assert.That(retirements, Is.EqualTo(1));

            for (int cycle = 4; cycle <= 6; cycle++)
            {
                manager.TryRead(cycle * 1000, out _, out _);
            }

            Assert.That(retirements, Is.EqualTo(1));
        }

        [Test]
        public void TryRead_FallbackUse_ReactivatesRetirement()
        {
            bool primaryHealthy = false;
            var primary = new FakeSource(42f, 7f, () => primaryHealthy);
            var fallback = new FakeSource(55f, 9f, () => true);
            int retirements = 0;
            var manager = CreateManager(
                SensorSource.Auto,
                primary,
                fallback,
                retireAfterSuccesses: 3,
                retireFallback: () => retirements++);

            manager.TryRead(1000, out _, out _);
            Assert.That(retirements, Is.EqualTo(0));

            primaryHealthy = true;
            manager.TryRead(32000, out _, out _);
            manager.TryRead(33000, out _, out _);
            manager.TryRead(34000, out _, out _);
            Assert.That(retirements, Is.EqualTo(1));

            primaryHealthy = false;
            manager.TryRead(35000, out _, out _);

            primaryHealthy = true;
            manager.TryRead(66000, out _, out _);
            manager.TryRead(67000, out _, out _);
            manager.TryRead(68000, out _, out _);
            Assert.That(retirements, Is.EqualTo(2));
        }

        [Test]
        public void TryRead_LogsSourceOnlyOnTransitions()
        {
            var primary = new FakeSource(42f, 7f, () => false);
            var fallback = new FakeSource(55f, 9f, () => true);
            var sources = new List<string>();
            var manager = CreateManager(SensorSource.Auto, primary, fallback, sources: sources);

            manager.TryRead(1000, out _, out _);
            manager.TryRead(2000, out _, out _);
            manager.TryRead(2000, out _, out _);

            Assert.That(sources, Has.Count.EqualTo(1));
            Assert.That(sources[0], Does.StartWith("local"));
        }

        [Test]
        public void Constructor_NullBackoff_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() => new SensorSourceManager(
                SensorSource.Auto,
                new FakeSource(1f, 1f, () => true),
                null,
                null,
                retireFallbackAfterSuccesses: 0,
                logEvent: _ => { },
                logSource: _ => { }));
        }

        [Test]
        public void Constructor_NullLogCallbacks_ThrowArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() => new SensorSourceManager(
                SensorSource.Auto,
                null,
                null,
                new RetryBackoff(1000),
                retireFallbackAfterSuccesses: 0,
                logEvent: null,
                logSource: _ => { }));

            Assert.Throws<ArgumentNullException>(() => new SensorSourceManager(
                SensorSource.Auto,
                null,
                null,
                new RetryBackoff(1000),
                retireFallbackAfterSuccesses: 0,
                logEvent: _ => { },
                logSource: null));
        }

        private static SensorSourceManager CreateManager(
            SensorSource mode,
            ISensorSource primary,
            ISensorSource fallback,
            int retireAfterSuccesses = 0,
            Action retireFallback = null,
            List<string> events = null,
            List<string> sources = null)
        {
            return new SensorSourceManager(
                mode,
                primary,
                fallback,
                new RetryBackoff(30000),
                retireAfterSuccesses,
                message => events?.Add(message),
                description => sources?.Add(description),
                retireFallback);
        }

        private sealed class FakeSource : ISensorSource
        {
            private readonly Func<bool> _behavior;

            public FakeSource(float temperature, float usage, Func<bool> behavior)
            {
                Temperature = temperature;
                Usage = usage;
                _behavior = behavior;
            }

            public int CallCount { get; private set; }

            public float Temperature { get; }

            public float Usage { get; }

            public bool TryRead(out float temperatureCelsius, out float usage)
            {
                CallCount++;
                temperatureCelsius = Temperature;
                usage = Usage;
                return _behavior();
            }
        }

        private sealed class ThrowingSource : ISensorSource
        {
            public bool TryRead(out float temperatureCelsius, out float usage)
            {
                throw new InvalidOperationException("boom");
            }
        }
    }
}
