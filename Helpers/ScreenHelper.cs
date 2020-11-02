using System;
using System.Drawing;
using System.Runtime.InteropServices;

namespace AimBot.Helpers
{
    public static class ScreenHelper
    {
        #region P/Invoke Signatures
        [StructLayout(LayoutKind.Sequential)]
        private struct RECT
        {
            public int Left;
            public int Top;
            public int Right;
            public int Bottom;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct POINT
        {
            public int X;
            public int Y;

            public POINT(int x, int y)
            {
                this.X = x;
                this.Y = y;
            }
        }

        [DllImport("user32.dll")]
        private static extern bool GetWindowRect(IntPtr hWnd, out RECT lpRect);

        [DllImport("user32.dll")]
        private static extern bool GetClientRect(IntPtr hWnd, out RECT lpRect);

        [DllImport("user32.dll")]
        private static extern bool ClientToScreen(IntPtr hWnd, ref POINT lpPoint);
        #endregion

        /// <summary>
        /// Determines the rectangle that bounds the specified window, including the title bar, any
        /// borders, scroll bars, etc.
        /// </summary>
        /// <remarks>
        /// The rectangle returned is in screen coordinates.
        /// </remarks>
        /// <param name="windowHandle">The handle to the window.</param>
        /// <returns>The bounding rectangle (in screen coordinates).</returns>
        public static Rectangle WindowRectangle(IntPtr windowHandle)
        {
            GetWindowRect(windowHandle, out var rect);
            return new Rectangle(rect.Left, rect.Top, rect.Right - rect.Left, rect.Bottom - rect.Top);
        }

        /// <summary>
        /// Determines the the width and height of the client area of the specified window. This
        /// excludes things like the title bar, any borders, scroll bars, etc.
        /// </summary>
        /// <param name="windowHandle">The handle to the window.</param>
        /// <param name="width">The client area width.</param>
        /// <param name="height">The client area height.</param>
        public static void ClientDimensions(IntPtr windowHandle, out int width, out int height)
        {
            GetClientRect(windowHandle, out var rect);
            width = rect.Right - rect.Left;
            height = rect.Bottom - rect.Top;
        }

        /// <summary>
        /// Converts the specified screen-space point to window-space.
        /// </summary>
        /// <param name="windowHandle">The handle to the window.</param>
        /// <param name="screenPoint">The screen-space point.</param>
        /// <returns>The window-space point.</returns>
        public static Point ScreenToWindowCoordinates(IntPtr windowHandle, Point screenPoint)
        {
            var rect = WindowRectangle(windowHandle);
            return new Point(screenPoint.X - rect.X, screenPoint.Y - rect.Y);
        }

        /// <summary>
        /// Converts the specified client-space point to screen-space.
        /// </summary>
        /// <param name="windowHandle">The handle to the window.</param>
        /// <param name="clientPoint">The client-space point.</param>
        /// <returns>The screen-space point.</returns>
        public static Point ClientToScreenCoordinates(IntPtr windowHandle, Point clientPoint)
        {
            var point = new POINT(clientPoint.X, clientPoint.Y);
            ClientToScreen(windowHandle, ref point);
            return new Point(point.X, point.Y);
        }

        /// <summary>
        /// Converts the specified client-space point to window-space.
        /// </summary>
        /// <param name="windowHandle">The handle to the window.</param>
        /// <param name="clientPoint">The client-space point.</param>
        /// <returns>The window-space point.</returns>
        public static Point ClientToWindowCoordinates(IntPtr windowHandle, Point clientPoint)
        {
            return ScreenToWindowCoordinates(windowHandle, ClientToScreenCoordinates(windowHandle, clientPoint));
        }

        /// <summary>
        /// Determines the rectangle that bounds the client area of the specified window. This
        /// excludes things like the title bar, any borders, scroll bars, etc.
        /// </summary>
        /// <remarks>
        /// The rectangle returned is in screen coordinates.
        /// </remarks>
        /// <param name="windowHandle">The handle to the window.</param>
        /// <returns>The bounding rectangle for the client area (in screen coordinates).</returns>
        public static Rectangle ClientRectangle(IntPtr windowHandle)
        {
            GetClientRect(windowHandle, out var rect);
            var topLeft = new POINT(rect.Left, rect.Top);
            var bottomRight = new POINT(rect.Right, rect.Bottom);
            ClientToScreen(windowHandle, ref topLeft);
            ClientToScreen(windowHandle, ref bottomRight);
            return new Rectangle(topLeft.X, topLeft.Y, bottomRight.X - topLeft.X, bottomRight.Y - topLeft.Y);
        }
    }
}
