using System.Collections.Generic;
using FanControl.DeepCoolDigital.Core;
using NUnit.Framework;

namespace FanControl.DeepCoolDigital.Tests
{
    /// <summary>
    /// Covers the CPU usage math applied to Windows system time counters.
    /// </summary>
    [TestFixture]
    [Category("Providers")]
    public class CpuUsageCalculatorTests
    {
        private static IEnumerable<TestCaseData> UsageCases()
        {
            yield return new TestCaseData(0UL, 0UL, 0UL, 50UL, 50UL, 50UL, 0f).Returns(50f);
            yield return new TestCaseData(0UL, 0UL, 0UL, 25UL, 50UL, 50UL, 0f).Returns(75f);
            yield return new TestCaseData(0UL, 0UL, 0UL, 100UL, 50UL, 50UL, 0f).Returns(0f);
            yield return new TestCaseData(0UL, 0UL, 0UL, 0UL, 50UL, 50UL, 0f).Returns(100f);
            yield return new TestCaseData(100UL, 100UL, 100UL, 150UL, 150UL, 150UL, 0f).Returns(50f);
        }

        [TestCaseSource(nameof(UsageCases))]
        public float Calculate_GivenDeltas_ReturnsBusyShare(
            ulong previousIdle,
            ulong previousKernel,
            ulong previousUser,
            ulong currentIdle,
            ulong currentKernel,
            ulong currentUser,
            float fallback)
        {
            return CpuUsageCalculator.Calculate(
                previousIdle,
                previousKernel,
                previousUser,
                currentIdle,
                currentKernel,
                currentUser,
                fallback);
        }

        [Test]
        public void Calculate_CounterRegression_ReturnsFallback()
        {
            float result = CpuUsageCalculator.Calculate(100, 100, 100, 50, 50, 50, 42f);

            Assert.That(result, Is.EqualTo(42f));
        }

        [Test]
        public void Calculate_ZeroDelta_ReturnsFallback()
        {
            float result = CpuUsageCalculator.Calculate(10, 20, 30, 10, 20, 30, 33f);

            Assert.That(result, Is.EqualTo(33f));
        }

        [Test]
        public void Calculate_FallbackAboveRange_IsClampedTo100()
        {
            float result = CpuUsageCalculator.Calculate(10, 20, 30, 10, 20, 30, 150f);

            Assert.That(result, Is.EqualTo(100f));
        }

        [Test]
        public void Calculate_FallbackBelowRange_IsClampedToZero()
        {
            float result = CpuUsageCalculator.Calculate(10, 20, 30, 10, 20, 30, -5f);

            Assert.That(result, Is.EqualTo(0f));
        }
    }
}
