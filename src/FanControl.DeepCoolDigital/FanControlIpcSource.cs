using System;
using System.Collections.Generic;
using System.Threading;
using FanControl.DeepCoolDigital.Core;
using FanControl.IPC;

namespace FanControl.DeepCoolDigital
{
    /// <summary>
    /// Reads CPU temperature and usage from FanControl's own sensors through its named-pipe IPC channel.
    /// </summary>
    /// <remarks>
    /// This is the preferred sensor source: it reuses the hardware data FanControl already collected,
    /// so the plugin does not need its own LibreHardwareMonitor instance. Sensor identifiers are
    /// discovered once and the values are read from a single <c>GetAllSensors</c> call per update cycle.
    /// The proto also declares <c>ReadSensorValues</c>, but FanControl V281 answers it with
    /// <c>Unimplemented</c>, so it is intentionally not used. Every failure degrades to
    /// <see cref="TryRead"/> returning <see langword="false"/> so the caller can fall back.
    /// </remarks>
    internal sealed class FanControlIpcSource : ISensorSource, IDisposable
    {
        private const int ReadTimeoutMilliseconds = 150;

        private readonly IReadOnlyList<string> _preferredTemperatureNames;
        private readonly string _preferredUsageSensor;
        private SensorsRPC.SensorsRPCClient _client;
        private string _temperatureIdentifier;
        private string _usageIdentifier;

        /// <summary>
        /// Initializes a new instance of the <see cref="FanControlIpcSource"/> class.
        /// </summary>
        /// <param name="preferredTemperatureNames">The preferred temperature sensor names, in priority order.</param>
        /// <param name="preferredUsageSensor">The preferred usage sensor name or identifier.</param>
        public FanControlIpcSource(IReadOnlyList<string> preferredTemperatureNames, string preferredUsageSensor)
        {
            _preferredTemperatureNames = preferredTemperatureNames;
            _preferredUsageSensor = preferredUsageSensor;
        }

        /// <summary>
        /// Reads the CPU temperature and usage from FanControl's sensors.
        /// </summary>
        /// <param name="temperatureCelsius">When this method returns, contains the CPU temperature in degrees Celsius.</param>
        /// <param name="usage">When this method returns, contains the CPU usage percentage.</param>
        /// <returns><see langword="true" /> when both values were read; otherwise, <see langword="false" />.</returns>
        public bool TryRead(out float temperatureCelsius, out float usage)
        {
            temperatureCelsius = 0f;
            usage = 0f;

            try
            {
                SensorsRPC.SensorsRPCClient client = EnsureClient();

                GetAllSensorsReply reply = client.GetAllSensors(
                    new GetAllSensorsRequest(),
                    null,
                    DateTime.UtcNow.AddMilliseconds(ReadTimeoutMilliseconds),
                    CancellationToken.None);

                if (_temperatureIdentifier == null || _usageIdentifier == null)
                {
                    if (!TrySelectIdentifiers(reply))
                    {
                        return false;
                    }
                }

                var samples = new List<SensorSample>(reply.Sensors.Count);

                foreach (SensorMessage sensor in reply.Sensors)
                {
                    if (sensor.HasValue)
                    {
                        samples.Add(new SensorSample(sensor.Name, sensor.Value, sensor.Identifier));
                    }
                }

                if (!SensorLookup.TryGetValue(samples, _temperatureIdentifier, out temperatureCelsius)
                    || !SensorLookup.TryGetValue(samples, _usageIdentifier, out usage))
                {
                    ResetDiscovery();
                    return false;
                }

                return true;
            }
            catch (Exception ex)
            {
                Log.Verbose("FanControl IPC read failed: " + ex.Message);
                ResetClient();
                return false;
            }
        }

        /// <inheritdoc />
        public void Dispose()
        {
            ResetClient();
        }

        private SensorsRPC.SensorsRPCClient EnsureClient()
        {
            if (_client != null)
            {
                return _client;
            }

            _client = IPCFactory.GetSensorClient();
            return _client;
        }

        private bool TrySelectIdentifiers(GetAllSensorsReply reply)
        {
            var temperatures = new List<SensorSample>();
            var usages = new List<SensorSample>();

            foreach (SensorMessage sensor in reply.Sensors)
            {
                if (!sensor.HasValue)
                {
                    continue;
                }

                if (sensor.Type == SensorMessageType.Temperature)
                {
                    temperatures.Add(new SensorSample(sensor.Name, sensor.Value, sensor.Identifier));
                }
                else if (sensor.Type == SensorMessageType.UsagePercent)
                {
                    usages.Add(new SensorSample(sensor.Name, sensor.Value, sensor.Identifier));
                }
            }

            SensorSample? temperature = TemperatureSensorSelector.SelectSample(
                CpuSensorFilter.SelectCpuSensors(temperatures),
                _preferredTemperatureNames);
            SensorSample? usage = UsageSensorSelector.Select(usages, _preferredUsageSensor);

            if (!temperature.HasValue || !usage.HasValue)
            {
                Log.Event("FanControl IPC: no suitable CPU temperature/usage sensor was found.");
                return false;
            }

            _temperatureIdentifier = string.IsNullOrEmpty(temperature.Value.Identifier)
                ? temperature.Value.Name
                : temperature.Value.Identifier;
            _usageIdentifier = string.IsNullOrEmpty(usage.Value.Identifier)
                ? usage.Value.Name
                : usage.Value.Identifier;

            Log.Event($"Connected to FanControl's IPC sensor channel; sensors: temperature=\"{temperature.Value.Name}\", usage=\"{usage.Value.Name}\".");
            return true;
        }

        private void ResetDiscovery()
        {
            _temperatureIdentifier = null;
            _usageIdentifier = null;
        }

        private void ResetClient()
        {
            // Best effort: the generated gRPC client does not implement IDisposable in the current
            // Grpc versions, so dropping the reference is the available release path.
            try
            {
                (_client as IDisposable)?.Dispose();
            }
            catch
            {
            }

            _client = null;
            ResetDiscovery();
        }
    }
}
