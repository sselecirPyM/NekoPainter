using NekoPainter.Controller;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CanvasRendering;
using ImGuiNET;
using imnodesNET;
using System.Numerics;
using Vortice.Direct3D11;
using Vortice.DXGI;
using NekoPainter.Util;
using NekoPainter.Core;
using NekoPainter.Nodes;

namespace NekoPainter.UI
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
            AppController appController = AppController.Instance;
            var context = appController.graphicsContext;
            var device = context.DeviceResources;
            if (!Initialized)
            {
                Initialize();
            }
            var io = ImGui.GetIO();
            io.DisplaySize = device.m_d3dRenderTargetSize;
            var document = AppController.Instance?.CurrentLivedDocument;

            ImGui.NewFrame();
            ImGui.ShowDemoWindow();
            Popups();
            MainMenuBar();

            while (Input.penInputData.TryDequeue(out var result))
            {
                Input.penInputData1.Enqueue(result);
            }
            //Input.penInputData.Clear();
            if (document != null)
            {
                LayoutsPanel();

                ImGui.SetNextWindowSize(new Vector2(200, 180), ImGuiCond.FirstUseEver);
                ImGui.SetNextWindowPos(new Vector2(200, 20), ImGuiCond.FirstUseEver);
                if (ImGui.Begin("混合模式"))
                {
                    if (document.SelectedLayout != null)
                    {
                        for (int i = 0; i < document.blendModes.Count; i++)
                        {
                            Core.BlendMode blendMode = document.blendModes[i];
                            bool selected = blendMode.Guid == document.SelectedLayout.BlendMode;
                            ImGui.Selectable(string.Format("{0}###{1}", blendMode.Name, blendMode.Guid), ref selected);
                            if (ImGui.IsItemHovered())
                                ImGui.SetTooltip(blendMode.Description);
                            if (blendMode.Guid != document.SelectedLayout.BlendMode && selected)
                            {
                                document.SetBlendMode(document.SelectedLayout, blendMode);
                            }
                        }
                    }
                }
                ImGui.End();

                LayoutInfoPanel();
                BrushPanel(appController);
                BrushParametersPanel(appController);
                ThumbnailPanel();
                NodesPanel();
                foreach (var livedDocument in AppController.Instance.livedDocuments)
                {
                    Canvas(livedDocument.Value, livedDocument.Key);
                }
            }

            ImGui.Render();
            Input.uiMouseCapture = io.WantCaptureMouse;
            Input.mousePos = io.MousePos;
        }

        static void LayoutsPanel()
        {
            var document = AppController.Instance?.CurrentLivedDocument;
            ImGui.SetNextWindowSize(new Vector2(200, 180), ImGuiCond.FirstUseEver);
            ImGui.SetNextWindowPos(new Vector2(0, 20), ImGuiCond.FirstUseEver);
            if (ImGuiExt.Begin("Layouts"))
            {
                if (ImGuiExt.Button("New"))
                {
                    if (selectedIndex != -1)
                    {
                        document.NewStandardLayout(selectedIndex);
                    }
                    else if (document != null)
                    {
                        document.NewStandardLayout(0);
                    }
                }

                if (ImGui.IsItemHovered())
                    ImGui.SetTooltip("新建图层");
                ImGui.SameLine();
                if (ImGuiExt.Button("Copy"))
                {
                    if (selectedIndex != -1)
                        document.CopyLayout(selectedIndex);
                }
                ImGui.SameLine();
                if (ImGuiExt.Button("Delete"))
                {
                    if (selectedIndex != -1)
                        document.DeleteLayout(selectedIndex);
                }

                if (document != null)
                {
                    selectedIndex = -1;
                    var layouts = document.Layouts;
                    for (int i = 0; i < layouts.Count; i++)
                    {
                        var layout = layouts[i];

                        bool selected = layout == document.SelectedLayout;
                        //if (ImGui.Button(string.Format("{0}###0{1}", layout.Hidden ? "显示" : "隐藏", layout.guid)))
                        //    layout.Hidden = !layout.Hidden;
                        //ImGui.SameLine();
                        ImGui.Selectable(string.Format("{0}###1{1}", layout.Name, layout.guid), ref selected);
                        if (selected)
                        {
                            if (layout != document.SelectedLayout)
                                document.SetActivatedLayout(layout);
                            document.SelectedLayout = layout;
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
                                document.UndoManager.AddUndoData(new Core.UndoCommand.CMD_MoveLayout(document, i, n_next));
                            }
                        }
                    }
                }
                ImGui.EndChildFrame();
            }
            ImGui.End();
        }

        static void LayoutInfoPanel()
        {
            var document = AppController.Instance?.CurrentLivedDocument;
            ImGui.SetNextWindowSize(new Vector2(200, 200), ImGuiCond.FirstUseEver);
            ImGui.SetNextWindowPos(new Vector2(200, 200), ImGuiCond.FirstUseEver);

            if (ImGui.Begin("图层信息") && document.SelectedLayout != null)
            {
                var layout = document.SelectedLayout;
                ImGui.SliderFloat("Alpha", ref layout.Alpha, 0, 1);
                ImGui.ColorEdit4("颜色", ref layout.Color);
                ImGuiExt.ComboBox("DataSource", ref layout.DataSource);
                ImGuiExt.Checkbox("Hidden", ref layout.Hidden);

                if (document.blendmodesMap.TryGetValue(layout.BlendMode, out var blendMode) && blendMode.Paramerters != null)
                {
                    for (int i = 0; i < blendMode.Paramerters.Length; i++)
                    {
                        layout.parameters.TryGetValue(blendMode.Paramerters[i].Name, out var parameter);
                        float f1 = (float)parameter.X;
                        parameter.Name = blendMode.Paramerters[i].Name;
                        if (ImGui.DragFloat(string.Format("{0}###{1}", blendMode.Paramerters[i].Name, i), ref f1))
                        {
                            parameter.X = f1;
                            layout.parameters[blendMode.Paramerters[i].Name] = parameter;
                        }
                        if (ImGui.IsItemHovered())
                        {
                            ImGui.SetTooltip(blendMode.Paramerters[i].Description);
                        }
                    }
                }
            }
            ImGui.End();
        }

        static void ThumbnailPanel()
        {
            var io = ImGui.GetIO();
            ImGui.SetNextWindowSize(new Vector2(200, 200), ImGuiCond.FirstUseEver);
            ImGui.SetNextWindowPos(new Vector2(200, 600), ImGuiCond.FirstUseEver);
            if (ImGuiExt.Begin("Thumbnail"))
            {
                string texPath = string.Format("{0}/Canvas", AppController.Instance.CurrentLivedDocument.Path);
                IntPtr imageId = new IntPtr(AppController.Instance.GetId(texPath));
                Vector2 pos = ImGui.GetCursorScreenPos();
                var tex = AppController.Instance.GetTexture(texPath);
                Vector2 spaceSize = ImGui.GetWindowSize() - new Vector2(20, 40);
                float factor = MathF.Max(MathF.Min(spaceSize.X / tex.width, spaceSize.Y / tex.height), 0.01f);

                Vector2 imageSize = new Vector2(tex.width * factor, tex.height * factor);

                ImGui.InvisibleButton("X", imageSize, ImGuiButtonFlags.MouseButtonLeft | ImGuiButtonFlags.MouseButtonRight | ImGuiButtonFlags.MouseButtonMiddle);
                ImGui.GetWindowDrawList().AddImage(imageId, pos, pos + imageSize);
                if (ImGui.IsItemHovered())
                {
                    Vector2 uv0 = (io.MousePos - pos) / imageSize - new Vector2(100, 100) / new Vector2(tex.width, tex.height);
                    Vector2 uv1 = uv0 + new Vector2(200, 200) / new Vector2(tex.width, tex.height);

                    ImGui.BeginTooltip();
                    ImGui.Image(imageId, new Vector2(100, 100), uv0, uv1);
                    ImGui.EndTooltip();
                }
            }
            ImGui.End();
        }


        static PenInputFlag currentState;
        static void Canvas(LivedNekoPainterDocument document, string path)
        {
            var io = ImGui.GetIO();
            var paintAgent = AppController.Instance?.CurrentLivedDocument?.PaintAgent;
            ImGui.SetNextWindowSize(new Vector2(400, 400), ImGuiCond.FirstUseEver);
            ImGui.SetNextWindowPos(new Vector2(400, 0), ImGuiCond.FirstUseEver);
            if (ImGui.Begin(string.Format("画布 {0}###{1}", document.Name, path)))
            {
                if (ImGui.IsWindowFocused())
                {
                    AppController.Instance.CurrentDCDocument = AppController.Instance.documents[path];
                    AppController.Instance.CurrentLivedDocument = AppController.Instance.livedDocuments[path];
                }
                string texPath = string.Format("{0}/Canvas", path);
                IntPtr imageId = new IntPtr(AppController.Instance.GetId(texPath));
                Vector2 pos = ImGui.GetCursorScreenPos();
                var tex = AppController.Instance.GetTexture(texPath);

                Vector2 spaceSize = ImGui.GetWindowSize() - new Vector2(20, 40);
                float factor = MathF.Max(MathF.Min(spaceSize.X / document.Width, spaceSize.Y / document.Height), 0.01f);

                Vector2 imageSize = new Vector2(document.Width * factor, document.Height * factor);

                ImGui.InvisibleButton("X", imageSize, ImGuiButtonFlags.MouseButtonLeft | ImGuiButtonFlags.MouseButtonRight | ImGuiButtonFlags.MouseButtonMiddle);
                ImGui.GetWindowDrawList().AddImage(imageId, pos, pos + imageSize);

                if (ImGui.IsItemActive() || (currentState == PenInputFlag.Drawing && ImGui.IsWindowFocused()))
                {
                    while (Input.penInputData1.TryDequeue(out var penInput))
                    {
                        if (!(currentState == PenInputFlag.End && penInput.penInputFlag == PenInputFlag.Drawing))
                        {
                            currentState = penInput.penInputFlag;
                            penInput.point = (penInput.point - pos) / factor;
                            switch (penInput.penInputFlag)
                            {
                                case PenInputFlag.Begin:
                                case PenInputFlag.Drawing:
                                case PenInputFlag.End:
                                    paintAgent.Draw(penInput);
                                    break;
                            }
                        }
                    }
                }

                //if (ImGui.IsItemHovered())
                //{
                //    Vector2 uv0 = (io.MousePos - pos) / imageSize - new Vector2(100, 100) / new Vector2(tex.width, tex.height);
                //    Vector2 uv1 = uv0 + new Vector2(200, 200) / new Vector2(tex.width, tex.height);

                //    ImGui.BeginTooltip();
                //    ImGui.Image(imageId, new Vector2(100, 100), uv0, uv1);
                //    ImGui.EndTooltip();
                //}
            }
            ImGui.End();
        }
        static Dictionary<int, int> nodeSocketStart = new Dictionary<int, int>();
        static List<int> socket2Node = new List<int>();
        static List<int> link2InputSocket = new List<int>();
        static Guid prevLayout;
        static HashSet<int> existNodes = new HashSet<int>();
        //static bool viewNodeTitleBar = false;
        static int[] selectednodes = null;
        static int[] selectedlinks = null;
        static void NodesPanel()
        {
            var currentLayout = AppController.Instance.CurrentLivedDocument?.ActivatedLayout;
            var document = AppController.Instance.CurrentLivedDocument;
            var graph = currentLayout?.graph;
            if (currentLayout == null)
            {
                existNodes.Clear();
                prevLayout = Guid.Empty;
            }
            else if (prevLayout != currentLayout.guid)
            {
                prevLayout = currentLayout.guid;
                existNodes.Clear();
            }
            else if (graph != null)
            {
                existNodes.RemoveWhere(u => !graph.Nodes.ContainsKey(u));
            }
            int numSelectedNodes = 0;
            int numSelectedLinks = 0;

            ImGui.SetNextWindowSize(new Vector2(400, 200), ImGuiCond.FirstUseEver);
            ImGui.SetNextWindowPos(new Vector2(0, 200), ImGuiCond.FirstUseEver);
            bool deleteNodes = false;
            bool setOutputNode = false;
            if (ImGui.Begin("节点编辑器"))
            {
                bool jumpToOutput = ImGui.Button("转到输出节点");
                ImGui.SameLine();
                setOutputNode = ImGui.Button("设为输出节点");
                ImGui.SameLine();
                deleteNodes = ImGuiExt.Button("Delete");
                //ImGui.SameLine();
                //ImGui.Checkbox("节点标题栏",ref viewNodeTitleBar);
                imnodes.BeginNodeEditor();
                nodeSocketStart.Clear();
                socket2Node.Clear();
                link2InputSocket.Clear();
                if (graph != null)
                {
                    foreach (var node in graph.Nodes)
                    {
                        if (node.Key == graph.outputNode)
                        {
                            imnodes.PushColorStyle(ColorStyle.NodeBackground, 0x994444ff);
                            imnodes.PushColorStyle(ColorStyle.NodeBackgroundHovered, 0x996666ff);
                            imnodes.PushColorStyle(ColorStyle.NodeBackgroundSelected, 0x997777ff);
                        }
                        imnodes.BeginNode(node.Value.Luid);
                        if (existNodes.Add(node.Value.Luid))
                        {
                            if (node.Value.Position == Vector2.Zero)
                                imnodes.SetNodeEditorSpacePos(node.Value.Luid, new Vector2(20, 30));
                            else
                                imnodes.SetNodeGridSpacePos(node.Value.Luid, node.Value.Position);
                        }
                        nodeSocketStart[node.Value.Luid] = socket2Node.Count;

                        //if(viewNodeTitleBar)
                        //{
                        //    imnodes.BeginNodeTitleBar();
                        //    ImGui.TextUnformatted(node.Value.GetNodeTypeName());
                        //    imnodes.EndNodeTitleBar();
                        //}
                        if (document.scriptNodeDefs.TryGetValue(node.Value.GetNodeTypeName(), out var nodeDef))
                        {
                            foreach (var socket in nodeDef.ioDefs)
                            {
                                if (socket.ioType == "input")
                                {
                                    imnodes.BeginInputAttribute(socket2Node.Count);
                                    ImGuiExt.Text(socket.displayName);
                                    imnodes.EndInputAttribute();
                                    socket2Node.Add(node.Key);
                                }
                                else if (socket.ioType == "output")
                                {
                                    imnodes.BeginOutputAttribute(socket2Node.Count);
                                    ImGuiExt.Text(socket.displayName);
                                    imnodes.EndOutputAttribute();
                                    socket2Node.Add(node.Key);
                                }
                            }
                        }
                        else
                        {

                        }
                        imnodes.EndNode();
                        if (node.Key == graph.outputNode)
                        {
                            imnodes.PopColorStyle();
                            imnodes.PopColorStyle();
                            imnodes.PopColorStyle();
                        }
                    }
                    int linkCount = 0;
                    foreach (var node in graph.Nodes)
                    {
                        if (node.Value.Inputs != null)
                        {
                            foreach (var pair in node.Value.Inputs)
                            {
                                var targetNode = graph.Nodes[pair.Value.targetUid];
                                var socketDefs = document.scriptNodeDefs[node.Value.GetNodeTypeName()].ioDefs;
                                var targetNodesocketDefs = document.scriptNodeDefs[targetNode.GetNodeTypeName()].ioDefs;

                                int inputSocketId = nodeSocketStart[node.Value.Luid] + socketDefs.FindIndex(u => u.name == pair.Key && u.ioType == "input");
                                int outputSocketId = nodeSocketStart[targetNode.Luid] + targetNodesocketDefs.FindIndex(u => u.name == pair.Value.targetSocket && u.ioType == "output");

                                imnodes.Link(linkCount, inputSocketId, outputSocketId);
                                link2InputSocket.Add(inputSocketId);
                                linkCount++;
                            }
                        }
                        node.Value.Position = imnodes.GetNodeGridSpacePos(node.Value.Luid);
                    }
                }
                if (jumpToOutput && graph != null && graph.Nodes.TryGetValue(graph.outputNode, out var outputNode1))
                {
                    imnodes.EditorContextMoveToNode(graph.outputNode);
                }
                numSelectedNodes = imnodes.NumSelectedNodes();
                numSelectedLinks = imnodes.NumSelectedLinks();
                if (numSelectedNodes > 0)
                {
                    if (selectednodes == null || selectednodes.Length < numSelectedNodes)
                        selectednodes = new int[numSelectedNodes];
                    imnodes.GetSelectedNodes(ref selectednodes[0]);
                    if (deleteNodes)
                    {
                        var removeNode = new Core.UndoCommand.CMD_Remove_RecoverNodes();
                        removeNode.BuildRemoveNodes(document, currentLayout.graph, new List<int>(selectednodes), currentLayout.guid);
                        var undoRemoveNode = removeNode.Execute();
                        document.UndoManager.AddUndoData(undoRemoveNode);
                    }
                    if (setOutputNode)
                    {
                        graph.outputNode = selectednodes[0];
                    }
                }
                if (numSelectedLinks > 0 && numSelectedNodes == 0)
                {
                    if (selectedlinks == null || selectedlinks.Length < numSelectedLinks)
                        selectedlinks = new int[numSelectedLinks];
                    imnodes.GetSelectedLinks(ref selectedlinks[0]);
                    if (deleteNodes)
                    {
                        var removeLink = new Core.UndoCommand.CMD_Remove_RecoverNodes();
                        removeLink.document = document;
                        removeLink.layoutGuid = currentLayout.guid;
                        removeLink.graph = graph;
                        removeLink.connectLinks = new List<LinkDesc>();
                        removeLink.setOutputNode = graph.outputNode;
                        foreach (var linkId1 in selectedlinks)
                        {
                            int inputSocket = link2InputSocket[linkId1];
                            int nodeId = socket2Node[inputSocket];

                            int socketStart = nodeSocketStart[nodeId];
                            int socketIndex = inputSocket - socketStart;
                            var iodef = document.scriptNodeDefs[graph.Nodes[nodeId].GetNodeTypeName()].ioDefs[socketIndex];
                            removeLink.connectLinks.Add(graph.DisconnectLink(nodeId, iodef.name));
                        }
                        document.UndoManager.AddUndoData(removeLink);
                    }
                }
                imnodes.EndNodeEditor();
                int linkA = 0;
                int linkB = 0;
                if (imnodes.IsLinkCreated(ref linkA, ref linkB))
                {
                    int nodeA = socket2Node[linkA];
                    int nodeB = socket2Node[linkB];
                    var socketDefsA = document.scriptNodeDefs[graph.Nodes[nodeA].GetNodeTypeName()].ioDefs;
                    var socketDefsB = document.scriptNodeDefs[graph.Nodes[nodeB].GetNodeTypeName()].ioDefs;
                    int nodeStartA = nodeSocketStart[nodeA];
                    int nodeStartB = nodeSocketStart[nodeB];

                    var undoCmd = new Core.UndoCommand.CMD_Remove_RecoverNodes();
                    if (graph.Nodes[nodeB].Inputs?.ContainsKey(socketDefsB[linkB - nodeStartB].name) == true)
                    {
                        var desc1 = graph.DisconnectLink(nodeB, socketDefsB[linkB - nodeStartB].name);
                        undoCmd.connectLinks = new List<LinkDesc>() { desc1 };
                    }
                    var desc2 = graph.Link(nodeA, socketDefsA[linkA - nodeStartA].name, nodeB, socketDefsB[linkB - nodeStartB].name);

                    currentLayout.generatePicture = true;
                    undoCmd.graph = currentLayout.graph;
                    undoCmd.disconnectLinks = new List<LinkDesc>() { desc2 };
                    undoCmd.setOutputNode = graph.outputNode;
                    undoCmd.layoutGuid = currentLayout.guid;
                    undoCmd.document = document;
                    if (graph.DependCheck())
                        document.UndoManager.AddUndoData(undoCmd);
                    else
                        undoCmd.Execute();
                }
                int linkId = 0;
                if (imnodes.IsLinkDestroyed(ref linkId))
                {

                }
                int nodeHovered = 0;
                if (imnodes.IsNodeHovered(ref nodeHovered))
                {
                    if (ImGui.IsMouseDown(ImGuiMouseButton.Right))
                    {
                        graph.outputNode = nodeHovered;
                    }
                }
            }
            ImGui.End();
            if (ImGui.Begin("节点属性"))
            {
                if (numSelectedNodes > 0 && graph != null && graph.Nodes.TryGetValue(selectednodes[0], out var selectedNode))
                {
                    if (selectedNode.paint2DNode != null)
                    {
                        bool colorChanged = false;
                        colorChanged |= ImGui.ColorEdit4("颜色", ref selectedNode.paint2DNode.color);
                        colorChanged |= ImGui.ColorEdit4("颜色2", ref selectedNode.paint2DNode.color2);
                        colorChanged |= ImGui.ColorEdit4("颜色3", ref selectedNode.paint2DNode.color3);
                        colorChanged |= ImGui.ColorEdit4("颜色4", ref selectedNode.paint2DNode.color4);
                        if (colorChanged)
                        {
                            graph.NodeParamCaches[selectednodes[0]].inputNodeModification.Clear();
                            currentLayout.generatePicture |= colorChanged;
                        }
                    }
                }
            }
            ImGui.End();
            if (ImGui.Begin("节点") && document != null)
            {
                foreach (var n in document.scriptNodeDefs)
                {
                    if (!n.Value.hidden && ImGui.Selectable(n.Key))
                    {
                        ScriptNode scriptNode = new ScriptNode();
                        scriptNode.nodeName = n.Key;
                        Node node = new Node();
                        node.scriptNode = scriptNode;
                        if (graph == null)
                        {
                            graph = new Graph();
                            currentLayout.graph = graph;
                            graph.Initialize();
                        }
                        graph.AddNode(node);
                        var undocmd = new Core.UndoCommand.CMD_Remove_RecoverNodes();
                        undocmd.BuildRemoveNodes(document, graph, new List<int>() { node.Luid }, currentLayout.guid);
                        document.UndoManager.AddUndoData(undocmd);
                    }
                }
            }
            ImGui.End();
        }

        static void BrushParametersPanel(AppController appController)
        {
            var paintAgent = appController?.CurrentLivedDocument?.PaintAgent;
            ImGui.SetNextWindowSize(new Vector2(200, 200), ImGuiCond.FirstUseEver);
            ImGui.SetNextWindowPos(new Vector2(0, 200), ImGuiCond.FirstUseEver);
            if (ImGui.Begin("笔刷参数"))
            {
                //ImGui.Text(TimeCost.ToString());
                ImGui.SliderFloat("笔刷尺寸", ref paintAgent.BrushSize, 1, 300);
                ImGui.ColorEdit4("颜色", ref paintAgent._color);
                ImGui.ColorEdit4("颜色2", ref paintAgent._color2);
                ImGui.ColorEdit4("颜色3", ref paintAgent._color3);
                ImGui.ColorEdit4("颜色4", ref paintAgent._color4);
                if (paintAgent.currentBrush != null && paintAgent.currentBrush.Parameters != null)
                {
                    var brushParams = paintAgent.currentBrush.Parameters;
                    for (int i = 0; i < brushParams.Length; i++)
                    {
                        float a1 = (float)brushParams[i].Value;
                        ImGui.DragFloat(string.Format("{0}###{1}", brushParams[i].Name, i), ref a1);
                        brushParams[i].Value = a1;
                    }
                }
            }
            ImGui.End();
        }

        static void BrushPanel(AppController appController)
        {
            var paintAgent = appController?.CurrentLivedDocument?.PaintAgent;

            ImGui.SetNextWindowSize(new Vector2(200, 200), ImGuiCond.FirstUseEver);
            ImGui.SetNextWindowPos(new Vector2(0, 400), ImGuiCond.FirstUseEver);
            if (ImGuiExt.Begin("Brushes"))
            {
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
            }

            ImGui.End();
        }

        static void MainMenuBar()
        {
            var document = AppController.Instance?.CurrentLivedDocument;
            bool canUndo = false;
            bool canRedo = false;
            if (document?.UndoManager.UndoStackIsNotEmpty == true)
                canUndo = true;
            if (document?.UndoManager.RedoStackIsNotEmpty == true)
                canRedo = true;

            var io = ImGui.GetIO();
            ImGui.BeginMainMenuBar();
            if (ImGuiExt.BeginMenu("File"))
            {
                if (ImGuiExt.MenuItem("New"))
                {
                    newDocument = true;
                }
                if (ImGuiExt.MenuItem("Open", "CTRL+O"))
                {
                    openDocument = true;
                }
                if (ImGuiExt.MenuItem("Save", "CTRL+S"))
                {
                    UIHelper.saveDocument = true;
                }
                ImGui.Separator();
                if (ImGuiExt.MenuItem("Import"))
                {
                    importImage = true;
                    UIHelper.selectOpenFile = true;
                }
                ImGuiExt.MenuItem("Export");
                ImGui.Separator();
                if (ImGuiExt.MenuItem("Exit"))
                {
                    UIHelper.quit = true;
                }
                ImGui.EndMenu();
            }
            if (ImGuiExt.BeginMenu("Edit"))
            {
                if (ImGuiExt.MenuItem("Undo", "CTRL+Z", false, canUndo))
                {
                    document.UndoManager.Undo();
                }
                if (ImGuiExt.MenuItem("Redo", "CTRL+Y", false, canRedo))
                {
                    document.UndoManager.Redo();
                }

                ImGui.Separator();
                ImGui.EndMenu();
            }
            if (ImGui.BeginMenu("View"))
            {
                ImGui.EndMenu();
            }
            if (document != null)
            {
                ImGui.Text(document.Name);
            }
            else
            {
                ImGui.Text("No document");
            }
            ImGui.EndMainMenuBar();
            if (document != null)
            {
                if (canUndo && io.KeyCtrl && ImGui.IsKeyPressed('Z'))
                {
                    document.UndoManager.Undo();
                }
                if (canRedo && io.KeyCtrl && ImGui.IsKeyPressed('Y'))
                {
                    document.UndoManager.Redo();
                }
                if (io.KeyCtrl && ImGui.IsKeyPressed('S'))
                {
                    UIHelper.saveDocument = true;
                }
            }
        }

        static void OpenDocument()
        {
            if (openDocument.SetFalse())
            {
                ImGui.OpenPopup("OpenDocument");
            }
            ImGui.SetNextWindowSize(new Vector2(400, 400), ImGuiCond.FirstUseEver);
            if (ImGui.BeginPopupModal("OpenDocument"))
            {
                if (UIHelper.folder != null)
                {
                    UIHelper.openDocumentPath = UIHelper.folder.FullName;
                    UIHelper.folder = null;
                }
                ImGui.SetNextItemWidth(200);
                ImGui.InputText("path", ref UIHelper.openDocumentPath, 260);
                ImGui.SameLine();
                if (ImGuiExt.Button("Browse"))
                {
                    UIHelper.selectFolder = true;
                }
                if (ImGuiExt.Button("Open") && !string.IsNullOrEmpty(UIHelper.openDocumentPath))
                {
                    ImGui.CloseCurrentPopup();
                    UIHelper.openDocument = true;
                }
                ImGui.SameLine();
                if (ImGuiExt.Button("Cancel"))
                {
                    ImGui.CloseCurrentPopup();
                }
                ImGui.EndPopup();
            }
        }

        static void Popups()
        {
            CreateDocument();
            OpenDocument();
            //ImportImage();
        }

        static void CreateDocument()
        {
            if (newDocument.SetFalse())
            {
                ImGui.OpenPopup("NewDocument");
                CreateDocumentParameters.Name = "NewDocument";
            }
            ImGui.SetNextWindowSize(new Vector2(400, 400), ImGuiCond.FirstUseEver);
            if (ImGui.BeginPopupModal("NewDocument"))
            {
                if (UIHelper.folder != null)
                {
                    CreateDocumentParameters.Folder = UIHelper.folder.FullName;
                    UIHelper.folder = null;
                }
                ImGui.SetNextItemWidth(200);
                ImGui.InputText("path", ref CreateDocumentParameters.Folder, 260);

                ImGui.SameLine();
                if (ImGuiExt.Button("Browse"))
                {
                    UIHelper.selectFolder = true;
                }
                ImGuiExt.InputText("Name", ref CreateDocumentParameters.Name, 200);
                ImGuiExt.InputInt("Width", ref CreateDocumentParameters.Width);
                ImGuiExt.InputInt("Height", ref CreateDocumentParameters.Height);
                if (ImGuiExt.Button("Create"))
                {
                    ImGui.CloseCurrentPopup();
                    UIHelper.createDocumentParameters = CreateDocumentParameters;
                    UIHelper.createDocument = true;
                }
                ImGui.SameLine();
                if (ImGuiExt.Button("Cancel"))
                {
                    ImGui.CloseCurrentPopup();
                }
                ImGui.EndPopup();
            }
        }

        static void ImportImage()
        {
            if (importImage.SetFalse())
            {
                ImGui.OpenPopup("ImportImage");
            }

            ImGui.SetNextWindowSize(new Vector2(400, 400), ImGuiCond.FirstUseEver);
            if (ImGui.BeginPopupModal("ImportImage"))
            {
                if (UIHelper.folder != null)
                {
                    UIHelper.importImagePath = UIHelper.openFile.FullName;
                    UIHelper.folder = null;
                }
                ImGui.SetNextItemWidth(200);
                ImGui.InputText("path", ref UIHelper.importImagePath, 260);
                ImGui.SameLine();
                if (ImGuiExt.Button("Browse"))
                {
                    UIHelper.selectOpenFile = true;
                }
                if (ImGuiExt.Button("Import") && !string.IsNullOrEmpty(UIHelper.importImagePath))
                {
                    ImGui.CloseCurrentPopup();

                }
                ImGui.SameLine();
                if (ImGuiExt.Button("Cancel"))
                {
                    ImGui.CloseCurrentPopup();
                }
                ImGui.EndPopup();
            }
        }

        public static void Render()
        {
            var data = ImGui.GetDrawData();
            var context = AppController.Instance.graphicsContext;
            var appcontroller = AppController.Instance;
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
                        context.SetSRV(appcontroller.textures[(long)cmd.TextureId], 0);
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
            Initialized = true;
            var appcontroller = AppController.Instance;
            var imguiContext = ImGui.CreateContext();
            ImGui.SetCurrentContext(imguiContext);
            imnodes.SetImGuiContext(imguiContext);
            imnodes.Initialize();
            var io = ImGui.GetIO();
            io.BackendFlags |= ImGuiBackendFlags.RendererHasVtxOffset;
            var device = AppController.Instance.graphicsContext.DeviceResources;
            constantBuffer = new ConstantBuffer(device, 64);
            mesh = new Mesh(device, 20, unnamedInputLayout);
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
            io.Fonts.TexID = new IntPtr(appcontroller.GetId("ImguiFont"));
            appcontroller.AddTexture("ImguiFont", FontAtlas);
        }

        public static FileFormat.CreateDocumentParameters CreateDocumentParameters = new FileFormat.CreateDocumentParameters();

        static bool newDocument;
        static bool openDocument;
        static bool importImage;
        public static UnnamedInputLayout unnamedInputLayout = new UnnamedInputLayout
        {
            inputElementDescriptions = new InputElementDescription[]
                {
                    new InputElementDescription("POSITION",0,Format.R32G32_Float,0),
                    new InputElementDescription("TEXCOORD",0,Format.R32G32_Float,0),
                    new InputElementDescription("COLOR",0,Format.R8G8B8A8_UNorm,0),
                }
        };
    }
}
