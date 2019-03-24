using System;
using System.Collections.Generic;
using System.Diagnostics;
using static Speedo.NativeMethods;

namespace Speedo.Hook
{
    public class Speed : IDisposable
    {
        private float _plane = 0.0f;
        public int[] addresses = new int[6];
        private Process process = new Process();
        public List<IntPtr> values = new List<IntPtr>();
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

        public IntPtr GetBoatPointer()
        {
            byte[] lpBuffer = new byte[4];
            IntPtr tmp;
            ReadProcessMemory(processHandle, process.MainModule.BaseAddress + 11280076, lpBuffer, (IntPtr)lpBuffer.Length, (IntPtr)0);
            tmp = (IntPtr)BitConverter.ToUInt32(lpBuffer, 0);
            ReadProcessMemory(processHandle, tmp + 184, lpBuffer, (IntPtr)lpBuffer.Length, (IntPtr)0);
            tmp = (IntPtr)BitConverter.ToUInt32(lpBuffer, 0);
            ReadProcessMemory(processHandle, tmp + 304, lpBuffer, (IntPtr)lpBuffer.Length, (IntPtr)0);
            tmp = (IntPtr)BitConverter.ToUInt32(lpBuffer, 0);
            ReadProcessMemory(processHandle, tmp + 1252, lpBuffer, (IntPtr)lpBuffer.Length, (IntPtr)0);
            return (IntPtr)BitConverter.ToUInt32(lpBuffer, 0) + 464;
        }

        public IntPtr GetCarPointer()
        {
            byte[] lpBuffer = new byte[4];
            IntPtr tmp;
            ReadProcessMemory(processHandle, process.MainModule.BaseAddress + 11280076, lpBuffer, (IntPtr)lpBuffer.Length, (IntPtr)0);
            tmp = (IntPtr)BitConverter.ToUInt32(lpBuffer, 0);
            ReadProcessMemory(processHandle, tmp + 176, lpBuffer, (IntPtr)lpBuffer.Length, (IntPtr)0);
            tmp = (IntPtr)BitConverter.ToUInt32(lpBuffer, 0);
            ReadProcessMemory(processHandle, tmp + 16, lpBuffer, (IntPtr)lpBuffer.Length, (IntPtr)0);
            tmp = (IntPtr)BitConverter.ToUInt32(lpBuffer, 0);
            ReadProcessMemory(processHandle, tmp + 756, lpBuffer, (IntPtr)lpBuffer.Length, (IntPtr)0);
            return (IntPtr)BitConverter.ToUInt32(lpBuffer, 0) + 200;
        }

        public IntPtr GetPlanePointer()
        {
            byte[] lpBuffer = new byte[4];
            IntPtr tmp;
            ReadProcessMemory(processHandle, process.MainModule.BaseAddress + 11280076, lpBuffer, (IntPtr)lpBuffer.Length, (IntPtr)0);
            tmp = (IntPtr)BitConverter.ToUInt32(lpBuffer, 0);
            ReadProcessMemory(processHandle, tmp + 180, lpBuffer, (IntPtr)lpBuffer.Length, (IntPtr)0);
            tmp = (IntPtr)BitConverter.ToUInt32(lpBuffer, 0);
            ReadProcessMemory(processHandle, tmp + 304, lpBuffer, (IntPtr)lpBuffer.Length, (IntPtr)0);
            tmp = (IntPtr)BitConverter.ToUInt32(lpBuffer, 0);
            ReadProcessMemory(processHandle, tmp + 1248, lpBuffer, (IntPtr)lpBuffer.Length, (IntPtr)0);
            return (IntPtr)BitConverter.ToUInt32(lpBuffer, 0) + 528;
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
            ReadProcessMemory(processHandle, values[0], lpBuffer, (IntPtr)lpBuffer.Length, (IntPtr)0);
            Car = BitConverter.ToSingle(lpBuffer, 0);
            ReadProcessMemory(processHandle, values[1], lpBuffer, (IntPtr)lpBuffer.Length, (IntPtr)0);
            Boat = BitConverter.ToSingle(lpBuffer, 0);
            ReadProcessMemory(processHandle, values[2], lpBuffer, (IntPtr)lpBuffer.Length, (IntPtr)0);
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
