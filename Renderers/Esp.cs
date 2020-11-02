using System;
using System.Drawing;

namespace AimBot.Renderers
{
    public readonly struct TextShape
    {
        public readonly Point Position;
        public readonly string Text;
        public readonly Color Color;
        public readonly int Size;

        public TextShape(Point position, string text, Color color, int size)
        {
            Position = position;
            Text = text;
            Color = color;
            Size = size;
        }
    }

    public readonly struct RectangleShape
    {
        /// <summary>
        /// The screen-space rectangle coordinates.
        /// </summary>
        public readonly Rectangle Rectangle;

        /// <summary>
        /// The fill color.
        /// </summary>
        public readonly Color FillColor;

        /// <summary>
        /// The stroke color.
        /// </summary>
        public readonly Color StrokeColor;

        /// <summary>
        /// The stroke thickness (in pixels).
        /// </summary>
        public readonly int StrokeThickness;

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="rectangle">The screen-space rectangle coordinates.</param>
        /// <param name="fillColor">The fill color.</param>
        /// <param name="strokeColor">The stroke color.</param>
        /// <param name="strokeThickness">The stroke thickness (in pixels).</param>
        public RectangleShape(Rectangle rectangle, Color fillColor, Color strokeColor, int strokeThickness)
        {
            Rectangle = rectangle;
            FillColor = fillColor;
            StrokeColor = strokeColor;
            StrokeThickness = strokeThickness;
        }
    }

    public readonly struct CircleShape
    {
        /// <summary>
        /// The screen-space center position.
        /// </summary>
        public readonly Point Center;

        /// <summary>
        /// The radius.
        /// </summary>
        public readonly double Radius;

        /// <summary>
        /// The fill color.
        /// </summary>
        public readonly Color FillColor;

        /// <summary>
        /// The stroke color.
        /// </summary>
        public readonly Color StrokeColor;

        /// <summary>
        /// The stroke thickness (in pixels).
        /// </summary>
        public readonly int StrokeThickness;

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="center">The screen-space center position.</param>
        /// <param name="radius">The radius.</param>
        /// <param name="fillColor">The fill color.</param>
        /// <param name="strokeColor">The stroke color.</param>
        /// <param name="strokeThickness">The stroke thickness.</param>
        public CircleShape(Point center, double radius, Color fillColor, Color strokeColor, int strokeThickness)
        {
            Center = center;
            Radius = radius;
            FillColor = fillColor;
            StrokeColor = strokeColor;
            StrokeThickness = strokeThickness;
        }
    }

    public interface Esp
    {
        void Start(IntPtr windowHandle);
        void Stop();
        void Refit();
        void Clear();
        void Add(RectangleShape rectangle);
        void Add(CircleShape circle);
        void Add(TextShape text);
        void SwapBuffers();
    }
}
