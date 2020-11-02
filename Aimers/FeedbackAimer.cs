using System;
using System.Drawing;

using AimBot.Injectors;

namespace AimBot.Aimers
{
    public class FeedbackAimer : Aimer
    {
        public enum ControllerType
        {
            P,
            PI,
            PID
        }

        private const double referenceFps = 100.0; // The FPS for which the controller parameters were tuned.

        private ControllerType type;
        private double ku;
        private double tu;
        private double pc;
        private double ic;
        private double dc;
        private double mxs;
        private double mys;
        private double mxa;
        private double mxd;
        private double mya;
        private double myd;
        private double cxd;
        private double cyud;
        private double cydd;

        private Point target;
        private Point position;

        private PID pidx;
        private PID pidy;

        private double time;
        private double ex;
        private double ey;
        private double pxs;
        private double pys;

        private bool disposed;

        public ControllerType Type
        {
            get { return type; }
            set
            {
                type = value;
                Configure();
            }
        }

        public double MaxHorizontalSpeed
        {
            get { return mxs; }
            set { mxs = value; }
        }

        public double MaxVerticalSpeed
        {
            get { return mys; }
            set { mys = value; }
        }

        public double MaxHorizontalAcceleration
        {
            get { return mxa; }
            set { mxa = value; }
        }

        public double MaxHorizontalDeceleration
        {
            get { return mxd; }
            set { mxd = value; }
        }

        public double MaxVerticalAcceleration
        {
            get { return mya; }
            set { mya = value; }
        }

        public double MaxVerticalDeceleration
        {
            get { return myd; }
            set { myd = value; }
        }

        public double CreepHorizontalDistance
        {
            get { return cxd; }
            set { cxd = value; }
        }

        public double CreepUpDistance
        {
            get { return cyud; }
            set { cyud = value; }
        }

        public double CreepDownDistance
        {
            get { return cydd; }
            set { cydd = value; }
        }

        public double CriticalGain
        {
            get { return ku; }
            set
            {
                ku = value;
                Configure();
            }
        }

        public double ProportionalConstant
        {
            get { return pc; }
            set
            {
                pc = value;
                Configure();
            }
        }

        public double IntegralConstant
        {
            get { return ic; }
            set
            {
                ic = value;
                Configure();
            }
        }

        public double DerivativeConstant
        {
            get { return dc; }
            set
            {
                dc = value;
                Configure();
            }
        }

        public double OscillationPeriod
        {
            get { return tu; }
            set
            {
                tu = value;
                Configure();
            }
        }

        public FeedbackAimer()
        {
            type = ControllerType.P;
            ku = 0.18;
            tu = 0.3;
            pc = 0.6;
            ic = 0.54;
            dc = 0.02;
            mxs = 600.0;
            mys = 600.0;
            mxa = 3500.0;
            mxd = 10000.0;
            mya = 2500.0;
            myd = 10000.0;
            cxd = 10.0;
            cyud = 10.0;
            cydd = 50.0;

            target = Point.Empty;
            position = Point.Empty;

            Configure();
            Clear();
        }

        public void Configure(double kp, double ki, double kd)
        {
            pidx = new PID(kp, ki, kd);
            pidy = new PID(kp, ki, kd);
        }

        public void Configure()
        {
            double kp = 0, ki = 0, kd = 0;
            switch (Type)
            {
                case ControllerType.P:
                    kp = 0.6 * CriticalGain;
                    ki = 0.0;
                    kd = 0.0;
                    break;
                case ControllerType.PI:
                    kp = ProportionalConstant * CriticalGain;
                    ki = IntegralConstant * CriticalGain / OscillationPeriod;
                    kd = 0.0;
                    break;
                case ControllerType.PID:
                    kp = ProportionalConstant * CriticalGain;
                    ki = IntegralConstant * CriticalGain / OscillationPeriod;
                    kd = DerivativeConstant * CriticalGain * OscillationPeriod;
                    break;
            }

            Configure(kp, ki, kd);
        }

        public void Clear()
        {
            pxs = 0.0;
            pys = 0.0;
            time = 0.0;
            ex = 0.0;
            ey = 0.0;

            pidx.Clear();
            pidy.Clear();
        }

