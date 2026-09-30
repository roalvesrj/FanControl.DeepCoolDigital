using System;
using System.Runtime.InteropServices;
using FanControl.DeepCoolDigital.Core;

namespace FanControl.DeepCoolDigital
{
    /// <summary>
    /// Samples CPU usage from the Windows kernel system time counters.
    /// </summary>
    /// <remarks>
    /// The readings are converted to percentages by <see cref="CpuUsageCalculator"/>; this class only
    /// owns the native interop and the previous sample state.
    /// </remarks>
    internal sealed class CpuUsage
    {
        [StructLayout(LayoutKind.Sequential)]
        private struct NativeFileTime
        {
            public uint LowDateTime;
            public uint HighDateTime;

            public ulong ToUInt64()
            {
                return ((ulong)HighDateTime << 32) | LowDateTime;
            }
        }

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool GetSystemTimes(out NativeFileTime idleTime, out NativeFileTime kernelTime, out NativeFileTime userTime);

        private ulong _previousIdle;
        private ulong _previousKernel;
        private ulong _previousUser;
        private bool _hasPrevious;
        private float _lastUsage;

        /// <summary>
        /// Reads the CPU usage accumulated since the previous call.
        /// </summary>
        /// <returns>The CPU usage percentage, between <c>0</c> and <c>100</c>.</returns>
        public float Read()
        {
            if (!GetSystemTimes(out NativeFileTime idle, out NativeFileTime kernel, out NativeFileTime user))
            {
                return _lastUsage;
            }

            ulong idleTime = idle.ToUInt64();
            ulong kernelTime = kernel.ToUInt64();
            ulong userTime = user.ToUInt64();

            if (!_hasPrevious)
            {
                _previousIdle = idleTime;
                _previousKernel = kernelTime;
                _previousUser = userTime;
                _hasPrevious = true;
                return 0f;
            }

            float usage = CpuUsageCalculator.Calculate(
                _previousIdle,
                _previousKernel,
                _previousUser,
                idleTime,
                kernelTime,
                userTime,
                _lastUsage);

            _previousIdle = idleTime;
            _previousKernel = kernelTime;
            _previousUser = userTime;
            _lastUsage = usage;

            return usage;
        }
    }
}
