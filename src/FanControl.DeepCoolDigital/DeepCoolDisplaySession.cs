using System;
using System.Linq;
using FanControl.DeepCoolDigital.Core;
using FanControl.DeepCoolDigital.Core.Protocols;
using HidSharp;

namespace FanControl.DeepCoolDigital
{
    /// <summary>
    /// Represents one cooler display: its HID connection, protocol, settings and mode state.
    /// </summary>
    /// <remarks>
    /// Every session owns the reconnect logic (retrying every 5 seconds while the device is missing or
    /// busy) and converts the shared system readings into the packets of its protocol. Failures are logged
    /// and degrade only this session.
    /// </remarks>
    internal sealed class DeepCoolDisplaySession : IDisposable
    {
        private readonly int _vendorId;
        private readonly int _productId;
        private readonly DeviceDefinition _definition;
        private readonly bool _isPrimary;
        private DeviceSettings _settings;
        private IDisplayProtocol _protocol;
        private HidStream _stream;
        private string _productName;
        private DateTime _nextConnectAttemptUtc = DateTime.MinValue;
        private bool _unsupportedLogged;
        private bool _fahrenheitWarningLogged;
        private int _lastModeSwitch;
        private bool _showUsage;

        /// <summary>
        /// Initializes a new instance of the <see cref="DeepCoolDisplaySession"/> class.
        /// </summary>
        /// <param name="vendorId">The USB vendor id of the device.</param>
        /// <param name="productId">The USB product id of the device.</param>
        /// <param name="settings">The effective settings of the device.</param>
        /// <param name="isPrimary">Whether this session represents the primary configured target.</param>
        /// <exception cref="ArgumentNullException"><paramref name="settings"/> is <see langword="null" />.</exception>
        public DeepCoolDisplaySession(int vendorId, int productId, DeviceSettings settings, bool isPrimary)
        {
            _vendorId = vendorId;
            _productId = productId;
            _settings = settings ?? throw new ArgumentNullException(nameof(settings));
            _isPrimary = isPrimary;

            _definition = DeviceRegistry.Resolve(vendorId, productId, out bool usedFallback);

            if (usedFallback)
            {
                Log.Event($"Unknown DeepCool device {vendorId:X4}:{productId:X4}; assuming the AG protocol.");
            }

            _protocol = _definition?.CreateProtocol(new DeviceOptions(settings.Fahrenheit));
            _lastModeSwitch = Environment.TickCount;
            _showUsage = settings.Mode == DisplayMode.Usage;
        }

        /// <summary>
        /// Gets the USB vendor id of the device.
        /// </summary>
        /// <value>The vendor id.</value>
        public int VendorId => _vendorId;

        /// <summary>
        /// Gets the USB product id of the device.
        /// </summary>
        /// <value>The product id.</value>
        public int ProductId => _productId;

        /// <summary>
        /// Gets the unique id of the FanControl sensor exposed for this device.
        /// </summary>
        /// <value>The stable sensor identifier.</value>
        public string SensorId => _isPrimary
            ? "DeepCoolDigital/CpuTemperature"
            : $"DeepCoolDigital/{_vendorId:X4}{_productId:X4}/CpuTemperature";

        /// <summary>
        /// Gets the FanControl sensor name of this device.
        /// </summary>
        /// <value>The human-readable sensor name.</value>
        public string SensorName => _isPrimary
            ? "DeepCool Display CPU Temp"
            : $"{_definition?.Model ?? "DeepCool Display"} CPU Temp";

        /// <summary>
        /// Gets or sets the sensor registered for this device.
        /// </summary>
        /// <value>The sensor whose value mirrors the temperature sent to the display.</value>
        public DeepCoolDisplaySensor Sensor { get; set; }

