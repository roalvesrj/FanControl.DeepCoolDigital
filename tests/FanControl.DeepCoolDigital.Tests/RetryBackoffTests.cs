using System;
using FanControl.DeepCoolDigital.Core;
using NUnit.Framework;

namespace FanControl.DeepCoolDigital.Tests
{
    /// <summary>
    /// Covers the time-based retry gate used when falling back from the IPC sensor source.
    /// </summary>
    [TestFixture]
    [Category("Providers")]
    public class RetryBackoffTests
    {
        [Test]
        public void CanRetry_BeforeAnyFailure_ReturnsTrue()
        {
            var backoff = new RetryBackoff(5000);

            Assert.That(backoff.CanRetry(1000), Is.True);
        }

        [Test]
        public void CanRetry_DuringCooldown_ReturnsFalse()
        {
            var backoff = new RetryBackoff(5000);
            backoff.ReportFailure(1000);

            Assert.That(backoff.CanRetry(5999), Is.False);
        }

        [Test]
        public void CanRetry_WhenCooldownElapsed_ReturnsTrue()
        {
            var backoff = new RetryBackoff(5000);
            backoff.ReportFailure(1000);

            Assert.That(backoff.CanRetry(6000), Is.True);
        }

        [Test]
        public void ReportSuccess_ClearsTheCooldown()
        {
            var backoff = new RetryBackoff(5000);
            backoff.ReportFailure(1000);
            backoff.ReportSuccess();

            Assert.That(backoff.CanRetry(1001), Is.True);
        }

        [Test]
        public void ReportFailure_ExtendsTheCooldownFromTheLatestFailure()
        {
            var backoff = new RetryBackoff(5000);
            backoff.ReportFailure(1000);
            backoff.ReportFailure(2000);

            Assert.That(backoff.CanRetry(6999), Is.False);
            Assert.That(backoff.CanRetry(7000), Is.True);
        }

        [Test]
        public void Constructor_NegativeDelay_ThrowsArgumentOutOfRangeException()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new RetryBackoff(-1));
        }
    }
}
