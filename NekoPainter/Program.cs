using NekoPainter.Controller;
using NekoPainter.UI;
using System;
using System.Collections.Generic;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading;
using static SDL2.SDL;
using ImGuiNET;

namespace NekoPainter
{
    class Program
    {
        const uint PM_REMOVE = 1;

        static void Main(string[] args)
        {
            bool quitRequested = false;
            IntPtr window = SDL_CreateWindow("NekoPainter", SDL_WINDOWPOS_UNDEFINED, SDL_WINDOWPOS_UNDEFINED, 1024, 768, SDL_WindowFlags.SDL_WINDOW_RESIZABLE);
            AppController appController = new AppController();
            System.Diagnostics.Stopwatch stopwatch = System.Diagnostics.Stopwatch.StartNew();
            long lastTime = 0;

            SDL_GetWindowSize(window, out int Width, out int Height);
            SDL_SysWMinfo info = new SDL_SysWMinfo();
            SDL_GetWindowWMInfo(window, ref info);
            IntPtr hwnd = info.info.win.window;
            appController.SetSwapChain(hwnd, new Vector2(Width, Height));
            ViewUIs.Initialize();
            ImguiInput imguiInput = new ImguiInput();
            #region key map
            Dictionary<uint, int> sdlMouse2ImguiMouse = new Dictionary<uint, int>();
            sdlMouse2ImguiMouse[SDL_BUTTON_LEFT] = 0;
            sdlMouse2ImguiMouse[SDL_BUTTON_MIDDLE] = 2;
            sdlMouse2ImguiMouse[SDL_BUTTON_RIGHT] = 1;
            sdlMouse2ImguiMouse[SDL_BUTTON_X1] = 3;
            sdlMouse2ImguiMouse[SDL_BUTTON_X2] = 4;
            Dictionary<SDL_Keycode, int> sdlKeycode2ImguiKey = new Dictionary<SDL_Keycode, int>();
            for (int i = 'a'; i <= 'z'; i++)
                sdlKeycode2ImguiKey[(SDL_Keycode)i] = i - 32;
            for (int i = '0'; i <= '9'; i++)
                sdlKeycode2ImguiKey[(SDL_Keycode)i] = i;
            sdlKeycode2ImguiKey[SDL_Keycode.SDLK_BACKSPACE] = (int)ImGuiKey.Backspace;
            sdlKeycode2ImguiKey[SDL_Keycode.SDLK_DELETE] = (int)ImGuiKey.Delete;
            sdlKeycode2ImguiKey[SDL_Keycode.SDLK_RETURN] = (int)ImGuiKey.Enter;
            sdlKeycode2ImguiKey[SDL_Keycode.SDLK_RETURN2] = (int)ImGuiKey.Enter;
            sdlKeycode2ImguiKey[SDL_Keycode.SDLK_KP_ENTER] = (int)ImGuiKey.KeyPadEnter;
            sdlKeycode2ImguiKey[SDL_Keycode.SDLK_ESCAPE] = (int)ImGuiKey.Escape;
            sdlKeycode2ImguiKey[SDL_Keycode.SDLK_TAB] = (int)ImGuiKey.Tab;
            sdlKeycode2ImguiKey[SDL_Keycode.SDLK_KP_TAB] = (int)ImGuiKey.Tab;
            sdlKeycode2ImguiKey[SDL_Keycode.SDLK_PAGEDOWN] = (int)ImGuiKey.PageDown;
            sdlKeycode2ImguiKey[SDL_Keycode.SDLK_PAGEUP] = (int)ImGuiKey.PageUp;
            sdlKeycode2ImguiKey[SDL_Keycode.SDLK_AC_HOME] = (int)ImGuiKey.Home;
            sdlKeycode2ImguiKey[SDL_Keycode.SDLK_HOME] = (int)ImGuiKey.Home;
            sdlKeycode2ImguiKey[SDL_Keycode.SDLK_END] = (int)ImGuiKey.End;
            sdlKeycode2ImguiKey[SDL_Keycode.SDLK_SPACE] = (int)ImGuiKey.Space;
            sdlKeycode2ImguiKey[SDL_Keycode.SDLK_UP] = (int)ImGuiKey.UpArrow;
            sdlKeycode2ImguiKey[SDL_Keycode.SDLK_DOWN] = (int)ImGuiKey.DownArrow;
            sdlKeycode2ImguiKey[SDL_Keycode.SDLK_LEFT] = (int)ImGuiKey.LeftArrow;
            sdlKeycode2ImguiKey[SDL_Keycode.SDLK_RIGHT] = (int)ImGuiKey.RightArrow;
            sdlKeycode2ImguiKey[SDL_Keycode.SDLK_INSERT] = (int)ImGuiKey.Insert;
            #endregion
            while (!quitRequested)
            {
                while (SDL_PollEvent(out var sdlEvent) == 1)
                {
                    switch (sdlEvent.type)
                    {
                        case SDL_EventType.SDL_QUIT:
                            quitRequested = true;
                            break;
                        case SDL_EventType.SDL_WINDOWEVENT:
                            if (sdlEvent.window.windowEvent == SDL_WindowEventID.SDL_WINDOWEVENT_RESIZED)
                            {
                                Width = sdlEvent.window.data1;
                                Height = sdlEvent.window.data2;
                            }
                            break;
                        case SDL_EventType.SDL_KEYDOWN:
                            {
                                if (sdlKeycode2ImguiKey.TryGetValue(sdlEvent.key.keysym.sym, out int imkey))
                                    imguiInput.keydown[imkey] = true;
                            }
                            break;
                        case SDL_EventType.SDL_KEYUP:
                            {
                                if (sdlKeycode2ImguiKey.TryGetValue(sdlEvent.key.keysym.sym, out int imkey))
                                    imguiInput.keydown[imkey] = false;
                                break;
                            }
                        case SDL_EventType.SDL_TEXTINPUT:
                            {
                                string utf8Str;
                                unsafe
                                {
                                    utf8Str = Marshal.PtrToStringUTF8(new IntPtr(sdlEvent.text.text));
                                }
                                foreach (var c in utf8Str)
                                    imguiInput.InputChar(c);
                                break;
                            }
                        case SDL_EventType.SDL_MOUSEBUTTONDOWN:
                            imguiInput.mouseDown[sdlMouse2ImguiMouse[sdlEvent.button.button]] = true;
                            if (sdlEvent.button.button == SDL_BUTTON_LEFT)
                            {
                                ImguiInput.penInputData.Enqueue(new PenInputData() { point = new Vector2(sdlEvent.button.x, sdlEvent.button.y), penInputFlag = PenInputFlag.Begin });
                            }
                            break;
                        case SDL_EventType.SDL_MOUSEBUTTONUP:
                            imguiInput.mouseDown[sdlMouse2ImguiMouse[sdlEvent.button.button]] = false;
                            if (sdlEvent.button.button == SDL_BUTTON_LEFT)
                            {
                                ImguiInput.penInputData.Enqueue(new PenInputData() { point = new Vector2(sdlEvent.button.x, sdlEvent.button.y), penInputFlag = PenInputFlag.End });
                            }
                            break;
                        case SDL_EventType.SDL_MOUSEMOTION:
                            {
                                int x = sdlEvent.motion.x;
                                int y = sdlEvent.motion.y;
                                imguiInput.MousePosition(new Vector2(x, y));
                                ImguiInput.penInputData.Enqueue(new PenInputData() { point = new Vector2(x, y), penInputFlag = PenInputFlag.Drawing });
                            }
                            break;
                        case SDL_EventType.SDL_MOUSEWHEEL:
                            imguiInput.mouseWheelH += sdlEvent.wheel.x;
                            imguiInput.mouseWheelV += sdlEvent.wheel.y;
                            break;
                    }
                }

                var modState = SDL_GetModState();
                imguiInput.KeyAlt = (int)(modState & SDL_Keymod.KMOD_ALT) != 0;
                imguiInput.KeyShift = (int)(modState & SDL_Keymod.KMOD_SHIFT) != 0;
                imguiInput.KeyControl = (int)(modState & SDL_Keymod.KMOD_CTRL) != 0;

                imguiInput.Update();

                if (imguiInput.WantTextInput)
                    SDL_StartTextInput();
                else
                    SDL_StopTextInput();
                SDL_CaptureMouse(imguiInput.WantCaptureMouse ? SDL_bool.SDL_TRUE : SDL_bool.SDL_FALSE);

                long current = stopwatch.ElapsedTicks;
                long delta = current - lastTime;
                lastTime = current;
                var graphicsDevice = appController.graphicsContext.DeviceResources;
                if (graphicsDevice.m_outputSize != new Vector2(Width, Height))
                    graphicsDevice.SetLogicalSize(new Vector2(Width, Height));
                ImGuiNET.ImGui.GetIO().DeltaTime = delta / 10000000.0f;
                UIHelper.OnFrame();
                appController.CanvasRender();
            }
        }
    }
}
