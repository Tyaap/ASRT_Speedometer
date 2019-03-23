using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace Speedo.Hook
{
    public class Speed : IDisposable
    {
        private float _plane = 0.0f;
        public int[] addresses = new int[6];
        private Process process = new Process();
        public List<uint> values = new List<uint>();
        private const int PROCESS_WM_READ = 16;
        private float _car1;
        private float _boat;
        public CurrentModeEnum CurrentMode;
        private static IntPtr processHandle;
        public bool isInitialised;
        public bool Display;

        public enum CurrentModeEnum
        {
            Car,
            Boat,
            Plane,
        }

        [DllImport("kernel32.dll")]
        public static extern IntPtr OpenProcess(
          int dwDesiredAccess,
          bool bInheritHandle,
          int dwProcessId);

        [DllImport("kernel32.dll")]
        public static extern bool ReadProcessMemory(
          int hProcess,
          uint lpBaseAddress,
          byte[] lpBuffer,
          int dwSize,
          int lpNumberOfBytesRead);

        public Speed(int processId)
        {
            if (!isInitialised)
            {
                processHandle = OpenProcess(16, false, processId);
                process = Process.GetProcessById(processId);
            }

            CurrentMode = CurrentModeEnum.Car;
            isInitialised = true;

            values.Clear();
            values.Add(GetCarPointer());
            values.Add(GetBoatPointer());
            values.Add(GetPlanePointer());
        }

        public uint GetBoatPointer()
        {
            byte[] lpBuffer = new byte[4];
            uint tmp;
            ReadProcessMemory((int)processHandle, (uint)(11280076 + (int)process.MainModule.BaseAddress), lpBuffer, lpBuffer.Length, 0);
            tmp = BitConverter.ToUInt32(lpBuffer, 0);
            ReadProcessMemory((int)processHandle, tmp + 184U, lpBuffer, lpBuffer.Length, 0);
            tmp = BitConverter.ToUInt32(lpBuffer, 0);
            ReadProcessMemory((int)processHandle, tmp + 304U, lpBuffer, lpBuffer.Length, 0);
            tmp = BitConverter.ToUInt32(lpBuffer, 0);
            ReadProcessMemory((int)processHandle, tmp + 1252U, lpBuffer, lpBuffer.Length, 0);
            return BitConverter.ToUInt32(lpBuffer, 0) + 464U;
        }

        public uint GetCarPointer()
        {
            byte[] lpBuffer = new byte[4];
            uint tmp;
            ReadProcessMemory((int)processHandle, (uint)(11280076 + (int)process.MainModule.BaseAddress), lpBuffer, lpBuffer.Length, 0);
            tmp = BitConverter.ToUInt32(lpBuffer, 0);
            ReadProcessMemory((int)processHandle, tmp + 176U, lpBuffer, lpBuffer.Length, 0);
            tmp = BitConverter.ToUInt32(lpBuffer, 0);
            ReadProcessMemory((int)processHandle, tmp + 16U, lpBuffer, lpBuffer.Length, 0);
            tmp= BitConverter.ToUInt32(lpBuffer, 0);
            ReadProcessMemory((int)processHandle, tmp + 756U, lpBuffer, lpBuffer.Length, 0);
            return BitConverter.ToUInt32(lpBuffer, 0) + 200U;
        }

        public uint GetPlanePointer()
        {
            byte[] lpBuffer = new byte[4];
            uint tmp;
            ReadProcessMemory((int)processHandle, (uint)(11280076 + (int)process.MainModule.BaseAddress), lpBuffer, lpBuffer.Length, 0);
            tmp = BitConverter.ToUInt32(lpBuffer, 0);
            ReadProcessMemory((int)processHandle, tmp + 180U, lpBuffer, lpBuffer.Length, 0);
            tmp = BitConverter.ToUInt32(lpBuffer, 0);
            ReadProcessMemory((int)processHandle, tmp + 304U, lpBuffer, lpBuffer.Length, 0);
            tmp = BitConverter.ToUInt32(lpBuffer, 0);
            ReadProcessMemory((int)processHandle, tmp + 1248U, lpBuffer, lpBuffer.Length, 0);
            return BitConverter.ToUInt32(lpBuffer, 0) + 528U;
        }

        public void Frame()
        {
            if (!Display)
            {
                return;
            }

            DoPeriodicUpdate();
        }

        public void DoPeriodicUpdate()
        {
            byte[] lpBuffer = new byte[4];
            ReadProcessMemory((int)processHandle, values[0], lpBuffer, lpBuffer.Length, 0);
            Car = BitConverter.ToSingle(lpBuffer, 0);
            ReadProcessMemory((int)processHandle, values[1], lpBuffer, lpBuffer.Length, 0);
            Boat = BitConverter.ToSingle(lpBuffer, 0);
            ReadProcessMemory((int)processHandle, values[2], lpBuffer, lpBuffer.Length, 0);
            Plane = BitConverter.ToSingle(lpBuffer, 0);
        }

        public float Car
        {
            get
            {
                if (_car1 > -275.0 && _car1 < 9999.0)
                {
                    return _car1;
                }

                return 0.0f;
            }
            set
            {
                if (value <= (double)_car1 && value >= (double)_car1)
                {
                    return;
                }

                _car1 = value;
                if (CurrentMode != CurrentModeEnum.Car)
                {
                    CurrentMode = CurrentModeEnum.Car;
                }
            }
        }

        public float Boat
        {
            get
            {
                if (_boat > -275.0 && _boat < 9999.0)
                {
                    return _boat * 3.59696f;
                }

                return 0.0f;
            }
            set
            {
                if (value <= (double)_boat && value >= (double)_boat)
                {
                    return;
                }

                _boat = value;
                if (CurrentMode != CurrentModeEnum.Boat)
                {
                    CurrentMode = CurrentModeEnum.Boat;
                }
            }
        }

        public float Plane
        {
            get
            {
                if (_plane > -275.0 && _plane < 9999.0)
                {
                    return _plane * 1.597903f;
                }

                return 0.0f;
            }
            set
            {
                if (value <= (double)_plane && value >= (double)_plane)
                {
                    return;
                }

                _plane = value;
                if (CurrentMode != CurrentModeEnum.Plane)
                {
                    CurrentMode = CurrentModeEnum.Plane;
                }
            }
        }

        public void Dispose()
        {
            Dispose(true);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposing)
            {
                return;
            }

            process.Dispose();
        }
    }
}
