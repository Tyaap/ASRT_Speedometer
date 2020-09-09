using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Security.Cryptography.X509Certificates;
using System.Windows.Forms;

using static MemoryHelper;

namespace Speedo.Hook
{
    public class Data
    {
        private delegate void CalculateSpeedDelegate(int dataPtr, int index);

        public float speed;
        public VehicleForm form;
        public bool racing;
        public int boostLevel;
        public bool canStunt;
        public bool allStar;
        public bool available;

        private static float[] speeds;
        private int playerIndex = 0;
        private const float speedMultiplier = 3.593f;
        private CalculateSpeedDelegate calculateSpeed = new CalculateSpeedDelegate(CalculateSpeed);
        private int calculateSpeedPtr;

        public Data()
        {
            speeds = new float[10];
            calculateSpeedPtr = Marshal.GetFunctionPointerForDelegate(calculateSpeed).ToInt32();
            GamePatch();
        }

        // Patch to retrieve accurate speed values
        public void GamePatch()
        {
            int callCodePtr = MemoryHelper.Allocate(0, 100);
            List<byte> callCode = new List<byte>();
            // backup registers
            callCode.Add(0x50); // push eax
            callCode.Add(0x51); // push ecx
            callCode.Add(0x52); // push edx
            // call the managed function
            callCode.Add(0x53); // push ebx
            callCode.Add(0x57); // push edi
            callCode.Add(0xE8); // call ...
            callCode.AddRange(BitConverter.GetBytes(calculateSpeedPtr - (callCodePtr + callCode.Count + 4))); // ... CalculateSpeed
            // restore registers
            callCode.Add(0x5a); // pop edx
            callCode.Add(0x59); // pop ecx
            callCode.Add(0x58); // pop eax
            // run original code and return
            callCode.AddRange(new byte[] { 0x0F, 0x28, 0x87, 0x20, 0x01, 0x00, 0x00 }); // movaps xmm0, [edi + 120]
            callCode.Add(0xC3); // ret
            MemoryHelper.Write(callCodePtr, callCode.ToArray());

            List<byte> jumpCode = new List<byte>();
            jumpCode.Add(0xE8); // call ...
            jumpCode.AddRange(BitConverter.GetBytes(callCodePtr - 0x4308A2 - 5)); // ... callCodePtr
            jumpCode.Add(0x90); // nop 
            jumpCode.Add(0x90); // nop
            MemoryHelper.Write(0x4308A2, jumpCode.ToArray());
        }

        private static void CalculateSpeed(int dataPtr, int index)
        {
            if (index > 10)
            {
                // there are brief moments when >10 players are registered, which should be ignored
                return;
            }
            float dl2 = 0;
            for (int i = 0; i < 3; i++) // XYZ components
            {
                float dx = ReadFloat(dataPtr + 0x120 + i * 4) - ReadFloat(dataPtr + 0x170 + i * 4);
                dl2 += dx * dx;
            }
            speeds[index - 1] = (float)Math.Sqrt(dl2) * 60 * speedMultiplier; // speed = dl/dt * speedMultiplier
        }

        public void GetData()
        {
            GetData(GetPlayerIndex());
        }

        public void GetData(int index)
        {
            UIntPtr tmp = ReadUIntPtr(0xBCE920);
            UIntPtr playerBase = ReadUIntPtr(tmp + 4 * index);
            racing = Racing(playerBase) && TimerRunning();
            available = MemoryHelper.readSuccess;
            if (!available)
            {
                speed = 0;
                form = 0;
                canStunt = false;
                allStar = false;
                return;
            }

            speed = speeds[index];
            form = (VehicleForm)ReadInt(GetServiceAddress(playerBase + 0xC880, ServiceID.RACERTRANSFORMSERVICE) + 0x1C);
            canStunt = ReadBoolean(GetServiceAddress(playerBase + 0xC880, ServiceID.RACERSTUNT) + 0x30);
            allStar = ReadBoolean(GetServiceAddress(playerBase + 0xC880, ServiceID.ALLSTARPOWER) + 0x70);
            boostLevel = ReadInt(GetServiceAddress(playerBase + 0xC880, ServiceID.BOOSTSERVICE) + 0x10C);
            if (allStar)
            {
                boostLevel = Math.Min(6, boostLevel + 3);
            }
        }

        public bool TimerRunning()
        {
            UIntPtr tmp = ReadUIntPtr(0xBCEE9C);
            return ReadBoolean(tmp + 0x18);
        }

        public bool Racing(UIntPtr playerBase)
        {
            return ReadBoolean(playerBase + 0xEB98);
        }

        public int GetPlayerIndex()
        {
            UIntPtr onlineBase = ReadUIntPtr(0xEC1A88);
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
    }
}
