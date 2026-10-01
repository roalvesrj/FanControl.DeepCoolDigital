using System;
using FanControl.DeepCoolDigital.Core;
using NUnit.Framework;

namespace FanControl.DeepCoolDigital.Tests
{
    /// <summary>
    /// Covers the narrowing of mixed sensor lists to CPU temperature candidates.
    /// </summary>
    [TestFixture]
    [Category("Providers")]
    public class CpuSensorFilterTests
    {
        [Test]
        public void SelectCpuSensors_MixedIdentifiers_KeepsOnlyCpuSensors()
        {
            var samples = new[]
            {
                new SensorSample("Core (Tctl/Tdie)", 55f, "/amdcpu/0/temperature/2"),
                new SensorSample("GPU Hot Spot", 78f, "/gpu-nvidia/0/temperature/2"),
                new SensorSample("CPU Package", 60f, "/intelcpu/0/temperature/0")
            };

            var result = CpuSensorFilter.SelectCpuSensors(samples);

            Assert.That(result, Has.Count.EqualTo(2));
            Assert.That(result[0].Name, Is.EqualTo("Core (Tctl/Tdie)"));
            Assert.That(result[1].Name, Is.EqualTo("CPU Package"));
        }

        [Test]
        public void SelectCpuSensors_NoCpuIdentifiers_ReturnsOriginalList()
        {
            var samples = new[]
            {
                new SensorSample("GPU Core", 70f, "/gpu-amd/0/temperature/0"),
                new SensorSample("System", 40f, "/lpc/nct6798d/temperature/0")
            };

            var result = CpuSensorFilter.SelectCpuSensors(samples);

            Assert.That(result, Is.SameAs(samples));
        }

        [Test]
        public void SelectCpuSensors_SamplesWithoutIdentifiers_ReturnOriginalList()
        {
            var samples = new[]
            {
                new SensorSample("CPU Package", 60f),
                new SensorSample("Core (Tctl/Tdie)", 55f)
            };

            var result = CpuSensorFilter.SelectCpuSensors(samples);

            Assert.That(result, Is.SameAs(samples));
        }

        [Test]
        public void SelectCpuSensors_NullList_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() => CpuSensorFilter.SelectCpuSensors(null));
        }
    }
}
