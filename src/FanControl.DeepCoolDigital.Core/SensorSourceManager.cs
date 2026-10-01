using System;

namespace FanControl.DeepCoolDigital.Core
{
    /// <summary>
    /// Implements the sensor source policy: prefer the primary source, fall back with a cooldown,
    /// and log source transitions.
    /// </summary>
    /// <remarks>
    /// The manager is deliberately free of hardware and FanControl dependencies so the fallback state
    /// machine is fully unit-testable. It also swallows exceptions thrown by the sources themselves,
    /// which covers the case where the primary source cannot even load its underlying types.
    /// </remarks>
    public sealed class SensorSourceManager
    {
        private readonly SensorSource _mode;
        private readonly ISensorSource _primary;
        private readonly ISensorSource _fallback;
        private readonly RetryBackoff _backoff;
        private readonly int _retireFallbackAfterSuccesses;
        private readonly Action<string> _logEvent;
        private readonly Action<string> _logSource;
        private readonly Action _retireFallback;
        private string _lastDescription;
        private int _primarySuccessStreak;

        /// <summary>
        /// Initializes a new instance of the <see cref="SensorSourceManager"/> class.
        /// </summary>
        /// <param name="mode">One of the enumeration values that specifies which sources may be used.</param>
        /// <param name="primary">The preferred source, typically FanControl's IPC channel; may be <see langword="null" />.</param>
        /// <param name="fallback">The fallback source, typically the local LibreHardwareMonitor/kernel source; may be <see langword="null" />.</param>
        /// <param name="backoff">The retry gate applied after primary failures.</param>
        /// <param name="retireFallbackAfterSuccesses">The number of consecutive primary successes after which <paramref name="retireFallback"/> is invoked; zero disables retirement.</param>
        /// <param name="logEvent">A callback that receives diagnostic event messages.</param>
        /// <param name="logSource">A callback that receives source transition descriptions.</param>
        /// <param name="retireFallback">An optional callback invoked when the fallback has been idle long enough to be released.</param>
        /// <exception cref="ArgumentNullException"><paramref name="backoff"/>, <paramref name="logEvent"/> or <paramref name="logSource"/> is <see langword="null" />.</exception>
        public SensorSourceManager(
            SensorSource mode,
            ISensorSource primary,
            ISensorSource fallback,
            RetryBackoff backoff,
            int retireFallbackAfterSuccesses,
            Action<string> logEvent,
            Action<string> logSource,
            Action retireFallback = null)
        {
            _mode = mode;
            _primary = primary;
            _fallback = fallback;
            _backoff = backoff ?? throw new ArgumentNullException(nameof(backoff));
            _retireFallbackAfterSuccesses = Math.Max(0, retireFallbackAfterSuccesses);
            _logEvent = logEvent ?? throw new ArgumentNullException(nameof(logEvent));
            _logSource = logSource ?? throw new ArgumentNullException(nameof(logSource));
            _retireFallback = retireFallback;
        }

        /// <summary>
        /// Reads the current CPU temperature and usage applying the configured source policy.
        /// </summary>
        /// <param name="nowMilliseconds">The current time, in milliseconds, used by the retry gate.</param>
        /// <param name="temperatureCelsius">When this method returns, contains the CPU temperature in degrees Celsius.</param>
        /// <param name="usage">When this method returns, contains the CPU usage percentage.</param>
        /// <returns><see langword="true" /> when a source produced both values; otherwise, <see langword="false" />.</returns>
        public bool TryRead(int nowMilliseconds, out float temperatureCelsius, out float usage)
        {
            temperatureCelsius = 0f;
            usage = 0f;

            if (_mode == SensorSource.Local)
            {
                return TryReadFallback(out temperatureCelsius, out usage);
            }

            if (_primary != null && _backoff.CanRetry(nowMilliseconds))
            {
                bool success = false;

                try
                {
                    success = _primary.TryRead(out temperatureCelsius, out usage);
                }
                catch (Exception ex)
                {
                    _logEvent("Primary sensor source failed: " + ex.Message);
                }

                if (success)
                {
                    _backoff.ReportSuccess();
                    _primarySuccessStreak++;
                    LogSource("FanControl IPC");
                    RetireFallbackWhenIdle();
                    return true;
                }

                _primarySuccessStreak = 0;
                _backoff.ReportFailure(nowMilliseconds);
            }

            if (_mode == SensorSource.FanControl)
            {
                LogSource(_backoff.CanRetry(nowMilliseconds)
                    ? "FanControl IPC (unavailable)"
                    : "FanControl IPC (waiting to retry)");
                return false;
            }

            return TryReadFallback(out temperatureCelsius, out usage);
        }

        private bool TryReadFallback(out float temperatureCelsius, out float usage)
        {
            temperatureCelsius = 0f;
            usage = 0f;

            if (_fallback == null)
            {
                return false;
            }

            try
            {
                if (_fallback.TryRead(out temperatureCelsius, out usage))
                {
                    LogSource("local (LibreHardwareMonitor + kernel)");
                    return true;
                }
            }
            catch (Exception ex)
            {
                _logEvent("Local sensor source failed: " + ex.Message);
            }

            return false;
        }

        private void RetireFallbackWhenIdle()
        {
            if (_retireFallback == null || _retireFallbackAfterSuccesses <= 0)
            {
                return;
            }

            if (_primarySuccessStreak >= _retireFallbackAfterSuccesses)
            {
                _primarySuccessStreak = 0;
                _retireFallback();
            }
        }

        private void LogSource(string description)
        {
            if (string.Equals(_lastDescription, description, StringComparison.Ordinal))
            {
                return;
            }

            _lastDescription = description;
            _logSource(description);
        }
    }
}
