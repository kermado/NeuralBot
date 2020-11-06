using System;
using System.Runtime.InteropServices;

namespace AimBot.Detectors
{
    public class Resizer : IDisposable
    {
        #region P/Invoke Signatures
        [DllImport("msvcrt.dll", EntryPoint = "memcpy", CallingConvention = CallingConvention.Cdecl, SetLastError = false)]
        public static extern IntPtr MemCpy(IntPtr dest, IntPtr src, UIntPtr count);
        #endregion

        private int width;
        private int height;
        private IntPtr resized;
        private bool disposed;

        public Resizer()
        {
            width = 0;
            height = 0;
            resized = IntPtr.Zero;
            disposed = false;
        }

        public IntPtr Resize(IntPtr source, int sourceWidth, int sourceHeight, int resizedWidth, int resizedHeight)
        {
            // Check resizing necessary.
            if (sourceWidth == resizedWidth && sourceHeight == resizedHeight)
            {
                return source;
            }

            // Check need to reallocate memory.
            if (width != resizedWidth || height != resizedHeight)
            {
                if (resized != IntPtr.Zero)
                {
                    Marshal.FreeHGlobal(resized);
                    resized = IntPtr.Zero;
                }

                resized = Marshal.AllocHGlobal(resizedWidth * resizedHeight * 3 * sizeof(float));
                width = resizedWidth;
                height = resizedHeight;
            }

            for (int c = 0; c < 3; ++c)
            {
                var resizedPage = resized + (width * height * c * sizeof(float));
                var sourcePage = source + (sourceWidth * sourceHeight * c * sizeof(float));
                NearestNeighbour(resizedPage, sourcePage, width, height, sourceWidth, sourceHeight);
            }

            return resized;
        }

        // See: http://tech-algorithm.com/articles/nearest-neighbor-image-scaling/
        private static void NearestNeighbour(IntPtr resized, IntPtr source, int resizedWidth, int resizedHeight, int sourceWidth, int sourceHeight)
        {
            double rx = (double)sourceWidth / resizedWidth;
            double ry = (double)sourceHeight / resizedHeight;

            unsafe
            {
                float* rsz = (float*)resized.ToPointer();
                float* src = (float*)source.ToPointer();

                double px, py;
                double srs;
                int rrs;
                for (int y = 0; y < resizedHeight; ++y)
                {
                    py = Math.Floor(y * ry);
                    rrs = y * resizedWidth;
                    srs = py * sourceWidth;
                    for (int x = 0; x < resizedWidth; ++x)
                    {
                        px = Math.Floor(x * rx);
                        rsz[rrs + x] = src[(int)(srs + px)];
                    }
                }
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

                if (resized != IntPtr.Zero)
                {
                    Marshal.FreeHGlobal(resized);
                    resized = IntPtr.Zero;
                }

                disposed = true;
            }
        }

        ~Resizer()
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
