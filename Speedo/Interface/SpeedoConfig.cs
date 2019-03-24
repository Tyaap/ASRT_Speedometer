using System;

namespace Speedo.Interface
{
    [Serializable]
    public class SpeedoConfig
    {
        public int PosX;
        public int PosY;
        public double Scale;
        public bool AlwaysShow;

        public SpeedoConfig()
        {
            PosX = 50;
            PosY = 120;
            Scale = 1.0;
            AlwaysShow = true;
        }
    }
}
