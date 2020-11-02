using System;
using System.Runtime.InteropServices;
using System.Threading;

namespace AimBot.Injectors
{
    public class WindowsMouseInjector : MouseInjector
    {
        #region P/Invoke Signatures
        private enum SendInputEventType : int
        {
            InputMouse,
            InputKeyboard,
            InputHardware
        }

        [Flags]
        private enum MouseEventFlags : uint
        {
            MOUSEEVENTF_MOVE = 0x0001,
            MOUSEEVENTF_LEFTDOWN = 0x0002,
            MOUSEEVENTF_LEFTUP = 0x0004,
            MOUSEEVENTF_RIGHTDOWN = 0x0008,
            MOUSEEVENTF_RIGHTUP = 0x0010,
            MOUSEEVENTF_MIDDLEDOWN = 0x0020,
            MOUSEEVENTF_MIDDLEUP = 0x0040,
            MOUSEEVENTF_XDOWN = 0x0080,
            MOUSEEVENTF_XUP = 0x0100,
            MOUSEEVENTF_WHEEL = 0x0800,
            MOUSEEVENTF_VIRTUALDESK = 0x4000,
            MOUSEEVENTF_ABSOLUTE = 0x8000
        }

        private struct MouseInputData
        {
            public int dx;
            public int dy;
            public uint mouseData;
            public MouseEventFlags dwFlags;
            public uint time;
            public IntPtr dwExtraInfo;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct KeyInputData
        {
            public ushort wVk;
            public ushort wScan;
            public uint dwFlags;
            public uint time;
            public IntPtr dwExtraInfo;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct HardwareInputData
        {
            public int uMsg;
            public short wParamL;
            public short wParamH;
        }

        [StructLayout(LayoutKind.Explicit)]
        private struct MouseKeybdhardwareInputUnion
        {
            [FieldOffset(0)]
            public MouseInputData mi;

            [FieldOffset(0)]
            public KeyInputData ki;

            [FieldOffset(0)]
            public HardwareInputData hi;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct Input
        {
            public SendInputEventType type;
            public MouseKeybdhardwareInputUnion mkhi;
        }

        [DllImport("user32.dll", SetLastError = true)]
        private static extern uint SendInput(uint nInputs, ref Input pInputs, int cbSize);

        private void MouseLeftClick(int timeMs)
        {
            var mouseDownInput = new Input();
            mouseDownInput.type = SendInputEventType.InputMouse;
            mouseDownInput.mkhi.mi.dwFlags = MouseEventFlags.MOUSEEVENTF_LEFTDOWN;
            SendInput(1, ref mouseDownInput, Marshal.SizeOf(mouseDownInput));

            Thread.Sleep(timeMs);

            var mouseUpInput = new Input();
            mouseUpInput.type = SendInputEventType.InputMouse;
            mouseUpInput.mkhi.mi.dwFlags = MouseEventFlags.MOUSEEVENTF_LEFTUP;
            SendInput(1, ref mouseUpInput, Marshal.SizeOf(mouseUpInput));
        }

        private void MouseRightClick(int timeMs)
        {
            var mouseDownInput = new Input();
            mouseDownInput.type = SendInputEventType.InputMouse;
            mouseDownInput.mkhi.mi.dwFlags = MouseEventFlags.MOUSEEVENTF_RIGHTDOWN;
            SendInput(1, ref mouseDownInput, Marshal.SizeOf(mouseDownInput));

            Thread.Sleep(timeMs);

            var mouseUpInput = new Input();
            mouseUpInput.type = SendInputEventType.InputMouse;
            mouseUpInput.mkhi.mi.dwFlags = MouseEventFlags.MOUSEEVENTF_RIGHTUP;
            SendInput(1, ref mouseUpInput, Marshal.SizeOf(mouseUpInput));
        }

        private void MouseMove(int dx, int dy)
        {
            var mouseMoveInput = new Input();
            mouseMoveInput.type = SendInputEventType.InputMouse;
            mouseMoveInput.mkhi.mi.dwFlags = MouseEventFlags.MOUSEEVENTF_MOVE;
            mouseMoveInput.mkhi.mi.dx = dx;
            mouseMoveInput.mkhi.mi.dy = dy;
            SendInput(1, ref mouseMoveInput, Marshal.SizeOf(mouseMoveInput));
        }
        #endregion

        private ulong processId;

        public ulong ProcessId
        {
            get { return processId; }
            set { processId = value; }
        }

        public WindowsMouseInjector()
        {
            processId = 0;
        }

        public void Move(int dx, int dy)
        {
            MouseMove(dx, dy);
        }

        public void Click(int button, int releaseMilliseconds)
        {
            new Thread(() => {
                switch (button)
                {
                    case 0: MouseLeftClick(releaseMilliseconds); break;
                    case 1: MouseRightClick(releaseMilliseconds); break;
                }
            }).Start();
        }

        public void Dispose()
        {
            // Nothing to do.
        }        
    }
}
