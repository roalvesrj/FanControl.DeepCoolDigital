using System;
using System.IO;
using FanControl.DeepCoolDigital.Core;

namespace FanControl.DeepCoolDigital
{
    /// <summary>
    /// Writes diagnostic information to <c>DeepCoolDigital.log</c> next to the plugin dll.
    /// </summary>
    /// <remarks>
    /// Logging is disabled by default and controlled by the <c>logLevel</c> configuration key
    /// (<c>off</c>, <c>events</c> or <c>verbose</c>; the legacy <c>log=true</c> maps to <c>events</c>).
    /// All write failures silently disable logging instead of surfacing an error.
    /// </remarks>
    internal static class Log
    {
        private static LogLevel _level = LogLevel.Off;
        private static string _path;

        /// <summary>
        /// Applies the log settings from the configuration, truncating the log file.
        /// </summary>
        /// <param name="config">The plugin configuration.</param>
        /// <exception cref="ArgumentNullException"><paramref name="config"/> is <see langword="null" />.</exception>
        public static void Init(PluginConfig config)
        {
            if (config == null)
            {
                throw new ArgumentNullException(nameof(config));
            }

            _level = config.LogLevel;

            if (_level == LogLevel.Off)
            {
                return;
            }

            try
            {
                string directory = Path.GetDirectoryName(typeof(Log).Assembly.Location) ?? string.Empty;
                _path = Path.Combine(directory, "DeepCoolDigital.log");
                File.WriteAllText(_path, string.Empty);
            }
            catch
            {
                _level = LogLevel.Off;
            }
        }

        /// <summary>
        /// Writes an event message when logging is enabled at the events level or higher.
        /// </summary>
        /// <param name="message">The message to write.</param>
        public static void Event(string message)
        {
            if (_level < LogLevel.Events)
            {
                return;
            }

            Write(message);
        }

        /// <summary>
        /// Writes a diagnostic message when logging is enabled at the verbose level.
        /// </summary>
        /// <param name="message">The message to write.</param>
        public static void Verbose(string message)
        {
            if (_level < LogLevel.Verbose)
            {
                return;
            }

            Write(message);
        }

        private static void Write(string message)
        {
            try
            {
                File.AppendAllText(_path, DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss ") + message + Environment.NewLine);
            }
            catch
            {
                _level = LogLevel.Off;
            }
        }
    }
}
