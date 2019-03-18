using System;
using System.Drawing;

namespace Speedo_Loader
{
    internal class ResolutionScaler
    {
        public static void ReadResolutionString(string text, int width, int height)
        {
            ResX = int.Parse(text.Split('x', 'X', '*')[0]);
            ResXMultiplier = GetResolutionMultiplier(width, ResX);
            ResY = int.Parse(text.Split('x', 'X', '*')[1]);
            ResYMultiplier = GetResolutionMultiplier(height, ResY);
            GetResolutionValues(new Point(ResX, ResY));
            AspectRatio = GetAspectRatio(ResX, ResY);
        }

        public static double GetResolutionMultiplier(int Original, int Resized)
        {
            return Resized / (double)Original;
        }

        public static Point SetResolutionValues(Point res)
        {
            return new Point()
            {
                X = (int)Math.Round(res.X * ResXMultiplier, 0),
                Y = (int)Math.Round(res.Y * ResYMultiplier, 0)
            };
        }

        public static Point GetResolutionValues(Point res)
        {
            return new Point()
            {
                X = (int)Math.Round(res.X * ResXMultiplier, 0),
                Y = (int)Math.Round(res.Y * ResYMultiplier, 0)
            };
        }

        public static double GetAspectRatio(int x, int y)
        {
            return x / (double)y;
        }

        public static int ResX { get; set; }

        public static int ResY { get; set; }

        public static double ResXMultiplier { get; set; }

        public static double ResYMultiplier { get; set; }

        public static double AspectRatio { get; set; }
    }
}
