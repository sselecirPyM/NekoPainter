using ImGuiNET;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace NekoPainter.UI
{
    public class ImguiInput
    {
        public static ConcurrentQueue<PenInputData> penInputData = new ConcurrentQueue<PenInputData>();

        #region mouse inputs
        public bool[] mouseDown = new bool[5];
        public Vector2 mousePos;
        public int mouseWheelH;
        public int mouseWheelV;
        #endregion

        public bool[] keydown = new bool[256];
        public bool KeyControl;
        public bool KeyShift;
        public bool KeyAlt;
        public bool KeySuper;
        public ConcurrentQueue<uint> inputChars = new ConcurrentQueue<uint>();
        #region outputs
        public bool WantCaptureMouse;
        public bool WantCaptureKeyboard;
        public bool WantSetMousePos;
        public bool WantTextInput;

        public Vector2 setMousePos;

        public ImGuiMouseCursor requestCursor;
        #endregion

        public void Update()
        {
            var io = ImGui.GetIO();
            for (int i = 0; i < 256; i++)
            {
                io.KeysDown[i] = keydown[i];
            }
            while (inputChars.TryDequeue(out uint char1))
                io.AddInputCharacter(char1);

            io.MouseWheel += Interlocked.Exchange(ref mouseWheelV, 0);
            io.MouseWheelH += Interlocked.Exchange(ref mouseWheelH, 0);

            io.KeyCtrl = KeyControl;
            io.KeyShift = KeyShift;
            io.KeyAlt = KeyAlt;
            io.KeySuper = KeySuper;

            #region outputs
            WantCaptureKeyboard = io.WantCaptureKeyboard;
            WantCaptureMouse = io.WantCaptureMouse;
            WantSetMousePos = io.WantSetMousePos;
            WantTextInput = io.WantTextInput;

            setMousePos = io.MousePos;
            requestCursor = ImGui.GetMouseCursor();
            #endregion

            #region mouse inputs
            for (int i = 0; i < 5; i++)
                io.MouseDown[i] = mouseDown[i];
            io.MousePos = mousePos;
            #endregion

            //mouseDrawCursor = ImGui.GetIO().MouseDrawCursor;
            //var mouseCursor = mouseDrawCursor ? ImGuiMouseCursor.None : ImGui.GetMouseCursor();
            //if (mouseCursor != lastCursor)
            //{
            //    lastCursor = mouseCursor;
            //    UpdateMouseCursor();
            //}
        }

        //bool UpdateMouseCursor()
        //{
        //    var io = ImGui.GetIO();
        //    if ((io.ConfigFlags & ImGuiConfigFlags.NoMouseCursorChange) != 0)
        //        return false;

        //    if (requestCursor == ImGuiMouseCursor.None || mouseDrawCursor)
        //        cursor = 0;
        //    else
        //    {
        //        cursor = SystemCursor.IDC_ARROW;
        //        switch (requestCursor)
        //        {
        //            case ImGuiMouseCursor.Arrow: cursor = SystemCursor.IDC_ARROW; break;
        //            case ImGuiMouseCursor.TextInput: cursor = SystemCursor.IDC_IBEAM; break;
        //            case ImGuiMouseCursor.ResizeAll: cursor = SystemCursor.IDC_SIZEALL; break;
        //            case ImGuiMouseCursor.ResizeEW: cursor = SystemCursor.IDC_SIZEWE; break;
        //            case ImGuiMouseCursor.ResizeNS: cursor = SystemCursor.IDC_SIZENS; break;
        //            case ImGuiMouseCursor.ResizeNESW: cursor = SystemCursor.IDC_SIZENESW; break;
        //            case ImGuiMouseCursor.ResizeNWSE: cursor = SystemCursor.IDC_SIZENWSE; break;
        //            case ImGuiMouseCursor.Hand: cursor = SystemCursor.IDC_HAND; break;
        //            case ImGuiMouseCursor.NotAllowed: cursor = SystemCursor.IDC_NO; break;
        //        }
        //    }
        //    return true;
        //}

        public void InputChar(char c)
        {
            inputChars.Enqueue(c);
        }

        public void MousePosition(Vector2 position)
        {
            mousePos = position;
        }

        public void KeyDown()
        {

        }

        //public IntPtr hwnd;
        //public bool mouseDrawCursor;
        //public bool mouseInRect;
        //public bool isForegroundWindow;
        //public bool isAnyMouseDown { get => mouseDown.All(u => u); }

        //public bool ProcessMessage(WindowMessage msg, UIntPtr wParam, IntPtr lParam)
        //{
        //    switch (msg)
        //    {
        //        case WindowMessage.LButtonDown:
        //        case WindowMessage.LButtonDoubleClick:
        //        case WindowMessage.RButtonDown:
        //        case WindowMessage.RButtonDoubleClick:
        //        case WindowMessage.MButtonDown:
        //        case WindowMessage.MButtonDoubleClick:
        //        case WindowMessage.XButtonDown:
        //        case WindowMessage.XButtonDoubleClick:
        //            {
        //                int button = 0;
        //                if (msg == WindowMessage.LButtonDown || msg == WindowMessage.LButtonDoubleClick) { button = 0; }
        //                if (msg == WindowMessage.RButtonDown || msg == WindowMessage.RButtonDoubleClick) { button = 1; }
        //                if (msg == WindowMessage.MButtonDown || msg == WindowMessage.MButtonDoubleClick) { button = 2; }
        //                if (msg == WindowMessage.XButtonDown || msg == WindowMessage.XButtonDoubleClick) { button = (GET_XBUTTON_WPARAM(wParam) == 1) ? 3 : 4; }
        //                if (mouseInRect || isAnyMouseDown)
        //                {
        //                    if (!isAnyMouseDown && User32.GetCapture() == IntPtr.Zero)
        //                        User32.SetCapture(hwnd);
        //                    mouseDown[button] = true;
        //                }
        //                return false;
        //            }
        //        case WindowMessage.LButtonUp:
        //        case WindowMessage.RButtonUp:
        //        case WindowMessage.MButtonUp:
        //        case WindowMessage.XButtonUp:
        //            {
        //                int button = 0;
        //                if (msg == WindowMessage.LButtonUp) { button = 0; }
        //                if (msg == WindowMessage.RButtonUp) { button = 1; }
        //                if (msg == WindowMessage.MButtonUp) { button = 2; }
        //                if (msg == WindowMessage.XButtonUp) { button = (GET_XBUTTON_WPARAM(wParam) == 1) ? 3 : 4; }
        //                mouseDown[button] = false;
        //                if (!isAnyMouseDown && User32.GetCapture() == hwnd)
        //                    User32.ReleaseCapture();
        //                return false;
        //            }
        //        case WindowMessage.MouseWheel:
        //            mouseWheelV += GET_WHEEL_DELTA_WPARAM(wParam) / WHEEL_DELTA;
        //            return false;
        //        case WindowMessage.MouseHWheel:
        //            mouseWheelH += GET_WHEEL_DELTA_WPARAM(wParam) / WHEEL_DELTA;
        //            return false;
        //        case WindowMessage.KeyDown:
        //        case WindowMessage.SysKeyDown:
        //            if ((ulong)wParam < 256)
        //            {
        //                keydown[(int)wParam] = true;
        //            }
        //            return false;
        //        case WindowMessage.KeyUp:
        //        case WindowMessage.SysKeyUp:
        //            if ((ulong)wParam < 256)
        //            {
        //                keydown[(int)wParam] = false;
        //            }
        //            return false;
        //        case WindowMessage.Char:
        //            inputChars.Enqueue((uint)wParam);
        //            return false;
        //        case WindowMessage.SetCursor:
        //            if (Utils.Loword((int)(long)lParam) == 1 && mouseInRect)
        //                return true;
        //            return false;
        //    }
        //    return false;
        //}

        //static int WHEEL_DELTA = 120;
    }
}
