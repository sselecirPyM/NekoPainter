using ImGuiNET;
using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using System.Collections.Concurrent;
using NekoPainter.Interoperation;
using System.Threading;

namespace NekoPainter
{
    public class ImGuiInputHandler
    {
        public IntPtr hwnd;
        ImGuiMouseCursor lastCursor;
        public bool mouseDrawCursor;
        public bool wantSetMouseCursor;
        public bool isForegroundWindow;
        public bool isAnyMouseDown { get => mouseDown.All(u => u); }
        public bool[] mouseDown = new bool[5];
        public bool mouseInRect;
        public System.Numerics.Vector2 mousePos;
        public System.Numerics.Vector2 setMousePos;
        public SystemCursor cursor = SystemCursor.IDC_ARROW;
        public short[] keyState = new short[256];
        public bool[] keydown = new bool[256];
        public ConcurrentQueue<uint> inputChars = new ConcurrentQueue<uint>();

        int mouseWheelH;
        int mouseWheelV;

        public ImGuiInputHandler()
        {
            InitKeyMap();
        }

        void InitKeyMap()
        {
            var io = ImGui.GetIO();

            io.KeyMap[(int)ImGuiKey.Tab] = (int)VK.TAB;
            io.KeyMap[(int)ImGuiKey.LeftArrow] = (int)VK.LEFT;
            io.KeyMap[(int)ImGuiKey.RightArrow] = (int)VK.RIGHT;
            io.KeyMap[(int)ImGuiKey.UpArrow] = (int)VK.UP;
            io.KeyMap[(int)ImGuiKey.DownArrow] = (int)VK.DOWN;
            io.KeyMap[(int)ImGuiKey.PageUp] = (int)VK.PRIOR;
            io.KeyMap[(int)ImGuiKey.PageDown] = (int)VK.NEXT;
            io.KeyMap[(int)ImGuiKey.Home] = (int)VK.HOME;
            io.KeyMap[(int)ImGuiKey.End] = (int)VK.END;
            io.KeyMap[(int)ImGuiKey.Insert] = (int)VK.INSERT;
            io.KeyMap[(int)ImGuiKey.Delete] = (int)VK.DELETE;
            io.KeyMap[(int)ImGuiKey.Backspace] = (int)VK.BACK;
            io.KeyMap[(int)ImGuiKey.Space] = (int)VK.SPACE;
            io.KeyMap[(int)ImGuiKey.Enter] = (int)VK.RETURN;
            io.KeyMap[(int)ImGuiKey.Escape] = (int)VK.ESCAPE;
            io.KeyMap[(int)ImGuiKey.KeyPadEnter] = (int)VK.RETURN;
            io.KeyMap[(int)ImGuiKey.A] = 'A';
            io.KeyMap[(int)ImGuiKey.C] = 'C';
            io.KeyMap[(int)ImGuiKey.V] = 'V';
            io.KeyMap[(int)ImGuiKey.X] = 'X';
            io.KeyMap[(int)ImGuiKey.Y] = 'Y';
            io.KeyMap[(int)ImGuiKey.Z] = 'Z';
        }

        public void Update()
        {
            var io = ImGui.GetIO();
            for (int i = 0; i < 5; i++)
                io.MouseDown[i] = mouseDown[i];
            for (int i = 0; i < 256; i++)
            {
                io.KeysDown[i] = keydown[i];
            }
            while (inputChars.TryDequeue(out uint char1))
                io.AddInputCharacter(char1);

            io.MouseWheel += Interlocked.Exchange(ref mouseWheelV, 0);
            io.MouseWheelH += Interlocked.Exchange(ref mouseWheelH, 0);

            io.KeyCtrl = (keyState[(int)VK.CONTROL] & 0x8000) != 0;
            io.KeyShift = (keyState[(int)VK.SHIFT] & 0x8000) != 0;
            io.KeyAlt = (keyState[(int)VK.MENU] & 0x8000) != 0;
            io.KeySuper = false;

            wantSetMouseCursor = io.WantSetMousePos;
            if (wantSetMouseCursor)
                setMousePos = io.MousePos;
            //io.MousePos = new System.Numerics.Vector2(-FLT_MAX, -FLT_MAX);

            if (isForegroundWindow)
                io.MousePos = mousePos;
            mouseDrawCursor = ImGui.GetIO().MouseDrawCursor;
            var mouseCursor = mouseDrawCursor ? ImGuiMouseCursor.None : ImGui.GetMouseCursor();
            if (mouseCursor != lastCursor)
            {
                lastCursor = mouseCursor;
                UpdateMouseCursor();
            }
        }

