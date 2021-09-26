using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System.Linq;
using NekoPainter.Core;
using NekoPainter.Util;
using NekoPainter.Nodes;
using CanvasRendering;
using System.Numerics;
using System.Runtime.InteropServices;
using System.IO;
using System;
using Microsoft.CodeAnalysis.CSharp.Scripting;
using Microsoft.CodeAnalysis.Scripting;
using NekoPainter.Data;

namespace NekoPainter
{
    public class ViewRenderer
    {
        public ViewRenderer(LivedNekoPainterDocument livedDocument)
        {
            this.livedDocument = livedDocument;
        }

        public void RenderAll()
        {
            if (ManagedLayout.Count == 0) return;
            PrepareRenderData();
            RenderTarget.Clear();
            int ofs = 0;
            var buffer = constantBuffer1.GetBuffer(DeviceResources);
            for (int i = ManagedLayout.Count - 1; i >= 0; i--)
            {
                PictureLayout selectedLayout = ManagedLayout[i];
                if (ManagedLayout[i].Hidden)
                {
                    ofs += 256;
                    continue;
                }


                livedDocument.LayoutTex.TryGetValue(selectedLayout.guid, out var tiledTexture);
                if (livedDocument.blendmodesMap.TryGetValue(selectedLayout.BlendMode, out var blendMode))
                {
                    if (selectedLayout.DataSource == LayoutDataSource.Color)
                    {
                        blendMode?.BlendPure(RenderTarget, buffer, ofs, 256);
                    }
                    else if (selectedLayout.generatePicture.SetFalse())
                    {
                        List<int> executeOrder;
                        if (selectedLayout.graph != null)
                        {
                            PaintingTexture.Clear();
                            if (selectedLayout.graph.Nodes.Count > 0)
                            {
                                executeOrder = selectedLayout.graph.GetExecuteList(selectedLayout.graph.outputNode);
                                ExecuteNodes(selectedLayout.graph, executeOrder);
                            }
                            if (selectedLayout.graph.NodeParamCaches.TryGetValue(selectedLayout.graph.outputNode, out var cache))
                            {
                                foreach (var cache1 in cache.outputCache)
                                    if (cache1.Value is TiledTexture t1)
                                        t1.UnzipToTexture(PaintingTexture);
                            }
                        }

                        blendMode?.Blend(PaintingTexture, RenderTarget, buffer, ofs, 256);

                        if (livedDocument.LayoutTex.TryGetValue(selectedLayout.guid, out var tiledTexture1)) tiledTexture1.Dispose();
                        var tiledTexture2 = new TiledTexture(PaintingTexture);
                        livedDocument.LayoutTex[selectedLayout.guid] = tiledTexture2;
                    }
                    else if (livedDocument.PaintAgent.CurrentLayout == selectedLayout)
                    {
                        List<int> executeOrder;
                        if (selectedLayout.graph != null)
                        {
                            PaintingTexture.Clear();
                            if (selectedLayout.graph.Nodes.Count > 0)
                            {
                                executeOrder = selectedLayout.graph.GetExecuteList(selectedLayout.graph.outputNode);
                                ExecuteNodes(selectedLayout.graph, executeOrder);
                            }
                            if (selectedLayout.graph.NodeParamCaches.TryGetValue(selectedLayout.graph.outputNode, out var cache))
                            {
                                foreach (var cache1 in cache.outputCache)
                                    if (cache1.Value is TiledTexture t1)
                                        t1.UnzipToTexture(PaintingTexture);
                            }
                        }

                        if (selectedLayout.generatePicture.SetFalse())
                        {
                            if (livedDocument.LayoutTex.TryGetValue(selectedLayout.guid, out var tiledTexture1)) tiledTexture1.Dispose();
                            var tiledTexture2 = new TiledTexture(PaintingTexture);
                            livedDocument.LayoutTex[selectedLayout.guid] = tiledTexture2;
                        }

                        blendMode?.Blend(PaintingTexture, RenderTarget, buffer, ofs, 256);
                    }
                    else if (tiledTexture != null && tiledTexture.tilesCount != 0)
                    {
                        blendMode?.Blend(tiledTexture, RenderTarget, buffer, ofs, 256);
                    }
                }
                ofs += 256;
            }
        }

