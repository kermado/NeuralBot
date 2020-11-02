using System;
using System.Collections.Generic;
using System.Drawing;

using Newtonsoft.Json;

using AimBot.Helpers;
using AimBot.Detectors;
using AimBot.Renderers;

namespace AimBot.Trackers
{
    public class HungarianTracker : Tracker
    {
        private readonly List<TrackedObject> trackedObjects;
        private readonly List<TrackedObject> droppedObjects;
        private readonly List<uint> dropIds;
        private uint nextId;
        private bool disposed;

        /// <summary>
        /// The length of time after which objects are removed from the inactive list.
        /// </summary>
        public double MaximumInactiveTime { get; set; } = 0.25;

        /// <summary>
        /// The maximum distance that a tracked object can move between frames before it is
        /// classified as a new object.
        /// </summary>
        public int MaximumTrackingDistance { get; set; } = 256;

        [JsonIgnore]
        public IEnumerable<TrackedObject> Active
        {
            get { return trackedObjects; }
        }

        [JsonIgnore]
        public IEnumerable<TrackedObject> Inactive
        {
            get { return droppedObjects; }
        }

        public HungarianTracker()
        {
            trackedObjects = new List<TrackedObject>(12);
            droppedObjects = new List<TrackedObject>(12);
            dropIds = new List<uint>(12);
            nextId = 1;
        }

        public void Clear()
        {
            trackedObjects.Clear();
            droppedObjects.Clear();
            dropIds.Clear();
            nextId = 1;
        }

        public void Track(List<Detection> detections, Esp esp, double dt)
        {
            // Purge dropped objects that have exceeded the time limit.
            for (int i = droppedObjects.Count - 1; i >= 0; --i)
            {
                var dropped = droppedObjects[i];
                if (dropped.Time + dt > MaximumInactiveTime)
                {
                    droppedObjects.RemoveFast(i);
                }
                else
                {
                    droppedObjects[i] = new TrackedObject(dropped.Detection, dropped.Id, dropped.Time + dt);
                }
            }

            // Build the cost matrix.
            var rows = trackedObjects.Count + droppedObjects.Count;
            var cols = detections.Count;
            var dimensions = Math.Max(rows, cols);
            var costs = new double[dimensions, dimensions];

            // Populate the cost matrix.
            // Detections are the rows of the matrix.
            // Tracked and droped objects are the columns.
            // Entries are the costs for matching each.
            // Note that if the number of detections is not equal to the number of tracked/dropped
            // objects then we will have a bunch of (dummy) zero entries. These correspond to
            // creating new tracked objects or dropping more tracked objects.
            for (int r = 0; r < detections.Count; ++r)
            {
                var detection = detections[r];

                for (int c = 0; c < trackedObjects.Count; ++c)
                {
                    costs[r, c] = GIOU(detection.BoundingBox, trackedObjects[c].Detection.BoundingBox);
                }

                for (int c = 0; c < droppedObjects.Count; ++c)
                {
                    costs[r, trackedObjects.Count + c] = GIOU(detection.BoundingBox, droppedObjects[c].Detection.BoundingBox);
                }
            }

            // Find matches using the Hungarian Algorithm.
            var assignments = HungarianAlgorithm.FindAssignments(costs);

            // Update or create the tracked/dropped objects.
            var persistedCount = trackedObjects.Count;
            var maximumDistanceSq = MaximumTrackingDistance * MaximumTrackingDistance;
            for (int r = 0; r < detections.Count; ++r)
            {
                var detection = detections[r];
                var detectionCenter = detection.BoundingBox.Center();
                var c = assignments[r];
                if (c < trackedObjects.Count)
                {
                    // We have a match with a tracked object.
                    // Check that the distance doesn't exceed the maximum allowed.
                    var tracked = trackedObjects[c];
                    var distanceSq = detectionCenter.DistanceSq(tracked.Detection.BoundingBox.Center());
                    if (distanceSq <= maximumDistanceSq)
                    {
                        // Okay, we can update the tracked object.
                        trackedObjects[c] = new TrackedObject(detection, trackedObjects[c].Id, trackedObjects[c].Time + dt);
                    }
                    else
                    {
                        // Distance limit exceeded.
                        // We need to create a new tracking object and drop the old one.
                        trackedObjects.RemoveFast(c);
                        trackedObjects.Add(new TrackedObject(detection, nextId++, 0.0));
                        droppedObjects.Add(new TrackedObject(tracked.Detection, tracked.Id, 0.0));
                        persistedCount -= 1;
                    }
                }
                else if (c < rows)
                {
                    // We have a match with a dropped object.
                    // Check the distance doesn't exceed the maximum allowed.
                    var idx = rows - trackedObjects.Count - 1;
                    var dropped = droppedObjects[idx];
                    var distanceSq = detectionCenter.DistanceSq(dropped.Detection.BoundingBox.Center());
                    if (distanceSq <= maximumDistanceSq)
                    {
                        // Okay, we can bring back the dropped object.
                        trackedObjects.Add(new TrackedObject(detection, dropped.Id, 0.0));
                        droppedObjects.RemoveFast(idx);
                    }
                    else
                    {
                        // Distance limit exceeded.
                        // We need to add a new tracking object.
                        trackedObjects.Add(new TrackedObject(detection, nextId++, 0.0));
                    }
                }
                else
                {
                    // Completely new object.
                    trackedObjects.Add(new TrackedObject(detection, nextId++, 0.0));
                }
            }

            // Collect the list of tracked object IDs to be removed from the tracked object list and
            // drop the trapped objects.
            dropIds.Clear();
            for (int r = detections.Count; r < assignments.Length; ++r)
            {
                var c = assignments[r];
                if (c < persistedCount) // We could have added/removed tracked objects above.
                {
                    var tracked = trackedObjects[c];
                    dropIds.Add(tracked.Id);
                    droppedObjects.Add(new TrackedObject(tracked.Detection, tracked.Id, 0.0));
                }
            }

            // Now remove the dropped tracked objects.
            foreach (var id in dropIds)
            {
                for (int i = 0; i < persistedCount; ++i)
                {
                    if (trackedObjects[i].Id == id)
                    {
                        trackedObjects.RemoveFast(i);
                        persistedCount -= 1;
                        break;
                    }
                }
            }

            // Render tracked objects.
            if (esp != null)
            {
                foreach (var tracked in trackedObjects)
                {
                    esp.Add(new TextShape(tracked.Detection.BoundingBox.CenterRight(), tracked.Id.ToString(), Color.Red, 12));
                }
            }
        }

