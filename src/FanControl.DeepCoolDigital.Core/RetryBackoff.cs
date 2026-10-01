using System;

namespace FanControl.DeepCoolDigital.Core
{
    /// <summary>
    /// Implements a simple time-based retry gate: after a failure, retries are blocked for a cooldown.
    /// </summary>
    /// <remarks>
    /// Used to fall back from the FanControl IPC source to the local sensor source without hammering a
    /// failing endpoint; the clock is passed in by the caller so the behavior is fully unit-testable.
    /// </remarks>
    public sealed class RetryBackoff
    {
        private readonly int _retryDelayMilliseconds;
        private long _blockedUntilMilliseconds = long.MinValue;

        /// <summary>
        /// Initializes a new instance of the <see cref="RetryBackoff"/> class.
        /// </summary>
        /// <param name="retryDelayMilliseconds">The cooldown applied after a failure, in milliseconds.</param>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="retryDelayMilliseconds"/> is negative.</exception>
        public RetryBackoff(int retryDelayMilliseconds)
        {
            if (retryDelayMilliseconds < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(retryDelayMilliseconds), "The retry delay cannot be negative.");
            }

            _retryDelayMilliseconds = retryDelayMilliseconds;
        }

        /// <summary>
        /// Determines whether a new attempt is allowed at the given time.
        /// </summary>
        /// <param name="nowMilliseconds">The current time, in milliseconds.</param>
        /// <returns><see langword="true" /> when the cooldown elapsed or no failure is recorded; otherwise, <see langword="false" />.</returns>
        public bool CanRetry(int nowMilliseconds)
        {
            return nowMilliseconds >= _blockedUntilMilliseconds;
        }

        /// <summary>
        /// Records a failure and starts the cooldown.
        /// </summary>
        /// <param name="nowMilliseconds">The current time, in milliseconds.</param>
        public void ReportFailure(int nowMilliseconds)
        {
            _blockedUntilMilliseconds = (long)nowMilliseconds + _retryDelayMilliseconds;
        }

        /// <summary>
        /// Records a success and clears any pending cooldown.
        /// </summary>
        public void ReportSuccess()
        {
            _blockedUntilMilliseconds = long.MinValue;
        }
    }
}
