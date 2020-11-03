using System;
using System.Drawing;
using System.Runtime.InteropServices;

using AimBot.Renderers;

namespace AimBot.Grabbers
{
    /// <summary>
    /// DirectX screen grabber.
    /// </summary>
    /// <remarks>
    /// Uses the desktop duplication API.
    /// </remarks>
    public class DXGrabber : Grabber
    {
        #region P/Invoke Signatures
        private const string LibraryName = "Grabbers/DxGrabber.dll";

        [DllImport(LibraryName, EntryPoint = "create")]
        private static extern IntPtr Create();

        [DllImport(LibraryName, EntryPoint = "release")]
        private static extern void Release(IntPtr native);

        [DllImport(LibraryName, EntryPoint = "grab")]
        private static extern IntPtr Capture(IntPtr native, int x, int y, int width, int height, int format, int timeout, bool wait, ref int frames);
        #endregion

        private IntPtr native;
        private bool disposed;

        public DXGrabber()
        {
            native = Create();
            disposed = false;
        }

        public IntPtr Grab(IntPtr windowHandle, Rectangle region, Esp esp, bool wait, out bool changed)
        {
            esp.Add(new RectangleShape(region, Color.Transparent, Color.LimeGreen, 1));

            if (native != IntPtr.Zero)
            {
                int frames = 0;
                var data = Capture(native, region.X, region.Y, region.Width, region.Height, 1, 1000, wait, ref frames);
                changed = frames > 0;

                return data;
            }

            changed = false;
            return IntPtr.Zero;
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposed == false)
            {
                if (disposing)
                {
                    // TODO: dispose managed state (managed objects).
                }

                if (native != IntPtr.Zero)
                {
                    Release(native);
                }

                disposed = true;
            }
        }

        ~DXGrabber()
        {
            Dispose(false);
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
    }
}
