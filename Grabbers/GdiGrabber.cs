using System;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;

using AimBot.Renderers;

namespace AimBot.Grabbers
{
    public class GdiGrabber : Grabber
    {
        #region P/Invoke Signatures
        [DllImport("user32.dll")]
        private static extern IntPtr GetDC(IntPtr hWnd);

        [DllImport("gdi32.dll")]
        private static extern bool DeleteDC(IntPtr hdc);

        [DllImport("user32.dll")]
        private static extern bool ReleaseDC(IntPtr hWnd, IntPtr hDC);

        [DllImport("gdi32.dll")]
        private static extern IntPtr CreateCompatibleDC(IntPtr hdc);

        [DllImport("gdi32.dll")]
        private static extern IntPtr CreateCompatibleBitmap(IntPtr hdc, int width, int height);

        [DllImport("gdi32.dll")]
        private static extern IntPtr SelectObject(IntPtr hdc, IntPtr hgdiobj);

        [DllImport("gdi32.dll")]
        private static extern bool DeleteObject(IntPtr hObject);

        [DllImport("gdi32.dll", SetLastError = true)]
        private static extern int BitBlt(IntPtr hdcDest, int nXDest, int nYDest, int nWidth, int nHeight, IntPtr hdcSrc, int nXSrc, int nYSrc, int dwRop);

        [DllImport("gdi32.dll", SetLastError = true)]
        private static extern int GetDIBits(IntPtr hdc, IntPtr hbmp, uint uStartScan, uint cScanLines, [Out] byte[] lpvBits, ref BITMAPINFOHEADER lpbi, uint uUsage);

        [DllImport("msvcrt.dll", EntryPoint = "memcmp")]
        private static extern int CompareMemory(IntPtr b1, IntPtr b2, long count);

        [StructLayout(LayoutKind.Sequential)]
        private struct BITMAPINFOHEADER
        {
            public uint biSize;
            public int biWidth;
            public int biHeight;
            public ushort biPlanes;
            public ushort biBitCount;
            public uint biCompression;
            public uint biSizeImage;
            public int biXPelsPerMeter;
            public int biYPelsPerMeter;
            public uint biClrUsed;
            public uint biClrImportant;
        }
        #endregion

        private IntPtr current;
        private IntPtr previous;
        private byte[] data;
        private int currentNumBytes;
        private int previousNumBytes;
        private int width;
        private int height;
        private bool disposed;

        public GdiGrabber()
        {
            current = IntPtr.Zero;
            previous = IntPtr.Zero;

            disposed = false;
        }

        public IntPtr Grab(IntPtr windowHandle, Rectangle region, Esp esp, bool wait, out bool changed)
        {
            changed = false;

            if (esp != null)
            {
                esp.Add(new RectangleShape(region, Color.Transparent, Color.LimeGreen, 1));
            }

            // Reuse existing bitmap if possible.
            // Better not to hammer the GC.
            width = region.Width;
            height = region.Height;
            var numPixels = width * height;
            var numBytes = numPixels * 12;
            if (previous == IntPtr.Zero || previousNumBytes != numBytes)
            {
                if (previous != IntPtr.Zero)
                {
                    Marshal.FreeHGlobal(previous);
                    previous = IntPtr.Zero;
                }

                previous = Marshal.AllocHGlobal(numBytes);
                previousNumBytes = numBytes;

                if (data == null || data.Length != numPixels * 3)
                {
                    data = new byte[numPixels * 3];
                }
            }

            var clientRect = Helpers.ScreenHelper.ClientRectangle(windowHandle);

            var hdc = GetDC(windowHandle); // Handle to display device context.
            var hdcMem = CreateCompatibleDC(hdc); // Create memory device context for the device.
            var hBitmap = CreateCompatibleBitmap(hdc, width, height); // Create a bitmap.

            do
            {
                var hOld = SelectObject(hdcMem, hBitmap); // Select the bitmap in the memory device context.

                // Copy from screen to memory device context.
                if (BitBlt(hdcMem, 0, 0, width, height, hdc, region.X - clientRect.X, region.Y - clientRect.Y, (int)CopyPixelOperation.SourceCopy) == 0)
                {
                    // Failed!
                    int error = Marshal.GetLastWin32Error(); // TODO: 
                }

                SelectObject(hdcMem, hOld); // Restore selection.

                var bitmapInfoHeader = new BITMAPINFOHEADER();
                bitmapInfoHeader.biSize = (uint)Marshal.SizeOf<BITMAPINFOHEADER>();
                bitmapInfoHeader.biWidth = width;
                bitmapInfoHeader.biHeight = -height; // Top down.
                bitmapInfoHeader.biPlanes = 1;
                bitmapInfoHeader.biBitCount = 24;
                bitmapInfoHeader.biCompression = 0; // BI_RGB = Uncompressed RGB
                bitmapInfoHeader.biSizeImage = 0; // Size of the image in bytes - set to zero for BI_RGB
                bitmapInfoHeader.biXPelsPerMeter = 0;
                bitmapInfoHeader.biYPelsPerMeter = 0;
                bitmapInfoHeader.biClrUsed = 0;
                bitmapInfoHeader.biClrImportant = 0;

                // Copy the bits of the RGB bitmap to the data array.
                var linesCopied = GetDIBits(hdc, hBitmap, 0, (uint)height, data, ref bitmapInfoHeader, 0);
                if (linesCopied != height || data == null)
                {
                    int error = Marshal.GetLastWin32Error();
                }

                // Copy to "previous" buffer.
                unsafe
                {
                    fixed (byte* source = data)
                    {
                        float* destination = (float*)previous;
                        for (int c = 0; c < 3; ++c)
                        {
                            float* page = destination + c * width * height;
                            for (int y = 0; y < height; ++y)
                            {
                                int offset = y * width;
                                float* scan0 = page + offset;
                                for (int x = 0; x < width; ++x)
                                {
                                    scan0[x] = (float)source[3 * (offset + x) + c] / 255.0F;
                                }
                            }
                        }
                    }
                }

                // Compare buffers.
                if (current == IntPtr.Zero) { changed = true; }
                else if (previousNumBytes != currentNumBytes) { changed = true; }
                else { changed = CompareMemory(previous, current, numBytes) != 0; }
            }
            while (wait == true && changed == false);

            // Free resources.
            DeleteObject(hBitmap);
            DeleteDC(hdcMem);
            ReleaseDC(IntPtr.Zero, hdc);

            // Swap buffers.
            var temp = current;
            current = previous;
            previous = temp;
            var temp2 = currentNumBytes;
            currentNumBytes = previousNumBytes;
            previousNumBytes = temp2;

            return current;
        }

        private void Save(string filepath)
        {
            if (data != null)
            {
                var bitmap = new Bitmap(width, height, PixelFormat.Format24bppRgb);
                var bitmapData = bitmap.LockBits(new Rectangle(0, 0, width, height), ImageLockMode.WriteOnly, bitmap.PixelFormat);

                Marshal.Copy(data, 0, bitmapData.Scan0, data.Length);

                bitmap.UnlockBits(bitmapData);

                bitmap.Save(filepath);
            }
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposed == false)
            {
                if (disposing)
                {
                    // TODO: dispose managed state (managed objects).
                }

                if (previous != IntPtr.Zero)
                {
                    Marshal.FreeHGlobal(previous);
                }

                if (current != IntPtr.Zero)
                {
                    Marshal.FreeHGlobal(current);
                }

                data = null;
                disposed = true;
            }
        }

        ~GdiGrabber()
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
