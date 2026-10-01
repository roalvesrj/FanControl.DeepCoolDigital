using System;

namespace FanControl.DeepCoolDigital.Core
{
    /// <summary>
    /// Implements a simple time-based retry gate: after a failure, retries are blocked for a cooldown.
    /// </summary>
    /// <remarks>
    /// The arithmetic is intentionally done in 32-bit, wrap-safe form so that the ~49.7-day
    /// <c>Environment.TickCount</c> wraparound cannot leave the gate permanently blocked.
    /// </remarks>
    public sealed class RetryBackoff
    {
        private readonly int _retryDelayMilliseconds;
        private int _blockedUntilMilliseconds;
        private bool _hasFailure;

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
            return !_hasFailure || unchecked(nowMilliseconds - _blockedUntilMilliseconds) >= 0;
        }

        /// <summary>
        /// Records a failure and starts the cooldown.
        /// </summary>
        /// <param name="nowMilliseconds">The current time, in milliseconds.</param>
        public void ReportFailure(int nowMilliseconds)
        {
            _blockedUntilMilliseconds = unchecked(nowMilliseconds + _retryDelayMilliseconds);
            _hasFailure = true;
        }

        /// <summary>
        /// Records a success and clears any pending cooldown.
        /// </summary>
        public void ReportSuccess()
        {
            _hasFailure = false;
        }
    }
}
