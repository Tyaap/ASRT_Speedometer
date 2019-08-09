using System;
using System.Runtime.InteropServices;
using static Speedo.NativeMethods;

namespace Speedo.Hook
{
    public class Speed : IDisposable
    {
        private float _plane = 0.0f;
        private float _car1;
        private float _boat;
        public CurrentModeEnum CurrentMode;
        private UIntPtr processHandle;
        public bool Display = true;
        UIntPtr baseAddress = (UIntPtr)0xEC1ECC;
        UIntPtr ptrSize = (UIntPtr)IntPtr.Size;
        UIntPtr floatSize = (UIntPtr)Marshal.SizeOf(typeof(float));

        public enum CurrentModeEnum
        {
            Car,
            Boat,
            Plane,
        }

        public Speed(int processId)
        {     
            processHandle = OpenProcess(16, false, processId);     
            CurrentMode = CurrentModeEnum.Car;
        }

        public UIntPtr GetBoatPointer()
        {
            byte[] lpBuffer = new byte[ptrSize.ToUInt32()];
            ReadProcessMemory(processHandle, baseAddress, lpBuffer, ptrSize, UIntPtr.Zero);
            ReadProcessMemory(processHandle, (UIntPtr)BitConverter.ToUInt32(lpBuffer, 0) + 184, lpBuffer, ptrSize, UIntPtr.Zero);
            ReadProcessMemory(processHandle, (UIntPtr)BitConverter.ToUInt32(lpBuffer, 0) + 304, lpBuffer, ptrSize, UIntPtr.Zero);
            ReadProcessMemory(processHandle, (UIntPtr)BitConverter.ToUInt32(lpBuffer, 0) + 1252, lpBuffer, ptrSize, UIntPtr.Zero);
            return (UIntPtr)BitConverter.ToUInt32(lpBuffer, 0) + 464;
        }

        public UIntPtr GetCarPointer()
        {
            byte[] lpBuffer = new byte[ptrSize.ToUInt32()];
            ReadProcessMemory(processHandle, (UIntPtr)0xEC1ECC, lpBuffer, ptrSize, UIntPtr.Zero);
            ReadProcessMemory(processHandle, (UIntPtr)BitConverter.ToUInt32(lpBuffer, 0) + 0xB0, lpBuffer, ptrSize, UIntPtr.Zero);
            ReadProcessMemory(processHandle, (UIntPtr)BitConverter.ToUInt32(lpBuffer, 0) + 0x10, lpBuffer, ptrSize, UIntPtr.Zero);
            ReadProcessMemory(processHandle, (UIntPtr)BitConverter.ToUInt32(lpBuffer, 0) + 0x2F4, lpBuffer, ptrSize, UIntPtr.Zero);
            return (UIntPtr)BitConverter.ToUInt32(lpBuffer, 0) + 0xC8;
        }

        public UIntPtr GetPlanePointer()
        {
            byte[] lpBuffer = new byte[ptrSize.ToUInt32()];
            ReadProcessMemory(processHandle, (UIntPtr)0xEC1ECC, lpBuffer, ptrSize, UIntPtr.Zero);
            ReadProcessMemory(processHandle, (UIntPtr)BitConverter.ToUInt32(lpBuffer, 0) + 180, lpBuffer, ptrSize, UIntPtr.Zero);
            ReadProcessMemory(processHandle, (UIntPtr)BitConverter.ToUInt32(lpBuffer, 0) + 304, lpBuffer, ptrSize, UIntPtr.Zero);
            ReadProcessMemory(processHandle, (UIntPtr)BitConverter.ToUInt32(lpBuffer, 0) + 1248, lpBuffer, ptrSize, UIntPtr.Zero);
            return (UIntPtr)BitConverter.ToUInt32(lpBuffer, 0) + 528;
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
            byte[] lpBuffer = new byte[floatSize.ToUInt32()];
            bool isAvailable = ReadProcessMemory(processHandle, GetCarPointer(), lpBuffer, floatSize, UIntPtr.Zero);
            Car = BitConverter.ToSingle(lpBuffer, 0);
            ReadProcessMemory(processHandle, GetBoatPointer(), lpBuffer, floatSize, UIntPtr.Zero);
            Boat = BitConverter.ToSingle(lpBuffer, 0);
            ReadProcessMemory(processHandle, GetPlanePointer(), lpBuffer, floatSize, UIntPtr.Zero);
            Plane = BitConverter.ToSingle(lpBuffer, 0);
            Display = Display && isAvailable;
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

        public void Dispose() => Dispose(true);

        protected virtual void Dispose(bool disposing)
        {
            if (!disposing)
            {
                return;
            }
        }
    }
}
