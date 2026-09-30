using System;
using System.Runtime.InteropServices;

namespace FanControl.DeepCoolDigital
{
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

            ulong idleDelta = idleTime - _previousIdle;
            ulong totalDelta = (kernelTime - _previousKernel) + (userTime - _previousUser);

            _previousIdle = idleTime;
            _previousKernel = kernelTime;
            _previousUser = userTime;

            if (totalDelta == 0) return _lastUsage;

            float usage = 100f * (totalDelta - idleDelta) / totalDelta;
            _lastUsage = Math.Max(0f, Math.Min(100f, usage));
            return _lastUsage;
        }
    }
}
