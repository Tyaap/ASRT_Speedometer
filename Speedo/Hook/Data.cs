using Speedo.Interface;
using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Security.Cryptography.X509Certificates;
using static MemoryHelper;

namespace Speedo.Hook
{
    public class Data
    {     
        public float speed;
        public VehicleForm form;
        public bool racing;
        public int boostLevel;
        public bool canStunt;
        public bool allStar;
        public bool available;
        private double lastTime = 0;
        private float[] lastPosition = new float[3] { 0, 0, 0 };
        private float lastSpeed = 0;
        private int playerIndex = 0;
        private float multiplier = 3.593f;
        private Stopwatch stopwatch = new Stopwatch();

        

        // buffers for frame smoothing
       
        private int currentBufferPos = 0;
        private float sumDt;
        private float sumDl;
        private float sumSpeed;
        private float[] dtBuffer;
        private float[] dlBuffer;
        private float[] speedBuffer;

        public SpeedType speedType = SpeedType.Momentum;
        public int bufferSize = 1;

        public Data()
        {
            stopwatch.Start();
            dtBuffer = new float[bufferSize];
            dlBuffer = new float[bufferSize];
            speedBuffer = new float[bufferSize];
        }

        public Data(SpeedoConfig config)
        {
            stopwatch.Start();
            UpdateConfig(config);
        }

        public void UpdateConfig(SpeedoConfig config)
        {
            speedType = config.SpeedType;
            bufferSize = 1 + config.SmoothingFrames;
            dtBuffer = new float[bufferSize];
            dlBuffer = new float[bufferSize];
            speedBuffer = new float[bufferSize];
            sumDl = 0;
            sumDt = 0;
            sumSpeed = 0;
        }

        public void GetData()
        {
            GetData(GetPlayerIndex());
        }

        public void GetData(int index)
        {
            UIntPtr tmp = ReadUIntPtr(0xBCE920);
            UIntPtr playerBase = ReadUIntPtr(tmp + 4 * index);
            available = TimerRunning();
            racing = available && Racing(playerBase);
            if (!available)
            {
                speed = 0;
                lastSpeed = 0;
                form = 0;
                canStunt = false;
                allStar = false;
                return;
            }

            switch (speedType)
            {
                case SpeedType.PositionIGT:
                    speed = PositionBasedSpeed(index, false);
                    break;
                case SpeedType.PositionRT:
                    speed = PositionBasedSpeed(index, true);
                    break;
                case SpeedType.Momentum:
                    speed = MomentumBasedSpeed(playerBase, index);
                    break;
            }
            
            form = (VehicleForm)ReadInt(GetServiceAddress(playerBase + 0xC880, ServiceID.RACERTRANSFORMSERVICE) + 0x1C);
            canStunt = ReadBoolean(GetServiceAddress(playerBase + 0xC880, ServiceID.RACERSTUNT) + 0x30);
            allStar = ReadBoolean(GetServiceAddress(playerBase + 0xC880, ServiceID.ALLSTARPOWER) + 0x70);
            boostLevel = ReadInt(GetServiceAddress(playerBase + 0xC880, ServiceID.BOOSTSERVICE) + 0x10C);
            if (allStar)
            {
                boostLevel = Math.Min(6, boostLevel + 3);
            }
        }

        public float MomentumBasedSpeed(UIntPtr playerBase, int index)
        {
            currentBufferPos++;
            if (currentBufferPos >= bufferSize)
                currentBufferPos = 0;
            sumSpeed -= speedBuffer[currentBufferPos];
            float speed = Math.Abs(ReadFloat(playerBase + 0xD81C) * 3.593f);
            speedBuffer[currentBufferPos] = speed;
            sumSpeed += speed;
            return sumSpeed / bufferSize;
        }

        public float PositionBasedSpeed(int index, bool realTime)
        {
            currentBufferPos++;
            if (currentBufferPos >= bufferSize)
                currentBufferPos = 0;

            double time = GetTime(realTime);
            float dt = (float)(time - lastTime);
            sumDt -= dtBuffer[currentBufferPos];
            dtBuffer[currentBufferPos] = dt;
            sumDt += dt;
            lastTime = time;

            float[] position = GetPosition(index);
            float dl = Distance(position, lastPosition);
            sumDl -= dlBuffer[currentBufferPos];
            dlBuffer[currentBufferPos] = dl;
            sumDl += dl;
            lastPosition = position;

            if (sumDt > 0.001 && sumDl / sumDt < 5000) // ignore large/negative speeds
            {
                lastSpeed = sumDl / sumDt * multiplier;
            }
            return lastSpeed;
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

        public float[] GetPosition(int index)
        {
            UIntPtr tmp = ReadUIntPtr(0xBC3E28);
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

        public double GetTime(bool realTime)
        {
            return realTime ? stopwatch.Elapsed.TotalSeconds : ReadFloat(0xBCE980);
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
