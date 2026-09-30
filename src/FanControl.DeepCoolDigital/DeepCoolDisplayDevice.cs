using System;
using System.Linq;
using FanControl.DeepCoolDigital.Core;
using FanControl.DeepCoolDigital.Core.Protocols;
using HidSharp;

namespace FanControl.DeepCoolDigital
{
    /// <summary>
    /// Owns the HID connection to the cooler display and streams status packets to it.
    /// </summary>
    /// <remarks>
    /// Wiring a device identity to its protocol is delegated to <see cref="DeviceRegistry"/>. The device
    /// reconnects automatically every 5 seconds while it is missing or busy, and every failure is logged
    /// without propagating an exception to the caller.
    /// </remarks>
    internal sealed class DeepCoolDisplayDevice : IDisposable
    {
        private readonly PluginConfig _config;
        private readonly DeviceDefinition _definition;
        private readonly IDisplayProtocol _protocol;
        private HidStream _stream;
        private DateTime _nextConnectAttemptUtc = DateTime.MinValue;
        private bool _unsupportedLogged;

        /// <summary>
        /// Initializes a new instance of the <see cref="DeepCoolDisplayDevice"/> class.
        /// </summary>
        /// <param name="config">The plugin configuration that identifies the device.</param>
        /// <exception cref="ArgumentNullException"><paramref name="config"/> is <see langword="null" />.</exception>
        public DeepCoolDisplayDevice(PluginConfig config)
        {
            _config = config ?? throw new ArgumentNullException(nameof(config));

            _definition = DeviceRegistry.Find(config.VendorId, config.ProductId);

            if (_definition == null && config.VendorId == DeviceRegistry.DeepCoolVendorId)
            {
                _definition = DeviceRegistry.AgDigital;
                Log.Event($"Unknown DeepCool device {config.VendorId:X4}:{config.ProductId:X4}; assuming the AG protocol.");
            }

            _protocol = _definition?.CreateProtocol();
        }

        /// <summary>
        /// Gets a value indicating whether the HID stream is currently open.
        /// </summary>
        /// <value><see langword="true" /> while the display is connected; otherwise, <see langword="false" />.</value>
        public bool IsConnected => _stream != null;

        /// <summary>
        /// Sends the CPU temperature to the display.
        /// </summary>
        /// <param name="celsius">The temperature, in degrees Celsius.</param>
        public void SendTemperature(float celsius)
        {
            Send(DisplayField.Temperature, celsius, IsAlarm(celsius));
        }

        /// <summary>
        /// Sends the CPU usage to the display, carrying the current alert state.
        /// </summary>
        /// <param name="usage">The usage percentage, between <c>0</c> and <c>100</c>.</param>
        /// <param name="celsius">The current CPU temperature, used for the alert state.</param>
        public void SendUsage(float usage, float celsius)
        {
            Send(DisplayField.Usage, usage, IsAlarm(celsius));
        }

        /// <inheritdoc />
        public void Dispose()
        {
            Disconnect();
        }

        private bool IsAlarm(float celsius)
        {
            return _definition != null
                && _definition.Capabilities.SupportsAlarm
                && celsius > _config.AlarmTemperature;
        }

        private void Send(DisplayField field, float value, bool alarm)
        {
            if (_protocol == null)
            {
                if (!_unsupportedLogged)
                {
                    _unsupportedLogged = true;
                    Log.Event($"Device {_config.VendorId:X4}:{_config.ProductId:X4} is not supported; no data will be sent.");
                }

                return;
            }

            if (!EnsureConnected())
            {
                return;
            }

            try
            {
                _stream.Write(_protocol.BuildPacket(field, value, alarm));
            }
            catch (Exception ex)
            {
                Log.Event("HID write failed: " + ex.Message);
                Disconnect();
            }
        }

        private bool EnsureConnected()
        {
            if (_stream != null)
            {
                return true;
            }

            if (DateTime.UtcNow < _nextConnectAttemptUtc)
            {
                return false;
            }

            _nextConnectAttemptUtc = DateTime.UtcNow.AddSeconds(5);

            try
            {
                HidDevice device = DeviceList.Local
                    .GetHidDevices(_config.VendorId, _config.ProductId)
                    .FirstOrDefault();

                if (device == null)
                {
                    return false;
                }

                if (device.TryOpen(out HidStream stream))
                {
                    _stream = stream;

                    foreach (byte[] initPacket in _protocol.CreateInitializationPackets())
                    {
                        stream.Write(initPacket);
                    }

                    Log.Event($"Connected to {device.GetProductName()} [{_definition.Model}] (VID=0x{device.VendorID:X4}, PID=0x{device.ProductID:X4})");
                    return true;
                }

                Log.Event($"Device found but could not be opened (VID=0x{device.VendorID:X4}, PID=0x{device.ProductID:X4}). DeepCool Hub running?");
            }
            catch (Exception ex)
            {
                Log.Event("HID connect failed: " + ex.Message);
            }

            return false;
        }

        private void Disconnect()
        {
            if (_stream == null)
            {
                return;
            }

            try
            {
                _stream.Dispose();
            }
            catch
            {
            }

            _stream = null;
            _nextConnectAttemptUtc = DateTime.UtcNow.AddSeconds(5);
            Log.Event("Disconnected");
        }
    }
}