        public void Aim(MouseInjector injector, Point target, Point position, double dt)
        {
            this.target = target;
            this.position = position;

            Move(injector, dt);
        }

        public void Tick(MouseInjector injector, bool aiming, double dt)
        {
            if (aiming)
            {
                Move(injector, dt);
            }
            else
            {
                Clear();
            }
        }

        private void Move(MouseInjector injector, double dt)
        {
            time += dt;

            if (target != Point.Empty)
            {
                var errorx = target.X - position.X;
                var errory = target.Y - position.Y;

                var dx = pidx.Tick(errorx, time);
                var dy = pidy.Tick(errory, time);

                var xs = dx / dt;
                var ys = dy / dt;

                if (errorx * errorx < cxd * cxd) { xs *= (errorx * errorx) / (cxd * cxd); }
                if (errory > 0 && errory * errory < cyud * cyud) { ys *= (errory * errory) / (cyud * cyud); }
                if (errory < 0 && errory * errory < cydd * cydd) { ys *= (errory * errory) / (cydd * cydd); }

                var xa = (xs - pxs) / dt;
                var ya = (ys - pys) / dt;

                if (Math.Sign(xs) * Math.Sign(pxs) >= 0)
                {
                    // Same direction.
                    if (Math.Abs(xs) >= Math.Abs(pxs))
                    {
                        // Acceleration.
                        if (mxa > 0.0)
                        {
                            if (xa > mxa) { xa = mxa; }
                            if (xa < -mxa) { xa = -mxa; }
                        }
                    }
                    else
                    {
                        // Deceleration
                        if (mxd > 0.0)
                        {
                            if (xa > mxd) { xa = mxd; }
                            if (xa < -mxd) { xa = -mxd; }
                        }
                    }

                    if (Math.Abs(ys) >= Math.Abs(pys))
                    {
                        // Acceleration.
                        if (mxa > 0.0)
                        {
                            if (ya > mya) { ya = mya; }
                            if (ya < -mya) { ya = -mya; }
                        }
                    }
                    else
                    {
                        // Deceleration
                        if (mxd > 0.0)
                        {
                            if (ya > myd) { ya = myd; }
                            if (ya < -myd) { ya = -myd; }
                        }
                    }
                }
                else
                {
                    // Different direction.
                    // We need to decelerate to a stop.
                    if (mxa > 0.0)
                    {
                        if (xa > mxd) { xa = mxd; }
                        if (xa < -mxd) { xa = -mxd; }
                    }

                    if (mxd > 0.0)
                    {
                        if (ya > myd) { ya = myd; }
                        if (ya < -myd) { ya = -myd; }
                    }
                }

                xs = pxs + xa * dt;
                ys = pys + ya * dt;

                if (xs > mxs) { xs = mxs; }
                if (xs < -mxs) { xs = -mxs; }
                if (ys > mys) { ys = mys; }
                if (ys < -mys) { ys = -mys; }

                dx = xs * dt + ex;
                dy = ys * dt + ey;

                var px = (int)Math.Round(dx);
                var py = (int)Math.Round(dy);

                ex = dx - px;
                ey = dy - py;

                pxs = xs;
                pys = ys;

                if (px != 0 || py != 0)
                {
                    injector.Move(px, py);
                }
            }
            else
            {
                Clear();
            }

            time = 0.0;
        }

        private struct PID
        {
            private double kp;
            private double ki;
            private double kd;

            private double integral;
            private double previousError;
            private bool idle;

            public PID(double kp, double ki, double kd)
            {
                this.kp = kp;
                this.ki = ki;
                this.kd = kd;

                integral = 0.0;
                previousError = 0.0;
                idle = true;
            }

            public double Tick(double error, double dt)
            {
                var derivative = (idle) ? 0.0 : error - previousError;
                idle = false;

                if (dt > 0)
                {
                    if (previousError != 0.0 && error * previousError < 0.0)
                    {
                        integral = 0.0;
                    }
                    else
                    {
                        integral += error * dt;
                    }

                    derivative /= dt;
                }

                previousError = error;

                return (dt * referenceFps) * (kp * error + ki * integral + kd * derivative);
            }

            public void Clear()
            {
                previousError = 0.0;
                integral = 0.0;
                idle = true;
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
