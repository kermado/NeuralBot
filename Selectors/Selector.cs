using System;
using System.Drawing;

using AimBot.Renderers;
using AimBot.Trackers;

namespace AimBot.Selectors
{
    public interface Selector : IDisposable
    {
        /// <summary>
        /// Selects and returns a point at which to aim.
        /// </summary>
        /// <param name="tracker">The tracker, which tracks detections.</param>
        /// <param name="position">The current mouse position.</param>
        /// <param name="esp">The esp, which can be rendered to.</param>
        /// <param name="aim">Whther to select a target.</param>
        /// <param name="dt">The timestep.</param>
        /// <returns>The selected point.</returns>
        Point Select(Tracker tracker, Point position, Esp esp, bool aim, double dt);

        void Clear();
    }
}
