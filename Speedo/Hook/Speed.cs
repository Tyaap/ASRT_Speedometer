using Speedo.Interface;
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
        private const uint CAR_BASE_POINTER = 11280076;
        private const uint BOAT_BASE_POINTER = 11280076;
        private const uint PLANE_BASE_POINTER = 11280076;
        private float _car1;
        private float _boat;
        private CurrentModeEnum _currentMode;
        private SpeedoInterface _interface;
        private static IntPtr processHandle;
        public bool isInitialised;

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
          ref int lpNumberOfBytesRead);

        public Speed(int processId, SpeedoInterface Interface)
        {
            _interface = Interface;
            if (!isInitialised)
            {
                processHandle = OpenProcess(16, false, processId);
                process = Process.GetProcessById(processId);
            }
            CurrentMode = CurrentModeEnum.Car;
            isInitialised = true;
            try
            {
                values.Clear();
                values.Add(GetCarPointer());
                values.Add(GetBoatPointer());
                values.Add(GetPlanePointer());
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        public uint GetBoatPointer()
        {
            int lpNumberOfBytesRead = 0;
            byte[] lpBuffer = new byte[4];
            uint uint32_1;
            try
            {
                ReadProcessMemory((int)processHandle, (uint)(11280076 + (int)process.MainModule.BaseAddress), lpBuffer, lpBuffer.Length, ref lpNumberOfBytesRead);
                uint uint32_2 = BitConverter.ToUInt32(lpBuffer, 0);
                ReadProcessMemory((int)processHandle, uint32_2 + 184U, lpBuffer, lpBuffer.Length, ref lpNumberOfBytesRead);
                uint uint32_3 = BitConverter.ToUInt32(lpBuffer, 0);
                ReadProcessMemory((int)processHandle, uint32_3 + 304U, lpBuffer, lpBuffer.Length, ref lpNumberOfBytesRead);
                uint uint32_4 = BitConverter.ToUInt32(lpBuffer, 0);
                ReadProcessMemory((int)processHandle, uint32_4 + 1252U, lpBuffer, lpBuffer.Length, ref lpNumberOfBytesRead);
                uint32_1 = BitConverter.ToUInt32(lpBuffer, 0);
            }
            catch (Exception)
            {
                throw;
            }
            return uint32_1 + 464U;
        }

        public uint GetCarPointer()
        {
            int lpNumberOfBytesRead = 0;
            byte[] lpBuffer = new byte[4];
            ReadProcessMemory((int)processHandle, (uint)(11280076 + (int)process.MainModule.BaseAddress), lpBuffer, lpBuffer.Length, ref lpNumberOfBytesRead);
            uint uint32_1 = BitConverter.ToUInt32(lpBuffer, 0);
            ReadProcessMemory((int)processHandle, uint32_1 + 176U, lpBuffer, lpBuffer.Length, ref lpNumberOfBytesRead);
            uint uint32_2 = BitConverter.ToUInt32(lpBuffer, 0);
            ReadProcessMemory((int)processHandle, uint32_2 + 16U, lpBuffer, lpBuffer.Length, ref lpNumberOfBytesRead);
            uint uint32_3 = BitConverter.ToUInt32(lpBuffer, 0);
            ReadProcessMemory((int)processHandle, uint32_3 + 756U, lpBuffer, lpBuffer.Length, ref lpNumberOfBytesRead);
            return BitConverter.ToUInt32(lpBuffer, 0) + 200U;
        }

        public uint GetPlanePointer()
        {
            int lpNumberOfBytesRead = 0;
            byte[] lpBuffer = new byte[4];
            ReadProcessMemory((int)processHandle, (uint)(11280076 + (int)process.MainModule.BaseAddress), lpBuffer, lpBuffer.Length, ref lpNumberOfBytesRead);
            uint uint32_1 = BitConverter.ToUInt32(lpBuffer, 0);
            ReadProcessMemory((int)processHandle, uint32_1 + 180U, lpBuffer, lpBuffer.Length, ref lpNumberOfBytesRead);
            uint uint32_2 = BitConverter.ToUInt32(lpBuffer, 0);
            ReadProcessMemory((int)processHandle, uint32_2 + 304U, lpBuffer, lpBuffer.Length, ref lpNumberOfBytesRead);
            uint uint32_3 = BitConverter.ToUInt32(lpBuffer, 0);
            ReadProcessMemory((int)processHandle, uint32_3 + 1248U, lpBuffer, lpBuffer.Length, ref lpNumberOfBytesRead);
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
            int lpNumberOfBytesRead = 0;
            byte[] lpBuffer = new byte[4];
            ReadProcessMemory((int)processHandle, values[0], lpBuffer, lpBuffer.Length, ref lpNumberOfBytesRead);
            Car = BitConverter.ToSingle(lpBuffer, 0);
            ReadProcessMemory((int)processHandle, values[1], lpBuffer, lpBuffer.Length, ref lpNumberOfBytesRead);
            Boat = BitConverter.ToSingle(lpBuffer, 0);
            ReadProcessMemory((int)processHandle, values[2], lpBuffer, lpBuffer.Length, ref lpNumberOfBytesRead);
            Plane = BitConverter.ToSingle(lpBuffer, 0);
        }

        protected void DebugMessage(string message)
        {
            _interface.Message(MessageType.Debug, "Speed: : " + message);
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

        public CurrentModeEnum CurrentMode
        {
            get => _currentMode;
            set => _currentMode = value;
        }

        public bool Display { get; set; }

        ~Speed()
        {
            Dispose(false);
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
            _interface = null;
            processHandle = IntPtr.Zero;
        }

        public enum CurrentModeEnum
        {
            Car,
            Boat,
            Plane,
        }
    }
}
