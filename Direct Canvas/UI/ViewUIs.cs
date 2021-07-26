using DirectCanvas.UI.Controller;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CanvasRendering;
using ImGuiNET;
using System.Numerics;

namespace DirectCanvas.UI
{
    public static class ViewUIs
    {
        public static bool Initialized = false;
        public static RenderTexture FontAtlas;
        public static ConstantBuffer constantBuffer;
        public static Mesh mesh;
        public static int selectedIndex = -1;
        //public static long TimeCost;
        public static void InputProcess()
        {
            var io = ImGui.GetIO();
            if (ImGui.GetCurrentContext() == default(IntPtr)) return;
            Vector2 mouseMoveDelta = new Vector2();
            float mouseWheelDelta = 0.0f;

            while (Input.inputDatas.TryDequeue(out InputData inputData))
            {
                if (inputData.inputType == InputType.MouseMove)
                    io.MousePos = inputData.point;
                else if (inputData.inputType == InputType.MouseLeftDown)
                {
                    io.MouseDown[0] = inputData.mouseDown;
                    io.MousePos = inputData.point;
                }
                else if (inputData.inputType == InputType.MouseRightDown)
                {
                    io.MouseDown[1] = inputData.mouseDown;
                    io.MousePos = inputData.point;
                }
                else if (inputData.inputType == InputType.MouseMiddleDown)
                {
                    io.MouseDown[2] = inputData.mouseDown;
                }
                else if (inputData.inputType == InputType.MouseWheelChanged)
                {
                    io.MouseWheel += inputData.mouseWheelDelta / 120.0f;
                    mouseWheelDelta += inputData.mouseWheelDelta;
                }
                else if (inputData.inputType == InputType.MouseMoveDelta)
                    mouseMoveDelta += inputData.point;
            }
            Input.deltaWheel = mouseWheelDelta;
        }
        public static void Draw()
        {
            var context = AppController.Instance.graphicsContext;
            var device = context.DeviceResources;
            if (!Initialized)
            {
                Initialized = true;
                Initialize();
            }
            var io = ImGui.GetIO();
            io.DisplaySize = device.m_d3dRenderTargetSize;
            var paintAgent = AppController.Instance?.CurrentCanvasCase?.PaintAgent;

            ImGui.NewFrame();
            ImGui.ShowDemoWindow();

            if (paintAgent != null)
            {
                var canvasCase = AppController.Instance?.CurrentCanvasCase;
                ImGui.SetNextWindowSize(new Vector2(200, 200), ImGuiCond.FirstUseEver);
                ImGui.SetNextWindowPos(new Vector2(200, 0), ImGuiCond.FirstUseEver);
                ImGui.Begin("混合模式");
                if (canvasCase.SelectedLayout != null)
                {
                    for (int i = 0; i < canvasCase.blendModes.Count; i++)
                    {
                        Core.BlendMode blendMode = canvasCase.blendModes[i];
                        bool selected = blendMode.Guid == canvasCase.SelectedLayout.BlendMode;
                        ImGui.Selectable(string.Format("{0}###{1}", blendMode.Name, blendMode.Guid), ref selected);
                        if (ImGui.IsItemHovered())
                            ImGui.SetTooltip(blendMode.Description);
                        if (blendMode.Guid != canvasCase.SelectedLayout.BlendMode && selected)
                        {
                            canvasCase.SetBlendMode(canvasCase.SelectedLayout, blendMode);
                        }
                    }
                }
                ImGui.End();

                LayoutsPanel();
                BrushParametersPanel();
                BrushPanel();
                LayoutInfoPanel();
            }

            ImGui.Render();
            Input.uiMouseCapture = io.WantCaptureMouse;
            Input.mousePos = io.MousePos;
            while (Input.penInputData.TryDequeue(out var result))
            {
                Input.penInputData1.Enqueue(result);
            }
            Input.penInputData.Clear();
        }

