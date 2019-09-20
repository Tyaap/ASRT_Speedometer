using System;
using System.Runtime.InteropServices;
using static Speedo.NativeMethods;

namespace Speedo.Hook
{
    public class Data
    {     
        private UIntPtr processHandle;
        UIntPtr ptrSize = (UIntPtr)IntPtr.Size;
        UIntPtr floatSize = (UIntPtr)Marshal.SizeOf(typeof(float));
        public float speed;
        public VehicleForm form;
        public bool racing;
        public int boostLevel;
        public bool canStunt;
        public bool allStar;
        public bool available;
        private bool readSuccess;

        public Data(int processId)
        {     
            processHandle = OpenProcess(16, false, processId);
        }

        public void GetData(int index)
        {
            UIntPtr tmp = ReadUIntPtr((UIntPtr)0xBCE920);
            UIntPtr playerBase = ReadUIntPtr(tmp + 4 * index);

            speed = Math.Abs(ReadFloat(playerBase + 0xD81C) * 3.593f);
            racing = ReadBoolean(playerBase + 0xEB98);
            form = (VehicleForm)ReadInt(GetServiceAddress(playerBase + 0xC880, ServiceID.RACERTRANSFORMSERVICE) + 0x1C);
            canStunt = ReadBoolean(GetServiceAddress(playerBase + 0xC880, ServiceID.RACERSTUNT) + 0x30);
            allStar = ReadBoolean(GetServiceAddress(playerBase + 0xC880, ServiceID.ALLSTARPOWER) + 0x70);
            boostLevel = ReadInt(GetServiceAddress(playerBase + 0xC880, ServiceID.BOOSTSERVICE) + 0x10C);
            available = readSuccess;
            if (allStar)
            {
                boostLevel = Math.Min(6, boostLevel + 3);
            }
        }

        public int GetPlayerIndex()
        {
            UIntPtr onlineBase = ReadUIntPtr((UIntPtr)0xEC1A88);
            byte count = ReadByte(onlineBase + 0x525);
            int index = 0;
            for (int i = 0; i < count; i++)
            {
                UIntPtr tmp = ReadUIntPtr(onlineBase + 0x528 + 4 * i);
                if (ReadByte(tmp + 0x10) > 0)
                {
                    index += ReadByte(tmp + 0x25D0);
                }
                else
                {
                    return index;
                }
            }
            return index;
        }

        public UIntPtr GetServiceAddress(UIntPtr serviceList, ServiceID serviceId)
        {
            int count = ReadInt(serviceList + 0x840);
            if (count > 48)
            {
                return UIntPtr.Zero;
            }
            int index = 0;
            while (index < count)
            {
                uint currentId = ReadUInt(serviceList + 4);
                if ((uint)serviceId == currentId)
                {
                    return ReadUIntPtr(serviceList);
                }
                serviceList += 0x2C;
                index++;
            }
            return UIntPtr.Zero;
        }

        public int ReadInt(UIntPtr address)
        {
            byte[] lpBuffer = new byte[sizeof(int)];
            readSuccess = ReadProcessMemory(processHandle, address, lpBuffer, (UIntPtr)sizeof(int), UIntPtr.Zero);
            return BitConverter.ToInt32(lpBuffer, 0);
        }

        public uint ReadUInt(UIntPtr address)
        {
            byte[] lpBuffer = new byte[sizeof(uint)];
            readSuccess = ReadProcessMemory(processHandle, address, lpBuffer, (UIntPtr)sizeof(uint), UIntPtr.Zero);
            return BitConverter.ToUInt32(lpBuffer, 0);
        }

        public float ReadFloat(UIntPtr address)
        {
            byte[] lpBuffer = new byte[sizeof(float)];
            readSuccess = ReadProcessMemory(processHandle, address, lpBuffer, (UIntPtr)sizeof(float), UIntPtr.Zero);
            return BitConverter.ToSingle(lpBuffer, 0);
        }

        public bool ReadBoolean(UIntPtr address)
        {
            byte[] lpBuffer = new byte[sizeof(bool)];
            readSuccess = ReadProcessMemory(processHandle, address, lpBuffer, (UIntPtr)sizeof(bool), UIntPtr.Zero);
            return BitConverter.ToBoolean(lpBuffer, 0);
        }

        public UIntPtr ReadUIntPtr(UIntPtr address)
        {
            return (UIntPtr)ReadUInt(address);
        }

        public byte ReadByte(UIntPtr address)
        {
            byte[] lpBuffer = new byte[1];
            readSuccess = ReadProcessMemory(processHandle, address, lpBuffer, (UIntPtr)1, UIntPtr.Zero);
            return lpBuffer[0];
        }
    }
}
