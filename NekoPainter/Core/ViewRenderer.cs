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
                                executeOrder = ExecuteAll(selectedLayout.graph);
                                ExecuteNodes(selectedLayout.graph, executeOrder);
                            }
                        }
                        if (livedDocument.LayoutTex.TryGetValue(selectedLayout.guid, out var tiledTexture1)) tiledTexture1.Dispose();
                        var tiledTexture2 = new TiledTexture(PaintingTexture);
                        livedDocument.LayoutTex[selectedLayout.guid] = tiledTexture2;

                        blendMode?.Blend(PaintingTexture, RenderTarget, buffer, ofs, 256);
                    }
                    else if (livedDocument.PaintAgent.CurrentLayout == selectedLayout)
                    {
                        List<int> executeOrder;
                        if (selectedLayout.graph != null)
                        {
                            PaintingTexture.Clear();
                            if (selectedLayout.graph.Nodes.Count > 0)
                            {
                                executeOrder = ExecuteAll(selectedLayout.graph);
                                ExecuteNodes(selectedLayout.graph, executeOrder);
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

        StreamedConstantBuffer constantBuffer1 = new StreamedConstantBuffer();


        StreamedConstantBuffer brushDataBuffer = new StreamedConstantBuffer();

        List<int> ExecuteAll(Graph graph)
        {
            var allNode = graph.GetInputChainSet(graph.outputNode);
            HashSet<int> executed = new HashSet<int>();
            List<int> executeOrder = new List<int>();
            executeOrder.Capacity = allNode.Count + 1;
            int c = 0;
            while (allNode.Count > 0)
            {
                foreach (int nodeId in allNode)
                {
                    var node = graph.Nodes[nodeId];
                    if (node.Inputs == null || node.Inputs.All(u => executed.Contains(u.Value.targetUid)))
                    {
                        executed.Add(nodeId);
                        executeOrder.Add(nodeId);
                        continue;
                    }
                }
                for (; c < executeOrder.Count; c++)
                {
                    allNode.Remove(executeOrder[c]);
                }
            }
            if (graph.Nodes.ContainsKey(graph.outputNode))
                executeOrder.Add(graph.outputNode);
            return executeOrder;
        }

        public void ExecuteNodes(Graph graph, List<int> executeOrder)
        {
            foreach (int nodeId in executeOrder)
            {
                var node = graph.Nodes[nodeId];
                if (node.paint2DNode != null)
                {
                    var paint2DNode = graph.Nodes[nodeId].paint2DNode;
                    StrokeNode strokeNode = graph.Nodes[node.Inputs["stroke"].targetUid].strokeNode;
                    PaintToTexture(PaintingTexture, paint2DNode, strokeNode);
                }
            }
        }

        public void PaintToTexture(RenderTexture texture, Paint2DNode paint2DNode, StrokeNode strokeNode)
        {
            var brush = livedDocument.brushes[paint2DNode.brushPath];
            var positions = strokeNode.stroke.position;
            int _width = PaintingTexture.width;
            int _height = PaintingTexture.height;
            for (int i = 1; i < positions.Count; i++)
            {
                brush.CheckBrush(DeviceResources);
                UpdateBrushData2(paint2DNode, strokeNode, i);
                ComputeBrush(brush.shader, texture, GetPaintingTiles(positions[(i == 0) ? 0 : (i - 1)], positions[i], paint2DNode.size, _width, _height));
            }
        }

        void UpdateBrushData2(Paint2DNode paint2DNode, StrokeNode strokeNode, int index)
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
                if (k >= 0 && k < strokeNode.stroke.position.Count)
                {
                    pointerData.Position = strokeNode.stroke.position[k];
                    pointerData.Pressure = strokeNode.stroke.presure[k];
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
        List<Int2> GetPaintingTiles(Vector2 start, Vector2 end, float BrushSize, int width, int height)
        {
            List<Int2> inRangeTiles = new List<Int2>();
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
            ComputeBuffer tilesPos = new ComputeBuffer(texture.GetDevice(), tilesCovered.Count, 8, tilesCovered.ToArray());
            computeShader.SetSRV(tilesPos, 0);
            computeShader.SetCBV(brushDataBuffer.GetBuffer(DeviceResources), 0);
            computeShader.SetUAV(texture, 0);

            computeShader.Dispatch(1, 1, tilesCovered.Count);
            tilesPos.Dispose();
        }
    }
}

