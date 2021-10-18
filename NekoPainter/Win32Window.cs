using System;
using System.Diagnostics;
using NekoPainter.Interoperation;
using static NekoPainter.Interoperation.User32;
using System.Drawing;
using NekoPainter.Controller;
using NekoPainter.UI;
using System.Numerics;
using System.Threading.Tasks;
using System.Threading;

namespace NekoPainter
{
    public class Win32Window : IDisposable
    {
        public string Title;
        public int Width;
        public int Height;
        public IntPtr hwnd;
        public bool IsMinimized;
        ImGuiInputHandler imguiInputHandler;
        AppController appController;
        System.Diagnostics.Stopwatch stopwatch;
        CancellationTokenSource cancellationTokenSource;
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

            this.hwnd = CreateWindowEx(
                (int)styleEx, wndClass, Title, (int)style,
                x, y, windowWidth, windowHeight,
                IntPtr.Zero, IntPtr.Zero, IntPtr.Zero, IntPtr.Zero);

        }
        public void Initialize()
        {
            cancellationTokenSource = new CancellationTokenSource();
            Task.Run(anotherThreadTask, cancellationTokenSource.Token);
        }

        public void anotherThreadTask()
        {
            Initialize1();
            while (!cancellationTokenSource.Token.IsCancellationRequested)
            {
                if (!IsMinimized)
                    Task1();
                else
                    Thread.Sleep(1);
            }
        }

        public void Initialize1()
        {
            appController = new AppController();
            appController.graphicsContext.DeviceResources.SetSwapChainPanel(hwnd, new Vector2(Width, Height));
            appController.graphicsContext.SetClearColor(new Vector4(0.2f, 0.2f, 0.2f, 1));
            appController.graphicsContext.ClearScreen();
            appController.graphicsContext.Present();
            ViewUIs.Initialize();
            imguiInputHandler = new ImGuiInputHandler();
            imguiInputHandler.hwnd = hwnd;

            stopwatch = Stopwatch.StartNew();
        }

        public void Task1()
        {
            var graphicsDevice = appController.graphicsContext.DeviceResources;
            if (graphicsDevice.m_outputSize != new Vector2(Width, Height))
                graphicsDevice.SetLogicalSize(new Vector2(Width, Height));
            long current = stopwatch.ElapsedTicks;
            long delta = current - lastTime;
            lastTime = current;
            ImGuiNET.ImGui.GetIO().DeltaTime = delta / 10000000.0f;
            imguiInputHandler.Update();
            appController.CanvasRender();
        }

        public void Update()
        {
            if (imguiInputHandler == null) return;
            if (imguiInputHandler.cursor != 0)
                User32.SetCursor(User32.LoadCursor(IntPtr.Zero, imguiInputHandler.cursor));
            else
                User32.SetCursor(IntPtr.Zero);

            imguiInputHandler.keyState[(int)VK.CONTROL] = User32.GetKeyState(VK.CONTROL);
            imguiInputHandler.keyState[(int)VK.SHIFT] = User32.GetKeyState(VK.SHIFT);
            imguiInputHandler.keyState[(int)VK.MENU] = User32.GetKeyState(VK.MENU);

            if (imguiInputHandler.wantSetMouseCursor)
            {
                imguiInputHandler.wantSetMouseCursor = false;
                var pos = new POINT((int)imguiInputHandler.setMousePos.X, (int)imguiInputHandler.setMousePos.Y);
                User32.ClientToScreen(hwnd, ref pos);
                User32.SetCursorPos(pos.X, pos.Y);
            }

            var foregroundWindow = User32.GetForegroundWindow();
            imguiInputHandler.isForegroundWindow = foregroundWindow == hwnd || User32.IsChild(foregroundWindow, hwnd);

            if (imguiInputHandler.isForegroundWindow)
            {
                POINT pos;
                if (User32.GetCursorPos(out pos) && User32.ScreenToClient(hwnd, ref pos))
                {
                    imguiInputHandler.mousePos = new System.Numerics.Vector2(pos.X, pos.Y);
                }
                var pos1 = imguiInputHandler.mousePos;
                if (pos1.X > 0 && pos1.Y > 0 && pos1.X < Width && pos1.Y < Height)
                    imguiInputHandler.mouseInRect = true;
                else
                    imguiInputHandler.mouseInRect = false;
            }
            else
                imguiInputHandler.mouseInRect = false;

            UIHelper.OnFrame();
            if (UIHelper.quit)
            {
                PostQuitMessage(0);
            }
        }

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
            if (hwnd != IntPtr.Zero)
            {
                cancellationTokenSource?.Cancel();
                DestroyWindow(hwnd);
                hwnd = IntPtr.Zero;
            }
        }

        public void Dispose()
        {
            Destroy();
        }
    }
}