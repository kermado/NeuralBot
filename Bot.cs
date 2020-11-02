using System;
using System.Diagnostics;
using System.Drawing;

using Newtonsoft.Json;

using AimBot.Input;
using AimBot.Renderers;
using AimBot.Detectors;
using AimBot.Grabbers;
using AimBot.Trackers;
using AimBot.Selectors;
using AimBot.Injectors;
using AimBot.Aimers;
using AimBot.Triggers;

namespace AimBot
{
    public class Bot : IDisposable
    {
        public enum ActivationKeys
        {
            None = 0,
            Caps = 20,
        }

        public enum ActivationButtons
        {
            None = 0,
            Back = GlobalMouse.Button.Back,
            Forward = GlobalMouse.Button.Forward,
        }

        private Grabber grabber;
        private Detector detector;
        private Tracker tracker;
        private Selector selector;
        private MouseInjector injector;
        private Aimer aimer;
        private Trigger trigger;

        private Stopwatch stopwatch;

        private ActivationKeys activationKey;
        private ActivationButtons activationButton;
        
        private Process process;
        private double time;
        private bool activated;
        private bool disposed;

        [JsonIgnore]
        public Process Process
        {
            get { return process; }
            set
            {
                process = value;
                if (injector != null)
                {
                    injector.ProcessId = (process != null) ? (ulong)process.Id : 0;
                }
            }
        }

        public Grabber Grabber
        {
            get { return grabber; }
            set
            {
                grabber?.Dispose();
                grabber = value;
            }
        }

        public Detector Detector
        {
            get { return detector; }
            set
            {
                detector?.Dispose();
                detector = value;
            }
        }

        public Tracker Tracker
        {
            get { return tracker; }
            set
            {
                tracker?.Dispose();
                tracker = value;
            }
        }

        public Selector Selector
        {
            get { return selector; }
            set
            {
                selector?.Dispose();
                selector = value;
            }
        }

        public MouseInjector Injector
        {
            get { return injector; }
            set
            {
                injector?.Dispose();
                injector = value;
            }
        }

        public Aimer Aimer
        {
            get { return aimer; }
            set
            {
                aimer?.Dispose();
                aimer = value;
            }
        }

        public Trigger Trigger
        {
            get { return trigger; }
            set
            {
                trigger?.Dispose();
                trigger = value;
            }
        }

        public ActivationKeys ActivationKey
        {
            get { return activationKey; }
            set { activationKey = value; }
        }

        public ActivationButtons ActivationButton
        {
            get { return activationButton; }
            set { activationButton = value; }
        }

        public Bot()
        {
            grabber = null;
            detector = null;
            tracker = null;
            selector = null;
            injector = null;
            aimer = null;
            trigger = null;

            stopwatch = new Stopwatch();

            activationKey = ActivationKeys.None;
            activationButton = ActivationButtons.None;            

            process = null;
            time = 0.0;
            activated = false;
            disposed = false;
        }

        public void OnKey(GlobalKey.KeyEventArgs e)
        {
            if (e.Data.VirtualCode == (int)activationKey)
            {
                if (e.State == GlobalKey.State.KeyUp || e.State == GlobalKey.State.SysKeyUp)
                {
                    activated = false;
                }
                else
                {
                    activated = true;
                }
            }
        }

        public void OnButton(GlobalMouse.ButtonEventArgs e)
        {
            if ((e.Button & (GlobalMouse.Button)activationButton) != 0)
            {
                if (e.State == GlobalMouse.State.ButtonUp)
                {
                    activated = false;
                }
                else
                {
                    activated = true;
                }
            }
        }

        public bool Tick(double dt, Rectangle region, Esp esp)
        {
            time += dt;

            if (process != null)
            {
                var image = grabber.Grab(process.MainWindowHandle, region, esp, true, out var changed);
                if (image != IntPtr.Zero && changed)
                {
                    if (detector != null)
                    {
                        stopwatch.Restart();

                        var position = new Point(region.X + region.Width / 2, region.Y + region.Height / 2);
                        var detections = detector.Detect(image, region, esp);

                        if (tracker != null && selector != null)
                        {
                            tracker.Track(detections, esp, time);
                            var target = selector.Select(tracker, position, esp, activated, time);

                            if (aimer != null)
                            {
                                if (activated)
                                {
                                    aimer.Aim(injector, target, position, dt);
                                }
                                else
                                {
                                    aimer.Tick(injector, activated, dt);
                                }
                            }

                            if (trigger != null)
                            {
                                trigger.Trigger(injector, position, target, region, esp, ref activated, time);
                            }
                        }

                        time = 0.0;
                        stopwatch.Stop();

                        if (esp != null)
                        {
                            var fps = 1000.0 / (double)stopwatch.Elapsed.TotalMilliseconds;
                            esp.Add(new TextShape(new Point(region.X + region.Width, region.Y + 40), $"FPS: {Math.Round(fps)}", Color.Blue, 18));
                        }
                    }
                    else
                    {
                        tracker?.Clear();
                        selector?.Clear();
                        aimer?.Clear();
                        trigger?.Clear();
                    }

                    return activated;
                }
                else
                {
                    if (aimer != null)
                    {
                        aimer.Tick(injector, activated, dt);
                    }

                    stopwatch.Stop();
                }
            }

            return false;
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposed == false)
            {
                if (disposing)
                {
                    grabber?.Dispose();
                    detector?.Dispose();
                    tracker?.Dispose();
                    selector?.Dispose();
                    injector?.Dispose();
                    aimer?.Dispose();
                    trigger?.Dispose();
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
