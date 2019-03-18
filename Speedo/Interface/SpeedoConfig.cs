using System;

namespace Speedo.Interface
{
    [Serializable]
    public class SpeedoConfig
    {
        public bool ShowOverlay { get; set; }

        public int PosX { get; set; }

        public int PosY { get; set; }

        public double Scale { get; set; }

        public bool AlwaysShow { get; set; }

        public SpeedoConfig()
        {
            ShowOverlay = true;
            PosX = 50;
            PosY = 120;
            Scale = 1.0;
            AlwaysShow = true;
        }
    }
}
