using System;
using System.Drawing;

using AimBot.Renderers;

namespace AimBot.Grabbers
{
    public interface Grabber : IDisposable
    {
        /// <summary>
        /// Grabs the specified region of the window as an image.
        /// </summary>
        /// <param name="windowHandle">The window handle.</param>
        /// <param name="region">The region.</param>
        /// <param name="esp">The esp, which can be rendered to.</param>
        /// <param name="wait">Whether to block until an image is available.</param>
        /// <param name="changed">Whether the image may have changed.</param>
        /// <returns>Pointer to the unmanaged byte array for the image.</returns>
        IntPtr Grab(IntPtr windowHandle, Rectangle region, Esp esp, bool wait, out bool changed);
    }
}
