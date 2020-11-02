using System;
using System.Drawing;

using AimBot.Injectors;

namespace AimBot.Aimers
{
    public interface Aimer : IDisposable
    {
        void Aim(MouseInjector injector, Point target, Point position, double dt);
        void Tick(MouseInjector injector, bool aiming, double dt);
        void Clear();
    }
}