        static void LayoutsPanel()
        {
            var canvasCase = AppController.Instance?.CurrentCanvasCase;
            //var paintAgent = AppController.Instance?.CurrentCanvasCase?.PaintAgent;
            ImGui.SetNextWindowSize(new Vector2(200, 200), ImGuiCond.FirstUseEver);
            ImGui.SetNextWindowPos(new Vector2(0, 400), ImGuiCond.FirstUseEver);
            ImGui.Begin("图层");
            if (ImGui.Button("新建"))
            {
                if (selectedIndex != -1)
                {
                    canvasCase.NewStandardLayout(selectedIndex, 0);
                }
                else if (canvasCase != null)
                {
                    canvasCase.NewStandardLayout(0, 0);
                }
            }
            if (ImGui.IsItemHovered())
                ImGui.SetTooltip("新建图层");
            ImGui.SameLine();
            if (ImGui.Button("复制"))
            {
                if (selectedIndex != -1)
                    canvasCase.CopyLayout(selectedIndex);
            }
            ImGui.SameLine();
            if (ImGui.Button("删除"))
            {
                if (selectedIndex != -1)
                    canvasCase.DeleteLayout(selectedIndex);
            }

            if (canvasCase != null)
            {
                selectedIndex = -1;
                var layouts = canvasCase.Layouts;
                for (int i = 0; i < layouts.Count; i++)
                {
                    var layout = layouts[i];

                    bool selected = layout == canvasCase.SelectedLayout;
                    //if (ImGui.Button(string.Format("{0}###0{1}", layout.Hidden ? "显示" : "隐藏", layout.guid)))
                    //    layout.Hidden = !layout.Hidden;
                    //ImGui.SameLine();
                    ImGui.Selectable(string.Format("{0}###1{1}", layout.Name, layout.guid), ref selected);
                    if (selected)
                    {
                        if (layout != canvasCase.SelectedLayout)
                            canvasCase.SetActivatedLayout(layout);
                        canvasCase.SelectedLayout = layout;
                        selectedIndex = i;
                    }
                    if (ImGui.IsItemActive() && !ImGui.IsItemHovered())
                    {
                        int n_next = i + (ImGui.GetMouseDragDelta(0).Y < 0.0f ? -1 : 1);
                        if (n_next >= 0 && n_next < layouts.Count)
                        {
                            layouts[i] = layouts[n_next];
                            layouts[n_next] = layout;
                            ImGui.ResetMouseDragDelta();
                            canvasCase.UndoManager.AddUndoData(new Undo.CMD_MoveLayout(canvasCase, i, n_next));
                        }
                    }
                }
            }

            ImGui.EndChildFrame();
            ImGui.End();
        }

        static void LayoutInfoPanel()
        {
            var canvasCase = AppController.Instance?.CurrentCanvasCase;
            //var paintAgent = AppController.Instance?.CurrentCanvasCase?.PaintAgent;
            ImGui.SetNextWindowSize(new Vector2(200, 200), ImGuiCond.FirstUseEver);
            ImGui.SetNextWindowPos(new Vector2(200, 400), ImGuiCond.FirstUseEver);
            ImGui.Begin("图层信息");
            if (canvasCase.SelectedLayout != null)
            {
                var layout = canvasCase.SelectedLayout;
                ImGui.SliderFloat("Alpha", ref layout.Alpha, 0, 1);
                ImGui.ColorEdit4("颜色", ref layout.Color);
                ImGui.Checkbox("使用颜色", ref layout.UseColor);
                ImGui.Checkbox("隐藏", ref layout.Hidden);

                if (canvasCase.blendmodesMap.TryGetValue(layout.BlendMode, out var blendMode) && blendMode.Paramerters != null)
                {
                    for (int i = 0; i < blendMode.Paramerters.Length; i++)
                    {

                        ImGui.DragFloat(string.Format("{0}###{1}", blendMode.Paramerters[i].Name, i), ref blendMode.Paramerters[i].fValue);
                        if (ImGui.IsItemHovered())
                        {
                            ImGui.SetTooltip(blendMode.Paramerters[i].Description);
                        }
                    }
                }

            }
            ImGui.End();
        }

        static void BrushParametersPanel()
        {
            var paintAgent = AppController.Instance?.CurrentCanvasCase?.PaintAgent;
            ImGui.SetNextWindowSize(new Vector2(200, 200), ImGuiCond.FirstUseEver);
            ImGui.SetNextWindowPos(new Vector2(), ImGuiCond.FirstUseEver);
            ImGui.Begin("笔刷参数");
            //ImGui.Text(TimeCost.ToString());
            ImGui.SliderFloat("笔刷尺寸", ref paintAgent.BrushSize, 1, 300);
            ImGui.ColorEdit4("颜色", ref paintAgent._color);
            ImGui.ColorEdit4("颜色2", ref paintAgent._color2);
            ImGui.ColorEdit4("颜色3", ref paintAgent._color3);
            ImGui.ColorEdit4("颜色4", ref paintAgent._color4);
            if (paintAgent.currentBrush != null)
            {
                var brushParams = paintAgent.currentBrush.Parameters;
                for (int i = 0; i < brushParams.Length; i++)
                {
                    ImGui.DragFloat(string.Format("{0}###{1}", brushParams[i].Name, i), ref brushParams[i].fValue);
                }
            }
            ImGui.End();
        }

        static void BrushPanel()
        {
            var paintAgent = AppController.Instance?.CurrentCanvasCase?.PaintAgent;

            ImGui.SetNextWindowSize(new Vector2(200, 200), ImGuiCond.FirstUseEver);
            ImGui.SetNextWindowPos(new Vector2(0, 200), ImGuiCond.FirstUseEver);
            ImGui.Begin("笔刷");

            var brushes = paintAgent.brushes;
            for (int i = 0; i < brushes.Count; i++)
            {
                Core.Brush brush = brushes[i];
                bool selected = brush == paintAgent.currentBrush;
                ImGui.Selectable(brush.Name, ref selected);
                if (ImGui.IsItemHovered())
                    ImGui.SetTooltip(brush.Description);
                if (selected)
                {
                    paintAgent.SetBrush(brush);
                }
                if (ImGui.IsItemActive() && !ImGui.IsItemHovered())
                {
                    int n_next = i + (ImGui.GetMouseDragDelta(0).Y < 0.0f ? -1 : 1);
                    if (n_next >= 0 && n_next < brushes.Count)
                    {
                        brushes[i] = brushes[n_next];
                        brushes[n_next] = brush;
                        ImGui.ResetMouseDragDelta();
                    }
                }
            }
            ImGui.End();
        }

