using System;
using System.Drawing;

using AimBot.Helpers;
using AimBot.Detectors;
using AimBot.Trackers;
using AimBot.Renderers;

namespace AimBot.Selectors
{
    /// <summary>
    /// Selects the point nearest to the current mouse position.
    /// </summary>
    public class NearestSelector : Selector
    {
        /// <summary>
        /// The ID for the object being targeted.
        /// </summary>
        /// <remarks>
        /// Zero if no object currently being targeted.
        /// </remarks>
        private uint targetId;

        /// <summary>
        /// The time spent tracking the current target.
        /// </summary>
        private double time;

        /// <summary>
        /// Whether the target was lost.
        /// </summary>
        private bool lost;

        /// <summary>
        /// Whether we have been disposed.
        /// </summary>
        private bool disposed;

        /// <summary>
        /// Which part (head/body) should be targeted.
        /// </summary>
        public Selecting Selecting { get; set; }

        /// <summary>
        /// The maximum length of time that can be spent selecting the same active target.
        /// </summary>
        public double MaximumActiveSelectionTime { get; set; } = 10.0;

        /// <summary>
        /// The minimum length of time before another target can be selected whilst aiming.
        /// </summary>
        /// <remarks>
        /// Aiming will be cancelled where the aim key/button is pressed continuously for this
        /// length of time.
        /// </remarks>
        public double MinimumReselectionTime { get; set; } = 20.0;

        /// <summary>
        /// The minimum detection confidence for target selection.
        /// </summary>
        public double MinimumSelectionConfidence { get; set; } = 0.1;

        public void Clear()
        {
            targetId = 0;
            time = MinimumReselectionTime;
            lost = false;
        }

        /// <summary>
        /// Selects and returns a target point.
        /// </summary>
        /// <param name="tracker">The tracker, which tracks detections.</param>
        /// <param name="position">The current mouse position.</param>
        /// <param name="esp">The esp, which can be rendered to.</param>
        /// <param name="aim">Whether to select a target.</param>
        /// <param name="dt">The timestep.</param>
        /// <returns>The selected point.</returns>
        public Point Select(Tracker tracker, Point position, Esp esp, bool aim, double dt)
        {
            // If we don't want to select a target (e.g. the aim key is not depressed), then cancel
            // any existing tracking.
            if (aim == false)
            {
                Clear();
                return Point.Empty;
            }

            time += dt;

            // Are we currently tracking an object?
            if (targetId > 0)
            {
                // Look in the list of active objects.
                foreach (var obj in tracker.Active)
                {
                    if (obj.Id == targetId)
                    {
                        if (time <= MaximumActiveSelectionTime)
                        {
                            var point = Select(obj, position);
                            Render(point, esp);
                            lost = false;
                            return point;
                        }
                        else
                        {
                            // Select a new target.
                            time = 0.0;
                            targetId = 0;
                            lost = false;
                            break;
                        }
                    }
                }

                // Couldn't find the object.
                // Reset the time so that we don't immediately reselect a new target.
                if (lost == false)
                {
                    time = 0.0;
                    lost = true;
                }
            }

            // Can we select a new target.
            if (time >= MinimumReselectionTime)
            {
                // Select a new target.
                // We select the closest active object.
                targetId = 0;
                lost = false;
                var distanceSq = double.MaxValue;
                var target = Point.Empty;
                foreach (var obj in tracker.Active)
                {
                    if (obj.Detection.Confidence >= MinimumSelectionConfidence)
                    {
                        if (Select(obj, position, ref distanceSq, ref target))
                        {
                            targetId = obj.Id;
                        }
                    }
                }

                // Did we select a new target?
                if (targetId > 0)
                {
                    // Reset the timer.
                    time = 0.0;

                    // Render.
                    Render(target, esp);

                    // Return the selected target.
                    return target;
                }
            }

            // We must wait before selecting another target.
            return Point.Empty;
        }

        private void Render(Point target, Esp esp)
        {
            if (esp != null && targetId > 0)
            {
                var width = 8;
                var height = 8;
                var rect = new Rectangle(target.X - width / 2, target.Y - height / 2, width, height);
                esp.Add(new RectangleShape(rect, Color.Red, Color.Red, 4));
            }
        }

        private bool Select(TrackedObject obj, Point position, ref double minimumDistanceSq, ref Point target)
        {
            var selected = false;
            if ((Selecting & Selecting.Body) != 0)
            {
                var body = obj.Detection.BodyPosition;
                var distanceSq = position.DistanceSq(body);
                if (distanceSq < minimumDistanceSq)
                {
                    minimumDistanceSq = distanceSq;
                    target = body;
                    selected = true;
                }
            }

            if ((Selecting & Selecting.Head) != 0)
            {
                var head = obj.Detection.HeadPosition;
                var distanceSq = position.DistanceSq(head);
                if (distanceSq <= minimumDistanceSq)
                {
                    minimumDistanceSq = distanceSq;
                    target = head;
                    selected = true;
                }
            }

            return selected;
        }

        private Point Select(TrackedObject obj, Point position)
        {
            var distanceSq = double.MaxValue;
            var target = Point.Empty;
            Select(obj, position, ref distanceSq, ref target);
            return target;
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposed == false)
            {
                if (disposing)
                {
                    // TODO: dispose managed state (managed objects).
                }

                disposed = true;
            }
        }

        public void Dispose()
        {
            Dispose(true);
        }
    }
}
