using System;
using System.Drawing;

using AimBot.Helpers;
using AimBot.Injectors;
using AimBot.Renderers;

namespace AimBot.Triggers
{
    public class ProximityTrigger : Trigger
    {
        [Flags]
        public enum Condition
        {
            Never = 0,
            WhenAiming = 1 << 0,
            WhenNotAiming = 1 << 1,
            Always = WhenAiming | WhenNotAiming
        }

        private double time;
        private bool disposed;

        public int Proximity { get; set; } = 20;
        public double TriggerRate { get; set; } = 0.8;
        public bool ContinueAiming { get; set; } = true;
        public Condition Conditions { get; set; } = Condition.Never;
        public int TriggerButton { get; set; } = 0;

        public ProximityTrigger()
        {
            Clear();
        }

        public void Clear()
        {
            time = 0.0;
        }

        public void Trigger(MouseInjector injector, Point current, Point target, Rectangle region, Esp esp, ref bool aim, double dt)
        {
            time += dt;

            if (Conditions != Condition.Never)
            {
                if (target != Point.Empty && esp != null)
                {
                    esp.Add(new CircleShape(new Point(target.X, target.Y), Proximity, Color.Transparent, Color.Cyan, 1));
                }

                bool trigger = (aim == true  && (Conditions & Condition.WhenAiming)    != 0) ||
                               (aim == false && (Conditions & Condition.WhenNotAiming) != 0);

                if (trigger && time >= TriggerRate)
                {
                    var distanceSq = current.DistanceSq(target);
                    if (distanceSq <= Proximity * Proximity)
                    {
                        injector.Click(TriggerButton, 50);
                        time = 0.0;
                        if (ContinueAiming == false)
                        {
                            aim = false;
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