        /// <summary>
        /// Applies updated settings, recreating the protocol when the temperature unit changed.
        /// </summary>
        /// <param name="settings">The new effective settings.</param>
        public void ApplySettings(DeviceSettings settings)
        {
            if (settings == null)
            {
                return;
            }

            bool fahrenheitChanged = settings.Fahrenheit != _settings.Fahrenheit;
            _settings = settings;

            if (_definition == null || !fahrenheitChanged)
            {
                return;
            }

            _protocol = _definition.CreateProtocol(new DeviceOptions(settings.Fahrenheit));
            Log.Event($"{Describe()} settings updated (Fahrenheit={settings.Fahrenheit}).");
        }

        /// <summary>
        /// Renders the current system state on the display.
        /// </summary>
        /// <param name="temperatureCelsius">The CPU temperature, in degrees Celsius.</param>
        /// <param name="usage">The CPU usage percentage.</param>
        public void Update(float temperatureCelsius, float usage)
        {
            if (Sensor != null)
            {
                Sensor.Value = temperatureCelsius;
            }

            if (_protocol == null)
            {
                if (!_unsupportedLogged)
                {
                    _unsupportedLogged = true;
                    Log.Event($"Device {_vendorId:X4}:{_productId:X4} is not supported; no data will be sent.");
                }

                return;
            }

            if (!EnsureConnected())
            {
                return;
            }

            if (_settings.Mode == DisplayMode.Auto)
            {
                if (unchecked(Environment.TickCount - _lastModeSwitch) >= _settings.AutoSwitchSeconds * 1000)
                {
                    _showUsage = !_showUsage;
                    _lastModeSwitch = Environment.TickCount;
                }
            }
            else
            {
                _showUsage = _settings.Mode == DisplayMode.Usage;
            }

            bool alarm = _settings.AlarmEnabled
                && _definition.Capabilities.SupportsAlarm
                && temperatureCelsius > _settings.AlarmTemperature;

            DisplayField field;
            float displayValue;

            if (_showUsage)
            {
                field = DisplayField.Usage;
                displayValue = usage;
            }
            else
            {
                field = DisplayField.Temperature;
                displayValue = ConvertTemperature(temperatureCelsius);
            }

            Send(new DisplayFrame(field, displayValue, temperatureCelsius, usage, alarm));
        }

        /// <inheritdoc />
        public void Dispose()
        {
            Disconnect();
        }

        private string Describe()
        {
            return $"{_definition?.Model ?? "DeepCool device"} {_vendorId:X4}:{_productId:X4}";
        }

        private float ConvertTemperature(float celsius)
        {
            if (!_settings.Fahrenheit)
            {
                return celsius;
            }

            if (!_definition.Capabilities.SupportsFahrenheit)
            {
                if (!_fahrenheitWarningLogged)
                {
                    _fahrenheitWarningLogged = true;
                    Log.Event($"{Describe()} does not support Fahrenheit; displaying Celsius.");
                }

                return celsius;
            }

            return celsius * 9f / 5f + 32f;
        }

        private void Send(DisplayFrame frame)
        {
            try
            {
                byte[] packet = _protocol.ApplyTransportQuirks(_protocol.BuildPacket(frame), _productName);
                _stream.Write(packet);
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
                    .GetHidDevices(_vendorId, _productId)
                    .FirstOrDefault();

                if (device == null)
                {
                    return false;
                }

                if (device.TryOpen(out HidStream stream))
                {
                    string productName = device.GetProductName();

                    try
                    {
                        foreach (byte[] initPacket in _protocol.CreateInitializationPackets())
                        {
                            stream.Write(_protocol.ApplyTransportQuirks(initPacket, productName));
                        }
                    }
                    catch (Exception ex)
                    {
                        Log.Event("HID initialization failed: " + ex.Message);

                        try
                        {
                            stream.Dispose();
                        }
                        catch
                        {
                        }

                        return false;
                    }

                    _productName = productName;
                    _stream = stream;
                    Log.Event($"Connected to {productName} [{_definition.Model}, {_protocol.GetType().Name}] (VID=0x{_vendorId:X4}, PID=0x{_productId:X4})");
                    return true;
                }

                Log.Event($"Device found but could not be opened (VID=0x{_vendorId:X4}, PID=0x{_productId:X4}). DeepCool Hub running?");
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
