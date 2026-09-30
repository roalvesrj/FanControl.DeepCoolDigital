using System;
using System.Linq;
using HidSharp;

namespace FanControl.DeepCoolDigital
{
    internal sealed class DeepCoolDisplayDevice : IDisposable
    {
        private const byte ReportId = 0x10;
        private const byte StatusCelsius = 19;
        private const byte StatusUsage = 76;
        private const int PacketLength = 64;

        private readonly PluginConfig _config;
        private readonly byte[] _packet = new byte[PacketLength];
        private HidStream _stream;
        private DateTime _nextConnectAttemptUtc = DateTime.MinValue;

        public DeepCoolDisplayDevice(PluginConfig config)
        {
            _config = config;
        }

        public bool IsConnected => _stream != null;

        public void SendTemperature(float celsius)
        {
            Send(StatusCelsius, celsius, IsAlarm(celsius));
        }

        public void SendUsage(float usage, float celsius)
        {
            Send(StatusUsage, usage, IsAlarm(celsius));
        }

        public void Dispose()
        {
            Disconnect();
        }

        private bool IsAlarm(float celsius)
        {
            return celsius > _config.AlarmTemperature;
        }

        private void Send(byte status, float value, bool alarm)
        {
            if (!EnsureConnected()) return;

            int digits = (int)value;
            if (digits < 0) digits = 0;

            _packet[0] = ReportId;
            _packet[1] = status;
            _packet[2] = 0;
            _packet[3] = (byte)(digits < 100 ? digits % 100 / 10 : 9);
            _packet[4] = (byte)(digits < 100 ? digits % 10 : 9);
            _packet[5] = (byte)(alarm ? 1 : 0);

            try
            {
                _stream.Write(_packet);
            }
            catch (Exception ex)
            {
                Log.Write("HID write failed: " + ex.Message);
                Disconnect();
            }
        }

        private bool EnsureConnected()
        {
            if (_stream != null) return true;
            if (DateTime.UtcNow < _nextConnectAttemptUtc) return false;

            _nextConnectAttemptUtc = DateTime.UtcNow.AddSeconds(5);

            try
            {
                HidDevice device = DeviceList.Local
                    .GetHidDevices(_config.VendorId, _config.ProductId)
                    .FirstOrDefault();

                if (device == null) return false;

                if (device.TryOpen(out HidStream stream))
                {
                    _stream = stream;
                    Log.Write($"Connected to {device.GetProductName()} (VID=0x{device.VendorID:X4}, PID=0x{device.ProductID:X4})");
                    return true;
                }

                Log.Write($"Device found but could not be opened (VID=0x{device.VendorID:X4}, PID=0x{device.ProductID:X4}). DeepCool Hub running?");
            }
            catch (Exception ex)
            {
                Log.Write("HID connect failed: " + ex.Message);
            }

            return false;
        }

        private void Disconnect()
        {
            if (_stream == null) return;

            try
            {
                _stream.Dispose();
            }
            catch
            {
            }

            _stream = null;
            _nextConnectAttemptUtc = DateTime.UtcNow.AddSeconds(5);
            Log.Write("Disconnected");
        }
    }
}
