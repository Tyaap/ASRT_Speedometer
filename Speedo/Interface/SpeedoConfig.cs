using System;

namespace Speedo.Interface
{
    [Serializable]
    public class SpeedoConfig
    {
        public int PosX;
        public int PosY;
        public float Scale;
        public byte Opacity;
        public bool AlwaysShow;
        public string Theme;
        public bool Enabled;

        public SpeedoConfig()
        {
            PosX = 50;
            PosY = 120;
            Scale = 1.0f;
            Opacity = 255;
            Theme = "Xenn";
            AlwaysShow = true;
            Enabled = true;
        }
    }
}
