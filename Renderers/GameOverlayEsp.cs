using System;
using System.Collections.Generic;

using GameOverlay.Drawing;
using GameOverlay.Windows;

namespace AimBot.Renderers
{
    public class GameOverlayEsp : Esp
    {
        private class Buffer
        {
            public readonly List<RectangleShape> Rectangles;
            public readonly List<CircleShape> Circles;
            public readonly List<TextShape> Text;

            public Buffer()
            {
                Rectangles = new List<RectangleShape>();
                Circles = new List<CircleShape>();
                Text = new List<TextShape>();
            }

            public void Clear()
            {
                Rectangles.Clear();
                Circles.Clear();
                Text.Clear();
            }
        }

        private IntPtr handle;
        private GraphicsWindow window;
        private Font defaultFont;
        private readonly Dictionary<Color, SolidBrush> brushes;
        private Buffer read;
        private Buffer write;
        private readonly object bufferLock;

        public GameOverlayEsp()
        {
            window = null;
            brushes = new Dictionary<Color, SolidBrush>();
            read = new Buffer();
            write = new Buffer();
            bufferLock = new object();
        }

        public void Start(IntPtr windowHandle)
        {
            Stop();

            handle = windowHandle;
            var graphics = new Graphics(windowHandle);
            window = new GraphicsWindow(graphics) { FPS = 120, IsTopmost = true, IsVisible = true };

            window.DrawGraphics += OnDrawGraphics;
            window.DestroyGraphics += OnDestroyGraphics;
            window.SetupGraphics += OnSetupGraphics;

            window.VisibilityChanged += OnVisibilityChanged;

            window.Create();
            Refit();
        }

        private void OnVisibilityChanged(object sender, OverlayVisibilityEventArgs e)
        {
            throw new NotImplementedException(); // TODO:
        }

        public void Stop()
        {
            if (window != null)
            {
                window.DrawGraphics -= OnDrawGraphics;
                window.DestroyGraphics -= OnDestroyGraphics;
                window.SetupGraphics -= OnSetupGraphics;
                window.Dispose();
                window = null;
            }

            brushes.Clear();

            lock (bufferLock)
            {
                read.Clear();
                write.Clear();
            }
        }

        public void Refit()
        {
            if (window != null)
            {
                window.PlaceAbove(handle);
                window.FitTo(handle, true);
            }
        }

        private void OnSetupGraphics(object sender, SetupGraphicsEventArgs e)
        {
            if (e.RecreateResources)
            {
                defaultFont?.Dispose();
                defaultFont = null;

                foreach (var kv in brushes)
                {
                    kv.Value.Dispose();
                }

                brushes.Clear();
            }

            var gfx = e.Graphics;
            defaultFont = gfx.CreateFont("Microsoft Sans Serif", 18, true, false, false);
        }

        private void OnDestroyGraphics(object sender, DestroyGraphicsEventArgs e)
        {
            defaultFont?.Dispose();
            defaultFont = null;

            foreach (var kv in brushes)
            {
                kv.Value.Dispose();
            }

            brushes.Clear();

            lock (bufferLock)
            {
                read.Clear();
                write.Clear();
            }
        }

        private void OnDrawGraphics(object sender, DrawGraphicsEventArgs e)
        {
            window.PlaceAbove(handle); // FIXME: Sometimes the overlay goes behind the target window.

            var gfx = e.Graphics;

            gfx.ClearScene();

            lock (bufferLock)
            {
                var clientRect = Helpers.ScreenHelper.ClientRectangle(handle);
                foreach (var rectangle in read.Rectangles)
                {
                    var geom = new Rectangle(rectangle.Rectangle.Left - clientRect.Left,
                                             rectangle.Rectangle.Top - clientRect.Top,
                                             rectangle.Rectangle.Right - clientRect.Left,
                                             rectangle.Rectangle.Bottom - clientRect.Top);

                    if (rectangle.FillColor.A > 0)
                    {
                        gfx.DrawRectangle(Brush(rectangle.FillColor), geom, 0);
                    }

                    if (rectangle.StrokeThickness > 0 && rectangle.StrokeColor.A > 0)
                    {
                        gfx.DrawRectangleEdges(Brush(rectangle.StrokeColor), geom, rectangle.StrokeThickness);
                    }
                }

                foreach (var circle in read.Circles)
                {
                    var geom = new Circle(new Point(circle.Center.X - clientRect.Left,
                                                    circle.Center.Y - clientRect.Top), (float)circle.Radius);

                    gfx.DrawCircle(Brush(circle.StrokeColor), geom, circle.StrokeThickness);
                }

                foreach (var text in read.Text)
                {
                    gfx.DrawText(defaultFont, text.Size, Brush(text.Color), text.Position.X - clientRect.Left, text.Position.Y - clientRect.Top, text.Text);
                }
            }
        }

        private SolidBrush Brush(System.Drawing.Color color)
        {
            return Brush(new Color(color.R, color.G, color.B, color.A));
        }

        private SolidBrush Brush(Color color)
        {
            if (brushes.TryGetValue(color, out var brush) == false)
            {
                brush = window.Graphics.CreateSolidBrush(color);
                brushes.Add(color, brush);
            }

            return brush;
        }

        public void Add(RectangleShape rectangle)
        {
            write.Rectangles.Add(rectangle);
        }

        public void Add(CircleShape circle)
        {
            write.Circles.Add(circle);
        }

        public void Add(TextShape text)
        {
            write.Text.Add(text);
        }

        public void Clear()
        {
            lock (bufferLock)
            {
                write.Rectangles.Clear();
                write.Text.Clear();
            }
        }

        public void SwapBuffers()
        {
            lock (bufferLock)
            {
                var temp = write;
                write = read;
                read = temp;

                write.Clear();
            }
        }
    }
}
