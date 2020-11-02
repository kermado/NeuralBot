using System;
using System.Drawing;

using AimBot.Injectors;
using AimBot.Renderers;

namespace AimBot.Triggers
{
    public interface Trigger : IDisposable
    {
        void Trigger(MouseInjector injector, Point current, Point target, Rectangle region, Esp esp, ref bool aim, double dt);
        void Clear();
    }
}
