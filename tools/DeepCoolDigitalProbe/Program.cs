using System;
using System.Globalization;
using System.Linq;
using System.Threading;
using FanControl.DeepCoolDigital.Core;
using FanControl.DeepCoolDigital.Core.Protocols;
using FanControl.IPC;
using HidSharp;

namespace DeepCoolDigitalProbe
{
    internal static class Program
    {
        private static int Main(string[] args)
        {
            if (args.Length == 0) return Usage();

            switch (args[0].ToLowerInvariant())
            {
                case "list":
                    return List();
                case "sensors":
                    return ListFanControlSensors(args);
                case "temp":
                    return Send(DisplayField.Temperature, args, "temperature");
                case "usage":
                    return Send(DisplayField.Usage, args, "usage");
                default:
                    return Usage();
            }
        }

        private static int Usage()
        {
            Console.WriteLine("Usage:");
            Console.WriteLine("  DeepCoolDigitalProbe list");
            Console.WriteLine("  DeepCoolDigitalProbe sensors [--filter TEXT] [--timeout MS]");
            Console.WriteLine("  DeepCoolDigitalProbe temp <celsius> [--seconds N] [--interval MS]");
            Console.WriteLine("  DeepCoolDigitalProbe usage <percent> [--seconds N] [--interval MS]");
            return 1;
        }

        private static int ListFanControlSensors(string[] args)
        {
            string filter = null;
            int timeoutMilliseconds = 5000;

            for (int i = 1; i < args.Length; i++)
            {
                if (args[i] == "--filter")
                {
                    if (i + 1 >= args.Length)
                    {
                        return Usage();
                    }

                    filter = args[i + 1];
                    i++;
                }
                else if (args[i] == "--timeout")
                {
                    if (i + 1 >= args.Length || !int.TryParse(args[i + 1], out timeoutMilliseconds))
                    {
                        return Usage();
                    }

                    i++;
                }
            }

            if (timeoutMilliseconds <= 0)
            {
                timeoutMilliseconds = 5000;
            }

            try
            {
                var client = IPCFactory.GetSensorClient();
                GetAllSensorsReply reply = client.GetAllSensors(
                    new GetAllSensorsRequest(),
                    null,
                    DateTime.UtcNow.AddMilliseconds(timeoutMilliseconds),
                    CancellationToken.None);

                int shown = 0;

                foreach (SensorMessage sensor in reply.Sensors)
                {
                    bool matches =
                        filter == null
                        || (sensor.Name != null && sensor.Name.IndexOf(filter, StringComparison.OrdinalIgnoreCase) >= 0)
                        || (sensor.Identifier != null && sensor.Identifier.IndexOf(filter, StringComparison.OrdinalIgnoreCase) >= 0);

                    if (!matches)
                    {
                        continue;
                    }

                    string valueText = sensor.HasValue ? $"value={sensor.Value}" : "no value";
                    Console.WriteLine($"[{sensor.Type}] \"{sensor.Name}\" id=\"{sensor.Identifier}\" origin=\"{sensor.Origin}\" {valueText}");
                    shown++;
                }

                Console.WriteLine($"{shown} sensor(s).");
                return 0;
            }
            catch (UnauthorizedAccessException)
            {
                Console.WriteLine("Access denied to FanControl's IPC pipe. Run this command from an elevated prompt (FanControl runs as administrator).");
                return 1;
            }
            catch (Exception ex)
            {
                Console.WriteLine("FanControl IPC unavailable: " + ex);
                return 1;
            }
        }

        private static int List()
        {
            var devices = DeviceList.Local.GetHidDevices(DeviceRegistry.DeepCoolVendorId).ToList();

            if (devices.Count == 0)
            {
                Console.WriteLine($"No DeepCool HID device found (VID=0x{DeviceRegistry.DeepCoolVendorId:X4}).");
                return 1;
            }

            foreach (HidDevice device in devices)
            {
                DeviceDefinition definition = DeviceRegistry.Find(device.VendorID, device.ProductID);
                string support = definition != null ? definition.Model : "unsupported";
                Console.WriteLine($"VID=0x{device.VendorID:X4} PID=0x{device.ProductID:X4} | {device.GetProductName()} | [{support}] | out={device.GetMaxOutputReportLength()}");
            }

            return 0;
        }

        private static int Send(DisplayField field, string[] args, string label)
        {
            if (args.Length < 2 || !float.TryParse(args[1], NumberStyles.Float, CultureInfo.InvariantCulture, out float value))
            {
                return Usage();
            }

            int seconds = 10;
            int interval = 1000;

            for (int i = 2; i + 1 < args.Length; i++)
            {
                if (args[i] == "--seconds")
                {
                    int.TryParse(args[i + 1], out seconds);
                }
                else if (args[i] == "--interval")
                {
                    int.TryParse(args[i + 1], out interval);
                }
            }

            if (seconds <= 0) seconds = 10;
            if (interval <= 0) interval = 1000;

            DeviceDefinition definition = DeviceRegistry.AgDigital;
            HidDevice device = DeviceList.Local.GetHidDevices(definition.VendorId, definition.ProductId).FirstOrDefault();

            if (device == null)
            {
                Console.WriteLine($"No AG DIGITAL device found (VID=0x{definition.VendorId:X4}, PID=0x{definition.ProductId:X4}).");
                return 1;
            }

            if (!device.TryOpen(out HidStream stream))
            {
                Console.WriteLine("Could not open the device. Is DeepCool Hub running?");
                return 1;
            }

            using (stream)
            {
                string productName = device.GetProductName();
                IDisplayProtocol protocol = definition.CreateProtocol(new DeviceOptions(false));
                var frame = new DisplayFrame(
                    field,
                    value,
                    field == DisplayField.Temperature ? value : 0f,
                    field == DisplayField.Usage ? value : 0f,
                    alarm: false);
                byte[] packet = protocol.ApplyTransportQuirks(protocol.BuildPacket(frame), productName);

                foreach (byte[] initPacket in protocol.CreateInitializationPackets())
                {
                    stream.Write(protocol.ApplyTransportQuirks(initPacket, productName));
                }

                Console.WriteLine($"Sending {label}={Math.Max(0, (int)value)} every {interval}ms for {seconds}s...");

                DateTime end = DateTime.UtcNow.AddSeconds(seconds);

                while (DateTime.UtcNow < end)
                {
                    stream.Write(packet);
                    Thread.Sleep(interval);
                }
            }

            Console.WriteLine("Done.");
            return 0;
        }
    }
}
