using System;

namespace FanControl.DeepCoolDigital.Core.Protocols
{
    /// <summary>
    /// Shared helpers for display families that render values as digit fields (AG, AK, LS, CH).
    /// </summary>
    internal static class DigitPacketBuilder
    {
        /// <summary>
        /// Truncates a value into the range that fits a digit field.
        /// </summary>
        /// <param name="value">The raw value.</param>
        /// <param name="digitCount">The number of decimal digits the field can render.</param>
        /// <returns>An integer between <c>0</c> and the largest value that fits the field.</returns>
        /// <remarks>
        /// Not-a-number and non-positive values become <c>0</c>; values too large for the field are
        /// clamped to all-nines, matching the official DeepCool Hub behavior.
        /// </remarks>
        internal static int ClampDigits(float value, int digitCount)
        {
            if (float.IsNaN(value) || value <= 0f)
            {
                return 0;
            }

            int limit = digitCount >= 3 ? 1000 : 100;

            if (value >= limit)
            {
                return limit - 1;
            }

            return (int)value;
        }

        /// <summary>
        /// Writes the decimal digits of a value into consecutive packet bytes, most significant first.
        /// </summary>
        /// <param name="packet">The packet to write into.</param>
        /// <param name="firstDigitIndex">The index of the most significant digit.</param>
        /// <param name="digitCount">The number of digits to write.</param>
        /// <param name="digits">The value to split into digits.</param>
        internal static void WriteDigits(byte[] packet, int firstDigitIndex, int digitCount, int digits)
        {
            for (int position = digitCount - 1; position >= 0; position--)
            {
                packet[firstDigitIndex + position] = (byte)(digits % 10);
                digits /= 10;
            }
        }

        /// <summary>
        /// Computes the status bar level of a display family from the usage percentage.
        /// </summary>
        /// <param name="usage">The CPU usage percentage.</param>
        /// <returns>A bar level between <c>1</c> and <c>10</c>.</returns>
        /// <remarks>
        /// The mapping matches the reference implementation: below 15% the bar shows the minimum
        /// level, otherwise it tracks usage in steps of 10%.
        /// </remarks>
        internal static byte UsageBar(float usage)
        {
            if (float.IsNaN(usage) || usage < 15f)
            {
                return 1;
            }

            float level = (float)Math.Round(usage / 10f, MidpointRounding.AwayFromZero);
            return (byte)Math.Max(1, Math.Min(10, level));
        }
    }
}
