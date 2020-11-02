using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Runtime.InteropServices;

namespace AimBot.Input
{
    public class GlobalMouse : IDisposable
    {
        #region P/Invoke Signatures
        private const int WH_MOUSE_LL = 14;
        private const int WH_MOUSE = 7;
        private const uint LLMHF_INJECTED = 1;
        private const uint LLMHF_LOWER_IL_INJECTED = 2;

        private enum MouseMessages
        {
            WM_LBUTTONDOWN = 0x0201,
            WM_LBUTTONUP = 0x0202,
            WM_MOUSEMOVE = 0x0200,
            WM_MOUSEWHEEL = 0x020A,
            WM_RBUTTONDOWN = 0x0204,
            WM_RBUTTONUP = 0x0205,
            WM_XBUTTONDOWN = 0x020B,
            WM_XBUTTONUP = 0x020C
        }

        private enum XButtons
        {
            XBUTTON1 = 0x0001,
            XBUTTON2 = 0x0002
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct POINT
        {
            public int x;
            public int y;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct MSLLHOOKSTRUCT
        {
            public POINT pt;
            public uint mouseData;
            public uint flags;
            public uint time;
            public IntPtr dwExtraInfo;
        }

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr SetWindowsHookEx(int idHook, HookProc lpfn, IntPtr hMod, uint dwThreadId);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool UnhookWindowsHookEx(IntPtr hhk);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode, IntPtr wParam, IntPtr lParam);

        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr GetModuleHandle(string lpModuleName);
        #endregion

        public enum State
        {
            ButtonDown,
            ButtonUp,
            ButtonDoubleClick
        }

        [Flags]
        public enum Button
        {
            Left = 1 << 0,
            Right = 1 << 1,
            Back = 1 << 2,
            Forward = 1 << 3
        }

        public class ButtonEventArgs : HandledEventArgs
        {
            public Point Position { get; private set; }
            public State State { get; private set; }
            public Button Button { get; private set; }
            public bool Injected { get; private set; }

            public ButtonEventArgs(Point position, State state, Button button, bool injected)
            {
                Position = position;
                State = state;
                Button = button;
                Injected = injected;
            }
        }

        private delegate IntPtr HookProc(int nCode, IntPtr wParam, IntPtr lParam);
        private HookProc lowLevelMouseProc;
        private IntPtr lowLevelMouseHandle;

        public event EventHandler<ButtonEventArgs> ButtonEvent;

        public GlobalMouse()
        {
            lowLevelMouseProc = LowLevelMouseHookCallback;
            lowLevelMouseHandle = SetLowLevelMouseHook(lowLevelMouseProc);
        }

        private static IntPtr SetLowLevelMouseHook(HookProc proc)
        {
            return SetWindowsHookEx(WH_MOUSE_LL, proc, GetModuleHandle(Process.GetCurrentProcess().MainModule.ModuleName), 0);
        }

        private IntPtr LowLevelMouseHookCallback(int nCode, IntPtr wParam, IntPtr lParam)
        {
            if (nCode >= 0)
            {
                var hookStruct = (MSLLHOOKSTRUCT)Marshal.PtrToStructure(lParam, typeof(MSLLHOOKSTRUCT));
                var highWord = hookStruct.mouseData >> 16;
                var injected = (hookStruct.flags & (LLMHF_INJECTED | LLMHF_LOWER_IL_INJECTED)) != 0;
                var position = new Point(hookStruct.pt.x, hookStruct.pt.y);

                switch ((MouseMessages)wParam)
                {
                    case MouseMessages.WM_LBUTTONDOWN:
                        ButtonEvent?.Invoke(this, new ButtonEventArgs(position, State.ButtonDown, Button.Left, injected));
                        break;
                    case MouseMessages.WM_LBUTTONUP:
                        ButtonEvent?.Invoke(this, new ButtonEventArgs(position, State.ButtonUp, Button.Left, injected));
                        break;
                    case MouseMessages.WM_RBUTTONDOWN:
                        ButtonEvent?.Invoke(this, new ButtonEventArgs(position, State.ButtonDown, Button.Right, injected));
                        break;
                    case MouseMessages.WM_RBUTTONUP:
                        ButtonEvent?.Invoke(this, new ButtonEventArgs(position, State.ButtonUp, Button.Right, injected));
                        break;
                    case MouseMessages.WM_XBUTTONDOWN:
                        switch (highWord)
                        {
                            case (uint)XButtons.XBUTTON1:
                                ButtonEvent?.Invoke(this, new ButtonEventArgs(position, State.ButtonDown, Button.Back, injected));
                                break;
                            case (uint)XButtons.XBUTTON2:
                                ButtonEvent?.Invoke(this, new ButtonEventArgs(position, State.ButtonDown, Button.Forward, injected));
                                break;
                        }
                        break;
                    case MouseMessages.WM_XBUTTONUP:
                        switch (highWord)
                        {
                            case (uint)XButtons.XBUTTON1:
                                ButtonEvent?.Invoke(this, new ButtonEventArgs(position, State.ButtonUp, Button.Back, injected));
                                break;
                            case (uint)XButtons.XBUTTON2:
                                ButtonEvent?.Invoke(this, new ButtonEventArgs(position, State.ButtonUp, Button.Forward, injected));
                                break;
                        }
                        break;
                }
            }

            return CallNextHookEx(lowLevelMouseHandle, nCode, wParam, lParam);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (lowLevelMouseHandle != IntPtr.Zero)
                {
                    if (UnhookWindowsHookEx(lowLevelMouseHandle) == false)
                    {
                        var errorCode = Marshal.GetLastWin32Error();
                        throw new Win32Exception(errorCode, $"Failed to remove mouse hooks for '{Process.GetCurrentProcess().ProcessName}'. Error {errorCode}: {new Win32Exception(Marshal.GetLastWin32Error()).Message}.");
                    }

                    lowLevelMouseHandle = IntPtr.Zero;
                    lowLevelMouseProc -= LowLevelMouseHookCallback;
                }
            }
        }

        public void Dispose()
        {
            Dispose(true);
        }
    }
}