        bool UpdateMouseCursor()
        {
            var io = ImGui.GetIO();
            if ((io.ConfigFlags & ImGuiConfigFlags.NoMouseCursorChange) != 0)
                return false;

            var requestedcursor = ImGui.GetMouseCursor();
            if (requestedcursor == ImGuiMouseCursor.None || mouseDrawCursor)
                cursor = 0;
            else
            {
                cursor = SystemCursor.IDC_ARROW;
                switch (requestedcursor)
                {
                    case ImGuiMouseCursor.Arrow: cursor = SystemCursor.IDC_ARROW; break;
                    case ImGuiMouseCursor.TextInput: cursor = SystemCursor.IDC_IBEAM; break;
                    case ImGuiMouseCursor.ResizeAll: cursor = SystemCursor.IDC_SIZEALL; break;
                    case ImGuiMouseCursor.ResizeEW: cursor = SystemCursor.IDC_SIZEWE; break;
                    case ImGuiMouseCursor.ResizeNS: cursor = SystemCursor.IDC_SIZENS; break;
                    case ImGuiMouseCursor.ResizeNESW: cursor = SystemCursor.IDC_SIZENESW; break;
                    case ImGuiMouseCursor.ResizeNWSE: cursor = SystemCursor.IDC_SIZENWSE; break;
                    case ImGuiMouseCursor.Hand: cursor = SystemCursor.IDC_HAND; break;
                    case ImGuiMouseCursor.NotAllowed: cursor = SystemCursor.IDC_NO; break;
                }
            }
            return true;
        }

        public bool ProcessMessage(WindowMessage msg, UIntPtr wParam, IntPtr lParam)
        {
            switch (msg)
            {
                case WindowMessage.LButtonDown:
                case WindowMessage.LButtonDoubleClick:
                case WindowMessage.RButtonDown:
                case WindowMessage.RButtonDoubleClick:
                case WindowMessage.MButtonDown:
                case WindowMessage.MButtonDoubleClick:
                case WindowMessage.XButtonDown:
                case WindowMessage.XButtonDoubleClick:
                    {
                        int button = 0;
                        if (msg == WindowMessage.LButtonDown || msg == WindowMessage.LButtonDoubleClick) { button = 0; }
                        if (msg == WindowMessage.RButtonDown || msg == WindowMessage.RButtonDoubleClick) { button = 1; }
                        if (msg == WindowMessage.MButtonDown || msg == WindowMessage.MButtonDoubleClick) { button = 2; }
                        if (msg == WindowMessage.XButtonDown || msg == WindowMessage.XButtonDoubleClick) { button = (GET_XBUTTON_WPARAM(wParam) == 1) ? 3 : 4; }
                        if (mouseInRect || isAnyMouseDown)
                        {
                            if (!isAnyMouseDown && User32.GetCapture() == IntPtr.Zero)
                                User32.SetCapture(hwnd);
                            mouseDown[button] = true;
                        }
                        return false;
                    }
                case WindowMessage.LButtonUp:
                case WindowMessage.RButtonUp:
                case WindowMessage.MButtonUp:
                case WindowMessage.XButtonUp:
                    {
                        int button = 0;
                        if (msg == WindowMessage.LButtonUp) { button = 0; }
                        if (msg == WindowMessage.RButtonUp) { button = 1; }
                        if (msg == WindowMessage.MButtonUp) { button = 2; }
                        if (msg == WindowMessage.XButtonUp) { button = (GET_XBUTTON_WPARAM(wParam) == 1) ? 3 : 4; }
                        mouseDown[button] = false;
                        if (!isAnyMouseDown && User32.GetCapture() == hwnd)
                            User32.ReleaseCapture();
                        return false;
                    }
                case WindowMessage.MouseWheel:
                    mouseWheelV += GET_WHEEL_DELTA_WPARAM(wParam) / WHEEL_DELTA;
                    return false;
                case WindowMessage.MouseHWheel:
                    mouseWheelH += GET_WHEEL_DELTA_WPARAM(wParam) / WHEEL_DELTA;
                    return false;
                case WindowMessage.KeyDown:
                case WindowMessage.SysKeyDown:
                    if ((ulong)wParam < 256)
                    {
                        keydown[(int)wParam] = true;
                    }
                    return false;
                case WindowMessage.KeyUp:
                case WindowMessage.SysKeyUp:
                    if ((ulong)wParam < 256)
                    {
                        keydown[(int)wParam] = false;
                    }
                    return false;
                case WindowMessage.Char:
                    inputChars.Enqueue((uint)wParam);
                    return false;
                case WindowMessage.SetCursor:
                    if (Utils.Loword((int)(long)lParam) == 1 && mouseInRect)
                        return true;
                    return false;
            }
            return false;
        }

        static int WHEEL_DELTA = 120;
        static int GET_WHEEL_DELTA_WPARAM(IntPtr wParam) => Utils.Hiword((int)(long)wParam);
        static int GET_XBUTTON_WPARAM(IntPtr wParam) => Utils.Hiword((int)(long)wParam);
        static int GET_WHEEL_DELTA_WPARAM(UIntPtr wParam) => Utils.Hiword((int)(long)wParam);
        static int GET_XBUTTON_WPARAM(UIntPtr wParam) => Utils.Hiword((int)(long)wParam);
    }
}
