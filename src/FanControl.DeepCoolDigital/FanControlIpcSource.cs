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
    /// so the plugin does not need its own LibreHardwareMonitor instance. The sensor identifiers are
    /// discovered once through <c>GetAllSensors</c> and then read in a single batched
    /// <c>ReadSensorValues</c> call per update cycle. Every failure degrades to <see cref="TryRead"/>
    /// returning <see langword="false"/> so the caller can fall back to the local sources.
    /// </remarks>
    internal sealed class FanControlIpcSource : IDisposable
    {
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

                if (_temperatureIdentifier == null || _usageIdentifier == null)
                {
                    if (!TryDiscover(client))
                    {
                        return false;
                    }
                }

                var request = new ReadSensorValuesRequest();
                request.Ids.Add(_temperatureIdentifier);
                request.Ids.Add(_usageIdentifier);

                ReadSensorValuesReply reply = client.ReadSensorValues(
                    request,
                    null,
                    DateTime.UtcNow.AddMilliseconds(500),
                    CancellationToken.None);

                if (!reply.Values.TryGetValue(_temperatureIdentifier, out temperatureCelsius)
                    || !reply.Values.TryGetValue(_usageIdentifier, out usage))
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

        /// <summary>
        /// Drops the client and the discovered sensor identifiers, forcing a reconnect on the next read.
        /// </summary>
        public void Reset()
        {
            ResetClient();
        }

        /// <inheritdoc />
        public void Dispose()
        {
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

        private SensorsRPC.SensorsRPCClient EnsureClient()
        {
            if (_client != null)
            {
                return _client;
            }

            _client = IPCFactory.GetSensorClient();
            Log.Event("Connected to FanControl's IPC sensor channel.");
            return _client;
        }

        private bool TryDiscover(SensorsRPC.SensorsRPCClient client)
        {
            GetAllSensorsReply reply = client.GetAllSensors(
                new GetAllSensorsRequest(),
                null,
                DateTime.UtcNow.AddSeconds(1),
                CancellationToken.None);

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

            SensorSample? temperature = TemperatureSensorSelector.SelectSample(temperatures, _preferredTemperatureNames);
            SensorSample? usage = UsageSensorSelector.Select(usages, _preferredUsageSensor);

            if (!temperature.HasValue || !usage.HasValue)
            {
                Log.Event("FanControl IPC: no suitable CPU temperature/usage sensor was found.");
                return false;
            }

            _temperatureIdentifier = temperature.Value.Identifier ?? temperature.Value.Name;
            _usageIdentifier = usage.Value.Identifier ?? usage.Value.Name;

            Log.Event($"FanControl IPC sensors: temperature=\"{temperature.Value.Name}\", usage=\"{usage.Value.Name}\".");
            return true;
        }

        private void ResetDiscovery()
        {
            _temperatureIdentifier = null;
            _usageIdentifier = null;
        }

        private void ResetClient()
        {
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