        /// <summary>
        /// Calculates the Generalised Intersection over Union.
        /// </summary>
        /// <remarks>
        /// This metric is used for determining how closely matched two AABBs are. A low value
        /// indicates that the two rectanges are well matched. Note that this is better than the
        /// IoU metric, since it works with non-intersecting rectangles.
        /// See the following for a detailed description: https://giou.stanford.edu/
        /// </remarks>
        /// <param name="r1">The first rectangle.</param>
        /// <param name="r2">The second rectangle.</param>
        /// <returns>The Generalised Intersection over Union (GIOU).</returns>
        private static double GIOU(Rectangle r1, Rectangle r2)
        {
            // Calculate areas of rectangles.
            var a1 = r1.Width * r1.Height;
            var a2 = r2.Width * r2.Height;

            // Calculate area of intersection.
            var xi1 = Math.Max(r1.Left, r2.Left);
            var xi2 = Math.Min(r1.Right, r2.Right);
            var yi1 = Math.Max(r1.Top, r2.Top);
            var yi2 = Math.Min(r1.Bottom, r2.Bottom);
            var ai = 0.0;
            if (xi2 > xi1 && yi2 > yi1)
            {
                // Intersecting!
                ai = (xi2 - xi1) * (yi2 - yi1);
            }

            // Calculate the area of smallest enclosing box.
            var xc1 = Math.Min(r1.Left, r2.Left);
            var xc2 = Math.Max(r1.Right, r2.Right);
            var yc1 = Math.Min(r1.Top, r2.Top);
            var yc2 = Math.Max(r1.Bottom, r2.Bottom);
            var ac = (xc2 - xc1) * (yc2 - yc1);

            // Calculate the IoU and GIoU.
            var au = a1 + a2 - ai;
            var iou = ai / au;
            var giou = iou - ((ac - au) / ac);

            // Return GIoU loss.
            return 1.0 - giou;
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

        // Copyright (c) 2010 Alex Regueiro
        // Licensed under MIT license, available at <http://www.opensource.org/licenses/mit-license.php>.
        // Published originally at <http://blog.noldorin.com/2009/09/hungarian-algorithm-in-csharp/>.
        // Based on implementation described at <http://www.public.iastate.edu/~ddoty/HungarianAlgorithm.html>.
        // Version 1.3, released 22nd May 2010.
        //
        // Note [Omar Kermad]: This looks to be performing a lot of allocations. We should really
        // try to allocate once and reuse where possible.
        private static class HungarianAlgorithm
        {
            public static int[] FindAssignments(double[,] costs)
            {
                var h = costs.GetLength(0);
                var w = costs.GetLength(1);

                for (int i = 0; i < h; i++)
                {
                    var min = double.MaxValue;

                    for (int j = 0; j < w; j++)
                    {
                        min = Math.Min(min, costs[i, j]);
                    }

                    for (int j = 0; j < w; j++)
                    {
                        costs[i, j] -= min;
                    }
                }

                var masks = new byte[h, w];
                var rowsCovered = new bool[h];
                var colsCovered = new bool[w];

                for (int i = 0; i < h; i++)
                {
                    for (int j = 0; j < w; j++)
                    {
                        if (costs[i, j] == 0 && !rowsCovered[i] && !colsCovered[j])
                        {
                            masks[i, j] = 1;
                            rowsCovered[i] = true;
                            colsCovered[j] = true;
                        }
                    }
                }

                ClearCovers(rowsCovered, colsCovered, w, h);

                var path = new Location[w * h];
                var pathStart = default(Location);
                var step = 1;
                while (step != -1)
                {
                    switch (step)
                    {
                        case 1:
                            step = RunStep1(masks, colsCovered, w, h);
                            break;
                        case 2:
                            step = RunStep2(costs, masks, rowsCovered, colsCovered, w, h, ref pathStart);
                            break;
                        case 3:
                            step = RunStep3(masks, rowsCovered, colsCovered, w, h, path, pathStart);
                            break;
                        case 4:
                            step = RunStep4(costs, masks, rowsCovered, colsCovered, w, h);
                            break;
                    }
                }

                var agentsTasks = new int[h];
                for (int i = 0; i < h; i++)
                {
                    for (int j = 0; j < w; j++)
                    {
                        if (masks[i, j] == 1)
                        {
                            agentsTasks[i] = j;
                            break;
                        }
                    }
                }

                return agentsTasks;
            }

            private static int RunStep1(byte[,] masks, bool[] colsCovered, int w, int h)
            {
                for (int i = 0; i < h; i++)
                {
                    for (int j = 0; j < w; j++)
                    {
                        if (masks[i, j] == 1)
                        {
                            colsCovered[j] = true;
                        }
                    }
                }

                var colsCoveredCount = 0;
                for (int j = 0; j < w; j++)
                {
                    if (colsCovered[j])
                    {
                        colsCoveredCount++;
                    }
                }

                if (colsCoveredCount == h)
                {
                    return -1;
                }
                else
                {
                    return 2;
                }
            }

            private static int RunStep2(double[,] costs, byte[,] masks, bool[] rowsCovered, bool[] colsCovered, int w, int h, ref Location pathStart)
            {
                Location loc;
                while (true)
                {
                    loc = FindZero(costs, rowsCovered, colsCovered, w, h);
                    if (loc.Row == -1)
                    {
                        return 4;
                    }
                    else
                    {
                        masks[loc.Row, loc.Column] = 2;
                        var starCol = FindStarInRow(masks, w, loc.Row);

                        if (starCol != -1)
                        {
                            rowsCovered[loc.Row] = true;
                            colsCovered[starCol] = false;
                        }
                        else
                        {
                            pathStart = loc;
                            return 3;
                        }
                    }
                }
            }

            private static int RunStep3(byte[,] masks, bool[] rowsCovered, bool[] colsCovered, int w, int h, Location[] path, Location pathStart)
            {
                var pathIndex = 0;
                path[0] = pathStart;

                while (true)
                {
                    var row = FindStarInColumn(masks, h, path[pathIndex].Column);
                    if (row == -1)
                    {
                        break;
                    }

                    pathIndex++;
                    path[pathIndex] = new Location(row, path[pathIndex - 1].Column);
                    var col = FindPrimeInRow(masks, w, path[pathIndex].Row);
                    pathIndex++;
                    path[pathIndex] = new Location(path[pathIndex - 1].Row, col);
                }

                ConvertPath(masks, path, pathIndex + 1);
                ClearCovers(rowsCovered, colsCovered, w, h);
                ClearPrimes(masks, w, h);

                return 1;
            }

            private static int RunStep4(double[,] costs, byte[,] masks, bool[] rowsCovered, bool[] colsCovered, int w, int h)
            {
                var minValue = FindMinimum(costs, rowsCovered, colsCovered, w, h);
                for (int i = 0; i < h; i++)
                {
                    for (int j = 0; j < w; j++)
                    {
                        if (rowsCovered[i])
                        {
                            costs[i, j] += minValue;
                        }

                        if (!colsCovered[j])
                        {
                            costs[i, j] -= minValue;
                        }
                    }
                }

                return 2;
            }

            private static void ConvertPath(byte[,] masks, Location[] path, int pathLength)
            {
                for (int i = 0; i < pathLength; i++)
                {
                    if (masks[path[i].Row, path[i].Column] == 1)
                    {
                        masks[path[i].Row, path[i].Column] = 0;
                    }
                    else if (masks[path[i].Row, path[i].Column] == 2)
                    {
                        masks[path[i].Row, path[i].Column] = 1;
                    }
                }
            }

            private static Location FindZero(double[,] costs, bool[] rowsCovered, bool[] colsCovered, int w, int h)
            {
                for (int i = 0; i < h; i++)
                {
                    for (int j = 0; j < w; j++)
                    {
                        if (costs[i, j] == 0.0 && !rowsCovered[i] && !colsCovered[j])
                        {
                            return new Location(i, j);
                        }
                    }
                }

                return new Location(-1, -1);
            }

            private static double FindMinimum(double[,] costs, bool[] rowsCovered, bool[] colsCovered, int w, int h)
            {
                var minValue = double.MaxValue;
                for (int i = 0; i < h; i++)
                {
                    for (int j = 0; j < w; j++)
                    {
                        if (!rowsCovered[i] && !colsCovered[j])
                        {
                            minValue = Math.Min(minValue, costs[i, j]);
                        }
                    }
                }

                return minValue;
            }

            private static int FindStarInRow(byte[,] masks, int w, int row)
            {
                for (int j = 0; j < w; j++)
                {
                    if (masks[row, j] == 1)
                    {
                        return j;
                    }
                }

                return -1;
            }

            private static int FindStarInColumn(byte[,] masks, int h, int col)
            {
                for (int i = 0; i < h; i++)
                {
                    if (masks[i, col] == 1)
                    {
                        return i;
                    }
                }

                return -1;
            }

            private static int FindPrimeInRow(byte[,] masks, int w, int row)
            {
                for (int j = 0; j < w; j++)
                {
                    if (masks[row, j] == 2)
                    {
                        return j;
                    }
                }

                return -1;
            }

            private static void ClearCovers(bool[] rowsCovered, bool[] colsCovered, int w, int h)
            {
                for (int i = 0; i < h; i++)
                {
                    rowsCovered[i] = false;
                }

                for (int j = 0; j < w; j++)
                {
                    colsCovered[j] = false;
                }
            }

            private static void ClearPrimes(byte[,] masks, int w, int h)
            {
                for (int i = 0; i < h; i++)
                {
                    for (int j = 0; j < w; j++)
                    {
                        if (masks[i, j] == 2)
                        {
                            masks[i, j] = 0;
                        }
                    }
                }
            }

            private struct Location
            {
                public readonly int Row;
                public readonly int Column;

                public Location(int row, int col)
                {
                    this.Row = row;
                    this.Column = col;
                }
            }
        }
    }
}
