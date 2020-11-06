using System;
using System.Drawing;
using System.Threading;

using AimBot.Injectors;

namespace AimBot.Aimers
{
    public class HybridAimer : Aimer
    {
        private Thread thread;
        private double gain; // Proportional gain.
        private double hs; // Horizontal sensitivity (mouse/screen).
        private double vs; // Vertical sensitivity (mouse/screen).
        private double ms; // Maximum speed (in pixels per second).
        private double fr; // Flick radius (in pixels).
        private int fd; // Flick delay (in milliseconds).
        private int ts; // Time-step (in milliseconds).
        private int td; // Trigger delay (in milliseconds).
        private int cd; // Cooldown (in milliseconds).
        private bool trigger; // Trigger after flicking?
        private bool continueAiming; // Continue aiming after flicking?

        private Point target;
        private Point position;

        private double time;
        private double ft;
        private double ex;
        private double ey;
        private bool ready;

        private bool disposed;

        public double MaxSpeed
        {
            get { return ms; }
            set { ms = value; }
        }

        public double Gain
        {
            get { return gain; }
            set { gain = value; }
        }

        public double FlickRadius
        {
            get { return fr; }
            set { fr = value; }
        }

        public int FlickDelayMs
        {
            get { return fd; }
            set { fd = value; }
        }

        public double HorizontalSensitivity
        {
            get { return hs; }
            set { hs = value; }
        }

        public double VerticalSensitivity
        {
            get { return vs; }
            set { vs = value; }
        }

        public int TimeStepMs
        {
            get { return ts; }
            set { ts = value; }
        }

        public bool Trigger
        {
            get { return trigger; }
            set { trigger = value; }
        }

        public int TriggerDelayMs
        {
            get { return td; }
            set { td = value; }
        }

        public int CooldownMs
        {
            get { return cd; }
            set { cd = value; }
        }

        public bool ContinueAiming
        {
            get { return continueAiming; }
            set { continueAiming = value; }
        }

        public HybridAimer()
        {
            gain = 0.1;
            hs = 0.65;
            vs = 0.65;
            ms = 4000.0;
            fd = 10;
            ts = 10;
            td = 50;
            cd = 250;
            trigger = true;
            continueAiming = false;

            Clear();
        }

        public void Clear()
        {
            time = 0.0;
            ft = 0.0;
            ex = 0.0;
            ey = 0.0;
            ready = true;
        }

        public void Aim(MouseInjector injector, Point target, Point position, double dt)
        {
            this.target = target;
            this.position = position;
            Move(injector, true, dt);
        }

        public void Tick(MouseInjector injector, bool aiming, double dt)
        {
            if (aiming)
            {
                Move(injector, false, dt);
            }
            else
            {
                Clear();
            }
        }

        private void Move(MouseInjector injector, bool canFlick, double dt)
        {
            if (ready && (thread == null || thread.IsAlive == false))
            {
                time += dt;

                if (target != Point.Empty)
                {
                    var errorx = target.X - position.X;
                    var errory = target.Y - position.Y;

                    // Determine speed using proportional control.
                    var dx = errorx * gain * time * 100.0 + ex;
                    var dy = errory * gain * time * 100.0 + ey;

                    var xs = dx / dt;
                    var ys = dy / dt;

                    if (xs >  ms) { xs =  ms; }
                    if (xs < -ms) { xs = -ms; }
                    if (ys >  ms) { ys =  ms; }
                    if (ys < -ms) { ys = -ms; }

                    // Can we flick?
                    if (canFlick && ft > 0.0 || Math.Sqrt(errorx * errorx + errory * errory) <= fr)
                    {
                        if (ft >= fd * 0.001)
                        {
                            ft = 0.0;
                            ready = continueAiming; // Wait for aim key/button to be pressed again?
                            thread = new Thread(() => Flick(injector, target, position, ms, hs, vs, ts, td, cd, trigger));
                            thread.Start();
                        }
                        else
                        {
                            ft += dt;
                        }
                    }
                    else // Move using proportional control.
                    {
                        dx = xs * dt;
                        dy = ys * dt;

                        var px = (int)Math.Round(dx);
                        var py = (int)Math.Round(dy);

                        ex = dx - px;
                        ey = dy - py;

                        if (px != 0 || py != 0)
                        {
                            injector.Move(px, py);
                        }
                    }
                }
                else
                {
                    ex = 0.0;
                    ey = 0.0;
                }
            }

            time = 0.0;
        }

        private static void Flick(MouseInjector injector, Point target, Point position, double flickSpeed, double horizontalSensitivity, double verticalSensitivity, int timestepMs, int triggerDelayMs, int cooldownMs, bool trigger)
        {
            double ex = 0.0;
            double ey = 0.0;

            var screenx = target.X - position.X;
            var screeny = target.Y - position.Y;

            var mousex = screenx * horizontalSensitivity;
            var mousey = screeny * verticalSensitivity;

            var distance = Math.Sqrt(mousex * mousex + mousey * mousey);
            var time = distance / flickSpeed;

            var frames = (int)Math.Ceiling((time * 1000) / timestepMs);
            for (int i = 0; i < frames; ++i)
            {
                var dx = ex + (double)mousex / frames;
                var dy = ey + (double)mousey / frames;

                var px = (int)Math.Round(dx);
                var py = (int)Math.Round(dy);

                ex = dx - px;
                ey = dy - py;

                if (px != 0 || py != 0)
                {
                    injector.Move(px, py);
                }

                if (i < frames - 1)
                {
                    Thread.Sleep(timestepMs);
                }
                else if (trigger)
                {
                    Thread.Sleep(triggerDelayMs);
                    var rng = new Random();
                    injector.Click(0, rng.Next(50, 80)); // TODO: Allow range to be configured?
                }
            }

            if (cooldownMs > 0)
            {
                Thread.Sleep(cooldownMs);
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
