using System;
using System.IO;

namespace FanControl.DeepCoolDigital
{
    internal static class Log
    {
        private static bool _enabled;
        private static string _path;

        public static void Init(PluginConfig config)
        {
            _enabled = config.LogEnabled;
            if (!_enabled) return;

            try
            {
                _path = Path.Combine(PluginConfig.BaseDirectory, "DeepCoolDigital.log");
                File.WriteAllText(_path, "");
            }
            catch
            {
                _enabled = false;
            }
        }

        public static void Write(string message)
        {
            if (!_enabled) return;

            try
            {
                File.AppendAllText(_path, DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss ") + message + Environment.NewLine);
            }
            catch
            {
                _enabled = false;
            }
        }
    }
}