        //public void RenderPart(List<Int2> part, TileRect filterRect)
        //{
        //    if (part == null || part.Count == 0) return;
        //    PrepareRenderData();
        //    for (int i = 0; i < RenderTarget.Count; i++)
        //    {
        //        //清除部分
        //        int partCount = part.Count;
        //        ComputeShader texturePartClear = TiledTexture.TexturePartClear;
        //        ComputeBuffer buf = new ComputeBuffer(RenderTarget[0].GetDeviceResources(), partCount, 8, part.ToArray());
        //        texturePartClear.SetSRV(buf, 0);
        //        texturePartClear.SetUAV(RenderTarget[i], 0);
        //        texturePartClear.Dispatch(1, 1, (partCount + 15) / 16);
        //        buf.Dispose();
        //    }
        //    int ofs = 0;
        //    for (int i = ManagedLayout.Count - 1; i >= 0; i--)
        //    {
        //        PictureLayout selectedLayout = ManagedLayout[i];
        //        if (ManagedLayout[i].Hidden)
        //        {
        //            ofs += 256;
        //            continue;
        //        }
        //        //if (selectedLayout is StandardLayout standardLayout)
        //        //{
        //        if (CanvasCase.blendmodesMap.TryGetValue(selectedLayout.BlendMode, out var blendMode))
        //        {
        //            CanvasCase.LayoutTex.TryGetValue(selectedLayout.guid, out var tiledTexture);
        //            if (selectedLayout.DataSource == PictureDataSource.Color)
        //            {
        //                blendMode?.BlendColor(RenderTarget[0], part, constantBuffer1, ofs, 256);
        //            }
        //            else if (CanvasCase.PaintAgent.CurrentLayout == selectedLayout)
        //            {
        //                blendMode?.Blend(PaintingTexture, RenderTarget[0], part, constantBuffer1, ofs, 256);
        //            }
        //            else if (tiledTexture != null && tiledTexture.tileRect.HaveIntersections(filterRect))
        //            {
        //                blendMode?.Blend(tiledTexture, RenderTarget[0], part, constantBuffer1, ofs, 256);
        //            }
        //        }
        //    }
        //}

        void PrepareRenderData()
        {
            var writer = constantBuffer1.Begin();
            int ofs = 0;
            for (int i = ManagedLayout.Count - 1; i >= 0; i--)
            {
                writer.Seek(ofs, SeekOrigin.Begin);
                Vector4 color = ManagedLayout[i].Color;
                writer.Write(color);

                GetData(ManagedLayout[i], writer);
                ofs += 256;
            }
        }

        public void GetData(PictureLayout layout, BinaryWriterPlus writer)
        {
            if (livedDocument.blendmodesMap.TryGetValue(layout.BlendMode, out var blendMode) && blendMode.Paramerters != null)
            {
                for (int i = 0; i < blendMode.Paramerters.Length; i++)
                {
                    if (layout.parameters.TryGetValue(blendMode.Paramerters[i].Name, out var value))
                    {
                        writer.Write((float)value.X);
                    }
                    else
                    {
                        writer.Write(0.0f);
                    }
                }
            }
        }

        RenderTexture RenderTarget { get { return livedDocument.Output; } }
        RenderTexture PaintingTexture { get { return livedDocument.PaintingTexture; } }
        DeviceResources DeviceResources { get { return livedDocument.DeviceResources; } }
        IReadOnlyList<PictureLayout> ManagedLayout { get { return livedDocument.Layouts; } }

        public readonly LivedNekoPainterDocument livedDocument;

        StreamedBuffer constantBuffer1 = new StreamedBuffer();

        StreamedBuffer brushDataBuffer = new StreamedBuffer();
        StreamedBuffer tilePosDataBuffer = new StreamedBuffer();

