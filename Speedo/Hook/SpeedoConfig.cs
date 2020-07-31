using System;

namespace Speedo.Hook
{
    [Serializable]
    public class SpeedoConfig
    {
        public int PosX = 50;
        public int PosY = 120;
        public float Scale = 1.0f;
        public byte Opacity = 255;
        public bool AlwaysShow = true;
        public string Theme = "Xenn";
        public SpeedType SpeedType = SpeedType.PositionIGT;
        public int SmoothingFrames = 4;
        public bool Enabled = true;
    }
}