        public static void Render()
        {
            var data = ImGui.GetDrawData();
            var context = AppController.Instance.graphicsContext;
            float L = data.DisplayPos.X;
            float R = data.DisplayPos.X + data.DisplaySize.X;
            float T = data.DisplayPos.Y;
            float B = data.DisplayPos.Y + data.DisplaySize.Y;
            float[] mvp =
            {
                    2.0f/(R-L),   0.0f,           0.0f,       0.0f,
                    0.0f,         2.0f/(T-B),     0.0f,       0.0f,
                    0.0f,         0.0f,           0.5f,       0.0f,
                    (R+L)/(L-R),  (T+B)/(B-T),    0.5f,       1.0f,
            };
            constantBuffer.UpdateResource(new Span<float>(mvp));
            var vertexShader = AppController.Instance.vertexShaders["VSImgui"];
            var pixelShader = AppController.Instance.pixelShaders["PSImgui"];
            context.SetVertexShader(vertexShader);
            context.SetPixelShader(pixelShader);
            //context.SetCBV(constantBuffer, 0);
            context.SetCBV(constantBuffer, 0, 0, 256);
            Vector2 clip_off = data.DisplayPos;
            byte[] vertexDatas;
            byte[] indexDatas;
            unsafe
            {
                vertexDatas = new byte[data.TotalVtxCount * sizeof(ImDrawVert)];
                indexDatas = new byte[data.TotalIdxCount * sizeof(UInt16)];
                int vtxByteOfs = 0;
                int idxByteOfs = 0;
                for (int i = 0; i < data.CmdListsCount; i++)
                {
                    var cmdList = data.CmdListsRange[i];
                    var vertBytes = cmdList.VtxBuffer.Size * sizeof(ImDrawVert);
                    var indexBytes = cmdList.IdxBuffer.Size * sizeof(UInt16);
                    new Span<byte>(cmdList.VtxBuffer.Data.ToPointer(), vertBytes).CopyTo(new Span<byte>(vertexDatas, vtxByteOfs, vertBytes));
                    new Span<byte>(cmdList.IdxBuffer.Data.ToPointer(), indexBytes).CopyTo(new Span<byte>(indexDatas, idxByteOfs, indexBytes));
                    vtxByteOfs += vertBytes;
                    idxByteOfs += indexBytes;
                }
                mesh.Update(vertexDatas, indexDatas);
                //System.Diagnostics.Stopwatch stopwatch = new System.Diagnostics.Stopwatch();
                //stopwatch.Start();
                //stopwatch.Stop();
                //TimeCost = stopwatch.ElapsedTicks;
                int vtxOfs = 0;
                int idxOfs = 0;
                for (int i = 0; i < data.CmdListsCount; i++)
                {
                    var cmdList = data.CmdListsRange[i];
                    context.SetMesh(mesh);
                    for (int j = 0; j < cmdList.CmdBuffer.Size; j++)
                    {
                        var cmd = cmdList.CmdBuffer[j];
                        var rect = new Vortice.RawRect((int)(cmd.ClipRect.X - clip_off.X), (int)(cmd.ClipRect.Y - clip_off.Y), (int)(cmd.ClipRect.Z - clip_off.X), (int)(cmd.ClipRect.W - clip_off.Y));
                        context.RSSetScissorRect(rect);
                        context.SetSRV(FontAtlas, 0);
                        context.DrawIndexed((int)cmd.ElemCount, (int)cmd.IdxOffset + idxOfs, (int)cmd.VtxOffset + vtxOfs);
                    }
                    vtxOfs += cmdList.VtxBuffer.Size;
                    idxOfs += cmdList.IdxBuffer.Size;
                }
            }
            context.SetScissorRectDefault();
        }

        public static void Initialize()
        {
            var imguiContext = ImGui.CreateContext();
            ImGui.SetCurrentContext(imguiContext);
            var io = ImGui.GetIO();
            io.BackendFlags |= ImGuiBackendFlags.RendererHasVtxOffset;
            var device = AppController.Instance.graphicsContext.DeviceResources;
            constantBuffer = new ConstantBuffer(device, 64);
            mesh = new Mesh(device, 20);
            io.Fonts.AddFontFromFileTTF("c:\\Windows\\Fonts\\SIMHEI.ttf", 13, null, io.Fonts.GetGlyphRangesChineseFull());
            FontAtlas = new RenderTexture();
            unsafe
            {
                io.Fonts.GetTexDataAsRGBA32(out byte* pixels, out int width, out int height);
                int size = width * height * 4;
                byte[] data = new byte[size];
                Span<byte> _pixels = new Span<byte>(pixels, size);
                _pixels.CopyTo(data);

                FontAtlas.Create2(device, width, height, Vortice.DXGI.Format.R8G8B8A8_UNorm, false, data);
            }
            io.Fonts.TexID = new IntPtr(1);
        }
    }
}
