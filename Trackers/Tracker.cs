using System;
using System.Collections.Generic;

using AimBot.Detectors;
using AimBot.Renderers;

namespace AimBot.Trackers
{
    public readonly struct TrackedObject
    {
        /// <summary>
        /// The detection.
        /// </summary>
        public readonly Detection Detection;

        /// <summary>
        /// The unique tracking identifier.
        /// </summary>
        public readonly uint Id;

        /// <summary>
        /// The time that the object has been in the active or inactive list. 
        /// </summary>
        public readonly double Time;

        public TrackedObject(Detection detection, uint id, double time)
        {
            Detection = detection;
            Id = id;
            Time = time;
        }
    }

    public interface Tracker : IDisposable
    {
        /// <summary>
        /// The list of active objects.
        /// </summary>
        /// <remarks>
        /// These are objects that are currently detected.
        /// </remarks>
        IEnumerable<TrackedObject> Active { get; }

        /// <summary>
        /// The list of inactive objects.
        /// </summary>
        /// <remarks>
        /// These are objects that are not currently detected, but were recently detected.
        /// </remarks>
        IEnumerable<TrackedObject> Inactive { get; }

        void Track(List<Detection> detections, Esp esp, double dt);

        void Clear();
    }
}
