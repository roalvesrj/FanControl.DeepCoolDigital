using System;
using System.Globalization;
using System.Linq;
using System.Threading;
using HidSharp;

namespace DeepCoolDigitalProbe
{
    internal static class Program
    {
        private const int DefaultVendorId = 0x3633;
        private const int AgDigitalProductId = 0x0008;
        private const byte ReportId = 0x10;
        private const byte StatusCelsius = 19;
        private const byte StatusUsage = 76;

        private static int Main(string[] args)
        {
            if (args.Length == 0) return Usage();

            switch (args[0].ToLowerInvariant())
            {
                case "list":
                    return List();
                case "temp":
                    return Send(StatusCelsius, args, "temperature");
                case "usage":
                    return Send(StatusUsage, args, "usage");
                default:
                    return Usage();
            }
        }

        private static int Usage()
        {
            Console.WriteLine("Usage:");
            Console.WriteLine("  DeepCoolDigitalProbe list");
            Console.WriteLine("  DeepCoolDigitalProbe temp <celsius> [--seconds N] [--interval MS]");
            Console.WriteLine("  DeepCoolDigitalProbe usage <percent> [--seconds N] [--interval MS]");
            return 1;
        }

        private static int List()
        {
            var devices = DeviceList.Local.GetHidDevices(DefaultVendorId).ToList();

            if (devices.Count == 0)
            {
                Console.WriteLine($"No DeepCool HID device found (VID=0x{DefaultVendorId:X4}).");
                return 1;
            }

            foreach (HidDevice device in devices)
            {
                Console.WriteLine($"VID=0x{device.VendorID:X4} PID=0x{device.ProductID:X4} | {device.GetProductName()} | out={device.GetMaxOutputReportLength()}");
            }

            return 0;
        }

        private static int Send(byte status, string[] args, string label)
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

            HidDevice device = DeviceList.Local.GetHidDevices(DefaultVendorId, AgDigitalProductId).FirstOrDefault();

            if (device == null)
            {
                Console.WriteLine($"No AG-DIGITAL device found (VID=0x{DefaultVendorId:X4}, PID=0x{AgDigitalProductId:X4}).");
                return 1;
            }

            if (!device.TryOpen(out HidStream stream))
            {
                Console.WriteLine("Could not open the device. Is DeepCool Hub running?");
                return 1;
            }

            using (stream)
            {
                var packet = new byte[64];
                packet[0] = ReportId;
                packet[1] = status;

                int digits = Math.Max(0, (int)value);
                packet[3] = (byte)(digits < 100 ? digits % 100 / 10 : 9);
                packet[4] = (byte)(digits < 100 ? digits % 10 : 9);

                Console.WriteLine($"Sending {label}={digits} every {interval}ms for {seconds}s...");

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
