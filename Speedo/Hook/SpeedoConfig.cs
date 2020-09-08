using System;

namespace Speedo.Hook
{
    [Serializable]
    public class SpeedoConfig
    {
        public int PosX = 0;
        public int PosY = 0;
        public float Scale = 1.0f;
        public byte Opacity = 0;
        public bool AlwaysShow = false;
        public string Theme = "";
        public bool Enabled = false;
    }
}