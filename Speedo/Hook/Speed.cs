using System;
using System.Diagnostics;
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
        private float lastTime = 0;
        private float[] lastPosition = new float[3] { 0, 0, 0 };
        private float lastSpeed = 0;
        private int playerIndex = 0;

        public Data(UIntPtr processHandle)
        {     
            this.processHandle = processHandle;
        }

        public void GetData(int index)
        {
            UIntPtr tmp = ReadUIntPtr((UIntPtr)0xBCE920);
            UIntPtr playerBase = ReadUIntPtr(tmp + 4 * index);

            speed = GetSpeed(index) * 3.593f;
            racing = ReadBoolean(playerBase + 0xEB98);
            form = (VehicleForm)ReadInt(GetServiceAddress(playerBase + 0xC880, ServiceID.RACERTRANSFORMSERVICE) + 0x1C);
            canStunt = ReadBoolean(GetServiceAddress(playerBase + 0xC880, ServiceID.RACERSTUNT) + 0x30);
            allStar = ReadBoolean(GetServiceAddress(playerBase + 0xC880, ServiceID.ALLSTARPOWER) + 0x70);
            boostLevel = ReadInt(GetServiceAddress(playerBase + 0xC880, ServiceID.BOOSTSERVICE) + 0x10C);
            available = readSuccess;
            if (!available)
            {
                speed = 0;
                lastSpeed = 0;
            }
            if (allStar)
            {
                boostLevel = Math.Min(6, boostLevel + 3);
            }
        }

        public float GetSpeed(int index)
        {
            float time = GetTime();
            float dt = time - lastTime;
            float[] position = GetPosition(index);
            float dl = Distance(position, lastPosition);
            if (dl != 0 && dt != 0)
            {
                lastSpeed = dl / dt;
                lastTime = time;
                lastPosition = position;
            }
            return lastSpeed;
        }

        public float[] GetPosition(int index)
        {
            UIntPtr tmp = ReadUIntPtr((UIntPtr)0xBC3E28);
            tmp = ReadUIntPtr(tmp + 0x160);
            tmp = ReadUIntPtr(tmp + 0x8);
            tmp = ReadUIntPtr(tmp + 0xD0);
            tmp = ReadUIntPtr(tmp + 0x68);
            tmp = ReadUIntPtr(tmp + 4 * index);
            return new float[3] { ReadFloat(tmp + 0x170), ReadFloat(tmp + 0x174), ReadFloat(tmp + 0x178) };
        }

        public float Distance(float[] pos1, float[] pos2)
        {
            float d = 0;
            for (int i = 0; i < 3; i++)
            {
                d += (pos1[i] - pos2[i]) * (pos1[i] - pos2[i]);
            }
            return (float)Math.Sqrt(d);
        }

        public float GetTime()
        {
            return ReadFloat((UIntPtr)0xBCE980);
        }

        public int GetPlayerIndex()
        {
            UIntPtr onlineBase = ReadUIntPtr((UIntPtr)0xEC1A88);
            byte count = ReadByte(onlineBase + 0x525);
            if (count == 0) // offline race
            {
                playerIndex = 0;
            }
            else if (ReadByte(onlineBase + 0x101D64 + 0xE) != 3) // Online race, do not update index if race is in progress (lobbyState = 3).
            {
                playerIndex = 0;
                for (int i = 0; i < count; i++) // iterate over player list
                {
                    UIntPtr playerPtr = ReadUIntPtr(onlineBase + 0x528 + 4 * i);
                    if (ReadByte(playerPtr + 0x10) == 0) 
                    {
                        break; // player found
                    }
                    playerIndex += ReadByte(playerPtr + 0x25D0); // + (number of local players)
                }
            }
            return playerIndex;
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
            readSuccess = ReadProcessMemory(processHandle, address, lpBuffer, sizeof(int), UIntPtr.Zero);
            return BitConverter.ToInt32(lpBuffer, 0);
        }

        public uint ReadUInt(UIntPtr address)
        {
            byte[] lpBuffer = new byte[sizeof(uint)];
            readSuccess = ReadProcessMemory(processHandle, address, lpBuffer, sizeof(uint), UIntPtr.Zero);
            return BitConverter.ToUInt32(lpBuffer, 0);
        }

        public float ReadFloat(UIntPtr address)
        {
            byte[] lpBuffer = new byte[sizeof(float)];
            readSuccess = ReadProcessMemory(processHandle, address, lpBuffer, sizeof(float), UIntPtr.Zero);
            return BitConverter.ToSingle(lpBuffer, 0);
        }

        public bool ReadBoolean(UIntPtr address)
        {
            byte[] lpBuffer = new byte[sizeof(bool)];
            readSuccess = ReadProcessMemory(processHandle, address, lpBuffer, sizeof(bool), UIntPtr.Zero);
            return BitConverter.ToBoolean(lpBuffer, 0);
        }

        public UIntPtr ReadUIntPtr(UIntPtr address)
        {
            return (UIntPtr)ReadUInt(address);
        }

        public byte ReadByte(UIntPtr address)
        {
            byte[] lpBuffer = new byte[1];
            readSuccess = ReadProcessMemory(processHandle, address, lpBuffer, 1, UIntPtr.Zero);
            return lpBuffer[0];
        }
    }
}
