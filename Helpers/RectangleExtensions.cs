using System.Drawing;

namespace AimBot.Helpers
{
    public static class RectangleExtensions
    {
        public static Point Center(this Rectangle rect)
        {
            return new Point(rect.X + rect.Width / 2, rect.Y + rect.Height / 2);
        }

        public static Point TopRight(this Rectangle rect)
        {
            return new Point(rect.X + rect.Width, rect.Y);
        }

        public static Point BottomRight(this Rectangle rect)
        {
            return new Point(rect.X + rect.Width, rect.Y + rect.Height);
        }

        public static Point CenterRight(this Rectangle rect)
        {
            return new Point(rect.X + rect.Width, rect.Y + rect.Height / 2);
        }
    }
}
