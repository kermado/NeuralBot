using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Runtime.InteropServices;

using AimBot.Renderers;

namespace AimBot.Detectors
{
    public class OpenComputerVisionDetector : NeuralNetDetector, Detector
    {
        private class OpenCV : IDisposable
        {
            #region P/Invoke Signatures
            private const string LibraryName = "Detectors/OpenCV.dll";

            [DllImport(LibraryName, EntryPoint = "create")]
            private static extern IntPtr Create(string configurationPath, string weightsPath);

            [DllImport(LibraryName, EntryPoint = "detect")]
            private static extern int Detect(IntPtr instance, IntPtr data, int width, int height, IntPtr boxes);

            [DllImport(LibraryName, EntryPoint = "release")]
            private static extern int Release(IntPtr instance);

            [StructLayout(LayoutKind.Sequential)]
            public struct BBox
            {
                public int classId;
                public int x;
                public int y;
                public int width;
                public int height;
                public float confidence;
            };
            #endregion

            private BBox[] boxes;
            private IntPtr instance;
            private bool disposed;

            public OpenCV(string configurationPath, string weightsPath)
            {
                boxes = new BBox[1000];
                instance = Create(configurationPath, weightsPath);
            }

            public BBox[] Detect(IntPtr bytes, int width, int height, out int count)
            {
                var handle = GCHandle.Alloc(boxes, GCHandleType.Pinned);

                try
                {
                    count = Detect(instance, bytes, width, height, handle.AddrOfPinnedObject());

                    if (count <= 0)
                    {
                        return null;
                    }

                    return boxes;
                }
                finally
                {
                    handle.Free();
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

                    Release(instance);
                    disposed = true;
                }
            }

            ~OpenCV()
            {
                Dispose(false);
            }

            public void Dispose()
            {
                Dispose(true);
                GC.SuppressFinalize(this);
            }
        }

        private bool disposed;
        private OpenCV opencv;
        private readonly Resizer resizer;

        public OpenComputerVisionDetector()
        {
            disposed = false;
            opencv = null;
            resizer = new Resizer();
        }

        protected override bool Reload()
        {
            if (opencv != null)
            {
                opencv.Dispose();
            }

            if (File.Exists(ConfigurationFile.FilePath))
            {
                var weightsPath = Path.ChangeExtension(ConfigurationFile.FilePath, ".weights");
                if (File.Exists(weightsPath))
                {
                    opencv = new OpenCV(ConfigurationFile.FilePath, weightsPath);
                }
            }

            return false;
        }

        protected override void AddDetections(IntPtr image, Rectangle region, Esp esp, List<Detection> detections)
        {
            if (image != IntPtr.Zero)
            {
                var resized = resizer.Resize(image, region.Width, region.Height, InputWidth, InputHeight);
                if (resized != IntPtr.Zero)
                {
                    var ratiox = (double)region.Width / 512.0;
                    var ratioy = (double)region.Height / 512.0;

                    var results = opencv.Detect(image, region.Width, region.Height, out var count);
                    if (results != null && results.Length > 0)
                    {
                        for (int i = 0; i < count; ++i)
                        {
                            var result = results[i];
                            if (result.confidence >= DetectionThreshold)
                            {
                                var x = (int)(result.x * ratiox);
                                var y = (int)(result.y * ratioy);
                                var w = (int)(result.width * ratiox);
                                var h = (int)(result.height * ratioy);

                                var bounds = new Rectangle(region.X + x, region.Y + y, w, h);
                                AddDetection(bounds, result.confidence, esp);
                            }
                        }
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
                    resizer?.Dispose();
                    opencv?.Dispose();
                }

                opencv = null;
                disposed = true;
            }
        }

        public void Dispose()
        {
            Dispose(true);
        }
    }
}
