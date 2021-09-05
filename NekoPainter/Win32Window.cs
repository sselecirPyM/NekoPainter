using System;
using System.Diagnostics;
using NekoPainter.Interoperation;
using static NekoPainter.Interoperation.User32;
using System.Drawing;
using NekoPainter.Controller;
using NekoPainter.UI;
using System.Numerics;

namespace NekoPainter
{
    public class Win32Window : IDisposable
    {
        public string Title;
        public int Width;
        public int Height;
        public IntPtr Handle;
        public bool IsMinimized;
        ImGuiInputHandler imguiInputHandler;
        AppController appController;
        System.Diagnostics.Stopwatch stopwatch;
        long lastTime;

        public Win32Window(string wndClass, string title, int width, int height)
        {
            Title = title;
            Width = width;
            Height = height;

            var screenWidth = GetSystemMetrics(SystemMetrics.SM_CXSCREEN);
            var screenHeight = GetSystemMetrics(SystemMetrics.SM_CYSCREEN);
            var x = (screenWidth - Width) / 2;
            var y = (screenHeight - Height) / 2;

            var style = WindowStyles.WS_OVERLAPPEDWINDOW;
            var styleEx = WindowExStyles.WS_EX_APPWINDOW | WindowExStyles.WS_EX_WINDOWEDGE;

            var windowRect = new Rectangle(0, 0, Width, Height);
            AdjustWindowRectEx(ref windowRect, style, false, styleEx);

            var windowWidth = windowRect.Right - windowRect.Left;
            var windowHeight = windowRect.Bottom - windowRect.Top;

            var hwnd = CreateWindowEx(
                (int)styleEx, wndClass, Title, (int)style,
                x, y, windowWidth, windowHeight,
                IntPtr.Zero, IntPtr.Zero, IntPtr.Zero, IntPtr.Zero);

            Handle = hwnd;
        }

        public void Initialize()
        {
            appController = new AppController();
            appController.graphicsContext.DeviceResources.SetSwapChainPanel(Handle, new Vector2(Width, Height));
            appController.graphicsContext.SetClearColor(new Vector4(0.2f, 0.2f, 0.2f, 1));
            appController.graphicsContext.ClearScreen();
            appController.graphicsContext.Present();
            ViewUIs.Initialize();
            imguiInputHandler = new ImGuiInputHandler();
            imguiInputHandler.hwnd = Handle;
            stopwatch = Stopwatch.StartNew();
        }

        public void Update()
        {
            long current = stopwatch.ElapsedTicks;
            long delta = current - lastTime;
            lastTime = current;
            var graphicsDevice = appController.graphicsContext.DeviceResources;
            if (graphicsDevice.m_outputSize != new Vector2(Width, Height))
                graphicsDevice.SetLogicalSize(new Vector2(Width, Height));
            ImGuiNET.ImGui.GetIO().DeltaTime = delta / 10000000.0f;
            imguiInputHandler.Update();
            UIHelper.OnFrame();
            appController.CanvasRender();
        }

        bool leftButtonDown = false;

        public bool ProcessMessage(uint msg, UIntPtr wParam, IntPtr lParam)
        {
            if (imguiInputHandler != null && imguiInputHandler.ProcessMessage((WindowMessage)msg, wParam, lParam))
                return true;

            switch ((WindowMessage)msg)
            {
                case WindowMessage.Size:
                    switch ((SizeMessage)wParam)
                    {
                        case SizeMessage.SIZE_RESTORED:
                        case SizeMessage.SIZE_MAXIMIZED:
                            IsMinimized = false;

                            var lp = (int)lParam;
                            Width = Utils.Loword(lp);
                            Height = Utils.Hiword(lp);
                            break;
                        case SizeMessage.SIZE_MINIMIZED:
                            IsMinimized = true;
                            break;
                        default:
                            break;
                    }
                    break;
                case WindowMessage.LButtonDown:
                case WindowMessage.LButtonUp:
                case WindowMessage.MouseMove:
                    pointProcess(msg, wParam, lParam);
                    break;
            }
            return false;
        }

        void pointProcess(uint msg, UIntPtr wParam, IntPtr lParam)
        {
            int x = Utils.Loword((int)lParam);
            int y = Utils.Hiword((int)lParam);

            var _wparam = (uint)wParam;
            if ((_wparam & 0x1) != 0)
                leftButtonDown = true;
            switch ((WindowMessage)msg)
            {
                case WindowMessage.LButtonDown:
                    Input.penInputData.Enqueue(new PenInputData() { point = new Vector2(x, y), penInputFlag = PenInputFlag.Begin });
                    break;
                case WindowMessage.LButtonUp:
                    Input.penInputData.Enqueue(new PenInputData() { point = new Vector2(x, y), penInputFlag = PenInputFlag.End });
                    break;
                case WindowMessage.MouseMove:
                    Input.penInputData.Enqueue(new PenInputData() { point = new Vector2(x, y), penInputFlag = PenInputFlag.Drawing });
                    break;
            }
        }

        public void Destroy()
        {
            if (Handle != IntPtr.Zero)
            {
                DestroyWindow(Handle);
                Handle = IntPtr.Zero;
            }
        }

        public void Dispose()
        {
            Destroy();
        }
    }
}