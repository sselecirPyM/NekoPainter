using NekoPainter.Interoperation;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using static NekoPainter.Interoperation.Kernel32;
using static NekoPainter.Interoperation.User32;

namespace NekoPainter
{
    class Program
    {
        const uint PM_REMOVE = 1;
        static bool quitRequested;

        static void Main(string[] args)
        {
            var moduleHandle = GetModuleHandle(null);

            var wndClass = new WNDCLASSEX
            {
                Size = Unsafe.SizeOf<WNDCLASSEX>(),
                Styles = WindowClassStyles.CS_HREDRAW | WindowClassStyles.CS_VREDRAW | WindowClassStyles.CS_OWNDC,
                WindowProc = WndProc,
                InstanceHandle = moduleHandle,
                CursorHandle = LoadCursor(IntPtr.Zero, SystemCursor.IDC_ARROW),
                BackgroundBrushHandle = IntPtr.Zero,
                IconHandle = IntPtr.Zero,
                ClassName = "WndClass",
            };

            RegisterClassEx(ref wndClass);
            Win32Window window1 = new Win32Window(wndClass.ClassName, "NekoPainter", 1024, 768);
            windows.Add(window1.Handle, window1);
            User32.ShowWindow(window1.Handle, ShowWindowCommand.Normal);
            window1.Initialize();
            while (!quitRequested)
            {
                while (!quitRequested && PeekMessage(out var msg, IntPtr.Zero, 0, 0, PM_REMOVE))
                {
                    TranslateMessage(ref msg);
                    DispatchMessage(ref msg);

                    if (msg.Value == (uint)WindowMessage.Quit)
                    {
                        quitRequested = true;
                        goto lable_stop;
                    }
                }

                foreach (var window in windows.Values)
                    window.Update();
            }
        lable_stop:

            foreach (var window in windows.Values)
                window.Dispose();
        }

        static Dictionary<IntPtr, Win32Window> windows = new Dictionary<IntPtr, Win32Window>();
        static IntPtr WndProc(IntPtr hWnd, uint msg, UIntPtr wParam, IntPtr lParam)
        {
            windows.TryGetValue(hWnd, out var window);

            if (window?.ProcessMessage(msg, wParam, lParam) ?? false)
                return IntPtr.Zero;

            switch ((WindowMessage)msg)
            {
                case WindowMessage.Destroy:
                    PostQuitMessage(0);
                    break;
            }

            return DefWindowProc(hWnd, msg, wParam, lParam);
        }
    }
}
