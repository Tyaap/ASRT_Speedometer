using System;
using System.Drawing;

namespace Speedo_Loader
{
    internal class ResolutionScaler
    {
        public static bool ReadResolutionString(string text, int width, int height)
        {
            string[] strings = text.Split(new char[] { 'x', 'X', '*' }, 2);
            if (strings.Length != 2)
            {
                return false;
            }

            if (int.TryParse(strings[0], out int x) && x >= 100 && x <= 10000 &&
                int.TryParse(strings[1], out int y) && y >= 100 && y <= 10000)
            {
                ResX = x;
                ResY = y;
                ResXMultiplier = GetResolutionMultiplier(width, ResX);
                ResYMultiplier = GetResolutionMultiplier(height, ResY);
                GetResolutionValues(new Point(ResX, ResY));
                AspectRatio = GetAspectRatio(ResX, ResY);
                return true;
            }

            return false;
        }

        public static decimal GetResolutionMultiplier(int Original, int Resized)
        {
            return Resized / (decimal)Original;
        }

        public static Point SetResolutionValues(Point res)
        {
            return new Point((int)(res.X * ResXMultiplier), (int)(res.Y * ResYMultiplier));
        }

        public static Point GetResolutionValues(Point res)
        {
            return new Point((int)(res.X * ResXMultiplier), (int)(res.Y * ResYMultiplier));
        }

        public static decimal GetAspectRatio(int x, int y)
        {
            return x / (decimal)y;
        }

        public static int ResX { get; set; }

        public static int ResY { get; set; }

        public static decimal ResXMultiplier { get; set; }

        public static decimal ResYMultiplier { get; set; }

        public static decimal AspectRatio { get; set; }
    }
}
