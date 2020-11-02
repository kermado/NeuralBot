using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Security.Permissions;

namespace AimBot.Input
{
    public class GlobalWindow : IDisposable
    {
        #region P/Invoke Signatures
        private static long SWEH_CHILDID_SELF = 0;

        public enum SWEH_dwFlags : uint
        {
            WINEVENT_OUTOFCONTEXT = 0x0000,
            WINEVENT_SKIPOWNTHREAD = 0x0001,
            WINEVENT_SKIPOWNPROCESS = 0x0002,
            WINEVENT_INCONTEXT = 0x0004
        }

        private enum SWEH_Events : uint
        {
            EVENT_MIN = 0x00000001,
            EVENT_MAX = 0x7FFFFFFF,
            EVENT_SYSTEM_SOUND = 0x0001,
            EVENT_SYSTEM_ALERT = 0x0002,
            EVENT_SYSTEM_FOREGROUND = 0x0003,
            EVENT_SYSTEM_MENUSTART = 0x0004,
            EVENT_SYSTEM_MENUEND = 0x0005,
            EVENT_SYSTEM_MENUPOPUPSTART = 0x0006,
            EVENT_SYSTEM_MENUPOPUPEND = 0x0007,
            EVENT_SYSTEM_CAPTURESTART = 0x0008,
            EVENT_SYSTEM_CAPTUREEND = 0x0009,
            EVENT_SYSTEM_MOVESIZESTART = 0x000A,
            EVENT_SYSTEM_MOVESIZEEND = 0x000B,
            EVENT_SYSTEM_CONTEXTHELPSTART = 0x000C,
            EVENT_SYSTEM_CONTEXTHELPEND = 0x000D,
            EVENT_SYSTEM_DRAGDROPSTART = 0x000E,
            EVENT_SYSTEM_DRAGDROPEND = 0x000F,
            EVENT_SYSTEM_DIALOGSTART = 0x0010,
            EVENT_SYSTEM_DIALOGEND = 0x0011,
            EVENT_SYSTEM_SCROLLINGSTART = 0x0012,
            EVENT_SYSTEM_SCROLLINGEND = 0x0013,
            EVENT_SYSTEM_SWITCHSTART = 0x0014,
            EVENT_SYSTEM_SWITCHEND = 0x0015,
            EVENT_SYSTEM_MINIMIZESTART = 0x0016,
            EVENT_SYSTEM_MINIMIZEEND = 0x0017,
            EVENT_SYSTEM_DESKTOPSWITCH = 0x0020,
            EVENT_SYSTEM_END = 0x00FF,
            EVENT_OEM_DEFINED_START = 0x0101,
            EVENT_OEM_DEFINED_END = 0x01FF,
            EVENT_UIA_EVENTID_START = 0x4E00,
            EVENT_UIA_EVENTID_END = 0x4EFF,
            EVENT_UIA_PROPID_START = 0x7500,
            EVENT_UIA_PROPID_END = 0x75FF,
            EVENT_CONSOLE_CARET = 0x4001,
            EVENT_CONSOLE_UPDATE_REGION = 0x4002,
            EVENT_CONSOLE_UPDATE_SIMPLE = 0x4003,
            EVENT_CONSOLE_UPDATE_SCROLL = 0x4004,
            EVENT_CONSOLE_LAYOUT = 0x4005,
            EVENT_CONSOLE_START_APPLICATION = 0x4006,
            EVENT_CONSOLE_END_APPLICATION = 0x4007,
            EVENT_CONSOLE_END = 0x40FF,
            EVENT_OBJECT_CREATE = 0x8000,
            EVENT_OBJECT_DESTROY = 0x8001,
            EVENT_OBJECT_SHOW = 0x8002,
            EVENT_OBJECT_HIDE = 0x8003,
            EVENT_OBJECT_REORDER = 0x8004,
            EVENT_OBJECT_FOCUS = 0x8005,
            EVENT_OBJECT_SELECTION = 0x8006,
            EVENT_OBJECT_SELECTIONADD = 0x8007,
            EVENT_OBJECT_SELECTIONREMOVE = 0x8008,
            EVENT_OBJECT_SELECTIONWITHIN = 0x8009,
            EVENT_OBJECT_STATECHANGE = 0x800A,
            EVENT_OBJECT_LOCATIONCHANGE = 0x800B,
            EVENT_OBJECT_NAMECHANGE = 0x800C,
            EVENT_OBJECT_DESCRIPTIONCHANGE = 0x800D,
            EVENT_OBJECT_VALUECHANGE = 0x800E,
            EVENT_OBJECT_PARENTCHANGE = 0x800F,
            EVENT_OBJECT_HELPCHANGE = 0x8010,
            EVENT_OBJECT_DEFACTIONCHANGE = 0x8011,
            EVENT_OBJECT_ACCELERATORCHANGE = 0x8012,
            EVENT_OBJECT_INVOKED = 0x8013,
            EVENT_OBJECT_TEXTSELECTIONCHANGED = 0x8014,
            EVENT_OBJECT_CONTENTSCROLLED = 0x8015,
            EVENT_SYSTEM_ARRANGMENTPREVIEW = 0x8016,
            EVENT_OBJECT_END = 0x80FF,
            EVENT_AIA_START = 0xA000,
            EVENT_AIA_END = 0xAFFF
        }

