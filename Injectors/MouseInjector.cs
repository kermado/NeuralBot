using System;

using Newtonsoft.Json;

namespace AimBot.Injectors
{
    public interface MouseInjector : IDisposable
    {
        /// <summary>
        /// The ID for the process into which the mouse events are to be injected.
        /// </summary>
        [JsonIgnore]
        public UInt64 ProcessId { get; set; }

        /// <summary>
        /// Presses and releases the specified button.
        /// </summary>
        /// <param name="button">The button (0 = left, 1 = right).</param>
        /// <param name="releaseMilliseconds">The delay after which the button is released (in milliseconds).</param>
        void Click(int button, int releaseMilliseconds);

        /// <summary>
        /// Moves the mouse cursor.
        /// </summary>
        /// <remarks>
        /// <param name="x">The x-coordinate (in screen-space).</param>
        /// <param name="y">The y-coordinate (in screen-space).</param>
        void Move(int x, int y);
    }
}