        public void ExecuteNodes(Graph graph, List<int> executeOrder)
        {
            GC(graph);
            foreach (int nodeId in executeOrder)
            {
                var node = graph.Nodes[nodeId];
                if (node.strokeNode != null)
                {
                    if (graph.NodeParamCaches == null)
                        graph.NodeParamCaches = new Dictionary<int, NodeParamCache>();
                    var cache = graph.NodeParamCaches.GetOrCreate(node.Luid);
                    cache.outputCache["strokes"] = new Stroke[] { node.strokeNode.stroke };
                    cache.modification = node.strokeNode.stroke.modification;
                }
                else if (node.paint2DNode != null || node.scriptNode != null)
                {
                    //测试
                    var paint2DNode = graph.Nodes[nodeId].paint2DNode;

                    var nodeDef = livedDocument.scriptNodeDefs[node.GetNodeTypeName()];

                    ScriptGlobal global = new ScriptGlobal { parameters = new Dictionary<string, object>() };
                    if (graph.NodeParamCaches == null)
                        graph.NodeParamCaches = new Dictionary<int, NodeParamCache>();
                    var cache = graph.NodeParamCaches.GetOrCreate(nodeId);

                    bool generateCache = false;
                    if (node.Inputs != null)
                    {
                        if (node.Inputs.Count == cache.inputNodeModification.Count)
                            foreach (var input in node.Inputs)
                            {
                                var inputNode = graph.Nodes[input.Value.targetUid];
                                var inputNodeCache = graph.NodeParamCaches.GetOrCreate(inputNode.Luid);
                                if (!cache.inputNodeModification.TryGetValue(input.Value.targetSocket, out var modifiaction1) || modifiaction1 != new Int2(input.Value.targetUid, inputNodeCache.modification))
                                {
                                    generateCache = true;
                                    cache.inputNodeModification[input.Value.targetSocket] = new Int2(input.Value.targetUid, inputNodeCache.modification);
                                }
                            }
                        else
                        {
                            generateCache = true;
                            cache.inputNodeModification.Clear();
                            foreach (var input in node.Inputs)
                            {
                                var inputNode = graph.Nodes[input.Value.targetUid];
                                var inputNodeCache = graph.NodeParamCaches.GetOrCreate(inputNode.Luid);
                                cache.inputNodeModification[input.Value.targetSocket] = new Int2(input.Value.targetUid, inputNodeCache.modification);
                            }
                        }
                    }
                    else
                        generateCache = false;

                    if (generateCache)
                    {
                        //获取输入
                        if (node.Inputs != null)
                            foreach (var input in node.Inputs)
                            {
                                var inputNode = graph.Nodes[input.Value.targetUid];
                                var inputNodeCache = graph.NodeParamCaches.GetOrCreate(inputNode.Luid);
                                if (inputNodeCache.outputCache.TryGetValue(input.Value.targetSocket, out object obj1))
                                {
                                    if (obj1 is TiledTexture tex1)
                                    {
                                        PaintingTexture.Clear();
                                        tex1.UnzipToTexture(PaintingTexture);
                                        global.parameters[input.Key] = new Texture2D { _texture = PaintingTexture, width = PaintingTexture.width, height = PaintingTexture.height };
                                    }
                                    else
                                    {
                                        global.parameters[input.Key] = obj1;
                                    }
                                }
                            }
                        //检查null输入
                        foreach (var ioDef in nodeDef.ioDefs)
                        {
                            if (ioDef.type == "texture2D" && ioDef.ioType == "input")
                            {
                                if (!global.parameters.ContainsKey(ioDef.name))
                                {
                                    PaintingTexture.Clear();
                                    global.parameters[ioDef.name] = new Texture2D { _texture = PaintingTexture, width = PaintingTexture.width, height = PaintingTexture.height };

                                }
                                else
                                {

                                }
                            }
                        }
                        if (node.paint2DNode != null)
                        {
                            if (global.parameters.ContainsKey("strokes"))
                                PaintToTexture(((Texture2D)global.parameters["texture2D"])._texture, paint2DNode, ((IList<Stroke>)global.parameters["strokes"])[0]);
                        }
                        else
                        {
                            string path = nodeDef.path;
                            var script = livedDocument.scriptCache.GetOrCreate(path, () =>
                            {
                                ScriptOptions options = ScriptOptions.Default.WithReferences(typeof(Texture2D).Assembly).WithImports("NekoPainter.Data");

                                return CSharpScript.Create(livedDocument.scripts[path], options, typeof(ScriptGlobal));
                            });
                            var state = script.RunAsync(global).Result;
                        }
                        //缓存输出
                        foreach (var ioDef in nodeDef.ioDefs)
                        {
                            if (ioDef.type == "texture2D" && ioDef.ioType == "output" && global.parameters.ContainsKey(ioDef.name))
                            {
                                Texture2D tex = (Texture2D)global.parameters[ioDef.name];
                                if (cache.outputCache.TryGetValue(ioDef.name, out var _tex1))
                                {
                                    ((TiledTexture)_tex1).Dispose();
                                }
                                cache.outputCache[ioDef.name] = new TiledTexture(tex._texture);
                            }
                        }
                        cache.modification++;
                    }
                }
            }
        }