        private enum SWEH_ObjectId : long
        {
            OBJID_WINDOW = 0x00000000,
            OBJID_SYSMENU = 0xFFFFFFFF,
            OBJID_TITLEBAR = 0xFFFFFFFE,
            OBJID_MENU = 0xFFFFFFFD,
            OBJID_CLIENT = 0xFFFFFFFC,
            OBJID_VSCROLL = 0xFFFFFFFB,
            OBJID_HSCROLL = 0xFFFFFFFA,
            OBJID_SIZEGRIP = 0xFFFFFFF9,
            OBJID_CARET = 0xFFFFFFF8,
            OBJID_CURSOR = 0xFFFFFFF7,
            OBJID_ALERT = 0xFFFFFFF6,
            OBJID_SOUND = 0xFFFFFFF5,
            OBJID_QUERYCLASSNAMEIDX = 0xFFFFFFF4,
            OBJID_NATIVEOM = 0xFFFFFFF0
        }

        private static SWEH_dwFlags WinEventHookInternalFlags = SWEH_dwFlags.WINEVENT_OUTOFCONTEXT | SWEH_dwFlags.WINEVENT_SKIPOWNPROCESS | SWEH_dwFlags.WINEVENT_SKIPOWNTHREAD;

        [DllImport("user32.dll")]
        private static extern uint GetWindowThreadProcessId(IntPtr hWnd, IntPtr voidProcessId);

        [DllImport("user32.dll", SetLastError = false)]
        private static extern IntPtr SetWinEventHook(SWEH_Events eventMin, SWEH_Events eventMax, IntPtr hmodWinEventProc, WinEventDelegate lpfnWinEventProc, uint idProcess, uint idThread, SWEH_dwFlags dwFlags);

        [DllImport("user32.dll", SetLastError = false)]
        private static extern bool UnhookWinEvent(IntPtr hWinEventHook);

        private delegate void WinEventDelegate(IntPtr hWinEventHook, SWEH_Events eventType, IntPtr hwnd, SWEH_ObjectId idObject, long idChild, uint dwEventThread, uint dwmsEventTime);

        private static uint GetWindowThread(IntPtr hWnd)
        {
            new UIPermission(UIPermissionWindow.AllWindows).Demand();
            return GetWindowThreadProcessId(hWnd, IntPtr.Zero);
        }

        private static IntPtr WinEventHookOne(SWEH_Events ev, WinEventDelegate wed, uint idProcess, uint idThread)
        {
            new UIPermission(UIPermissionWindow.AllWindows).Demand();
            return SetWinEventHook(ev, ev, IntPtr.Zero, wed, idProcess, idThread, WinEventHookInternalFlags);
        }
        #endregion

        public class MoveEventArgs : HandledEventArgs
        {
            public MoveEventArgs() { }
        }

        private Process process;
        private IntPtr windowHandle;
        private GCHandle hookHandle;
        private IntPtr hookId;
        private bool disposed;

        public event EventHandler<MoveEventArgs> MoveEvent;

        public GlobalWindow(Process process)
        {
            this.process = process;
            if (process != null)
            {
                windowHandle = process.MainWindowHandle;
                if (windowHandle != IntPtr.Zero)
                {
                    var eventDelegate = new WinEventDelegate(WindowEventCallback);
                    hookHandle = GCHandle.Alloc(eventDelegate);

                    var threadId = GetWindowThread(windowHandle);
                    hookId = WinEventHookOne(SWEH_Events.EVENT_OBJECT_LOCATIONCHANGE, eventDelegate, (uint)process.Id, threadId);
                }
            }
        }

        private void WindowEventCallback(IntPtr hWinEventHook, SWEH_Events eventType, IntPtr hWnd, SWEH_ObjectId idObject, long idChild, uint dwEventThread, uint dwmsEventTime)
        {
            if (hWnd == windowHandle)
            {
                if (eventType == SWEH_Events.EVENT_OBJECT_LOCATIONCHANGE && idObject == (SWEH_ObjectId)SWEH_CHILDID_SELF)
                {
                    MoveEvent?.Invoke(this, new MoveEventArgs());
                }
            }
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposed == false)
            {
                if (disposing)
                {
                    // Dispose managed state (managed objects)
                }

                // Free unmanaged resources (unmanaged objects).
                if (hookId != IntPtr.Zero) {
                    hookHandle.Free();
                    UnhookWinEvent(hookId);
                    hookId = IntPtr.Zero;
                }

                // Set large fields to null
                disposed = true;
            }
        }

        ~GlobalWindow()
        {
            Dispose(disposing: false);
        }

        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
    }
}
