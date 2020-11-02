using System;
using System.Drawing;

namespace AimBot.Helpers
{
    public static class PointExtensions
    {
        public static int DistanceSq(this Point p1, Point p2)
        {
            var vx = p2.X - p1.X;
            var vy = p2.Y - p1.Y;
            return vx * vx + vy * vy;
        }

        public static double Distance(this Point p1, Point p2)
        {
            var vx = p2.X - p1.X;
            var vy = p2.Y - p1.Y;
            return Math.Sqrt(vx * vx + vy * vy);
        }
    }
}