        HashSet<int> gcRemoveNode = new HashSet<int>();
        public void GC(Graph graph)
        {
            if (graph.NodeParamCaches == null) return;
            gcRemoveNode.Clear();
            foreach (var cache in graph.NodeParamCaches)
            {
                if (!graph.Nodes.ContainsKey(cache.Key))
                {
                    gcRemoveNode.Add(cache.Key);
                    foreach (var cache1 in cache.Value.outputCache)
                    {
                        if (cache1.Value is TiledTexture t1)
                        {
                            t1.Dispose();
                        }
                    }
                }
            }
            foreach (var key in gcRemoveNode)
                graph.NodeParamCaches.Remove(key);
        }

        public void PaintToTexture(RenderTexture texture, Paint2DNode paint2DNode, Stroke stroke)
        {
            var brush = livedDocument.brushes[paint2DNode.brushPath];
            var positions = stroke.position;
            int _width = texture.width;
            int _height = texture.height;
            for (int i = 1; i < positions.Count; i++)
            {
                brush.CheckBrush(DeviceResources);
                UpdateBrushData2(paint2DNode, stroke, i);
                ComputeBrush(brush.shader, texture, GetPaintingTiles(positions[(i == 0) ? 0 : (i - 1)], positions[i], paint2DNode.size, _width, _height));
                brushDataBuffer.writer.Seek(0, SeekOrigin.Begin);
            }
        }

        void UpdateBrushData2(Paint2DNode paint2DNode, Stroke stroke, int index)
        {
            var brushDataWriter = brushDataBuffer.Begin();
            brushDataWriter.Write(paint2DNode.color);
            brushDataWriter.Write(paint2DNode.color2);
            brushDataWriter.Write(paint2DNode.color3);
            brushDataWriter.Write(paint2DNode.color4);
            brushDataWriter.Write(paint2DNode.size);
            brushDataWriter.Write(new Vector3());
            int k = index;
            for (int i = 0; i < 4; i++)
            {
                PointerData pointerData = new PointerData();
                if (k >= 0 && k < stroke.position.Count)
                {
                    pointerData.Position = stroke.position[k];
                    pointerData.Pressure = stroke.presure[k];
                }
                k--;
                brushDataWriter.Write(pointerData);
            }
            var brush = livedDocument.brushes[paint2DNode.brushPath];
            if (brush.Parameters != null)
                for (int i = 0; i < brush.Parameters.Length; i++)
                {
                    if (brush.Parameters[i].IsFloat)
                        brushDataWriter.Write((float)brush.Parameters[i].Value);
                    else
                        brushDataWriter.Write((int)brush.Parameters[i].Value);
                }
            else
            {

            }
        }

        List<Int2> inRangeTiles = new List<Int2>();
        List<Int2> GetPaintingTiles(Vector2 start, Vector2 end, float BrushSize, int width, int height)
        {
            inRangeTiles.Clear();
            int minx = Math.Max((int)MathF.Min(start.X - BrushSize, end.X - BrushSize), 0);
            int miny = Math.Max((int)MathF.Min(start.Y - BrushSize, end.Y - BrushSize), 0);
            minx &= -32;
            miny &= -32;
            int maxx = Math.Min((int)MathF.Max(start.X + BrushSize, end.X + BrushSize), width);
            int maxy = Math.Min((int)MathF.Max(start.Y + BrushSize, end.Y + BrushSize), height);

            Vector2 NS2E = start - end;
            Vector2 OSS2 = new Vector2(4.0f, 4.0f) - start;
            Vector2 normalizedRS2E = Vector2.Normalize(new Vector2(NS2E.Y, -NS2E.X));

            float sRange2 = BrushSize + 6f;//6大于4*sqrt2=5.656854

            for (int x = minx; x < maxx; x += 8)
                for (int y = miny; y < maxy; y += 8)
                {
                    if (MathF.Abs(Vector2.Dot(new Vector2(x, y) + OSS2, normalizedRS2E)) > sRange2) continue;
                    Int2 vx = new Int2(x, y);
                    inRangeTiles.Add(vx);
                }
            return inRangeTiles;
        }

        void ComputeBrush(ComputeShader computeShader, RenderTexture texture, List<Int2> tilesCovered)
        {
            if (tilesCovered.Count <= 0) return;
            var writer = tilePosDataBuffer.Begin();
            writer.Write(tilesCovered.ToArray());
            computeShader.SetSRV(tilePosDataBuffer.GetComputeBuffer(DeviceResources, 8), 0);
            computeShader.SetCBV(brushDataBuffer.GetBuffer(DeviceResources), 0);
            computeShader.SetUAV(texture, 0);

            computeShader.Dispatch(1, 1, tilesCovered.Count);
        }
    }

    public class ScriptGlobal
    {
        public Dictionary<string, object> parameters;
        public NodeContext context;
    }
}

