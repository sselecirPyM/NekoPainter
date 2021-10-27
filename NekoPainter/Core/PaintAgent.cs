using NekoPainter.Core;
using System;
using System.Numerics;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Concurrent;
using NekoPainter.UI;
using System.IO;
using NekoPainter.Core.Nodes;
using NekoPainter.Core.UndoCommand;
using NekoPainter.Data;

namespace NekoPainter
{
    public class PaintAgent
    {
        /// <summary>
        /// 设置当前使用的笔刷。
        /// </summary>
        public void SetBrush(Brush brush)
        {
            currentBrush = brush;
        }

        public bool Draw(PenInputData penInputData)
        {
            if (CurrentLayout == null || currentBrush == null) { return false; }
            inputPointerDatas.Enqueue(new InputPointerData { PointerData = GetBrushData(penInputData.point, penInputData.pointerPoint), penInputFlag = penInputData.penInputFlag });
            return true;
        }
        Stroke stroke;
        Node strokeNode1;
        System.Diagnostics.Stopwatch stopwatch = System.Diagnostics.Stopwatch.StartNew();
        double previousStopWatchValue;
        public void Process()
        {
            while (inputPointerDatas.TryDequeue(out var inputPointerData))
            {
                Vector2 position = inputPointerData.PointerData.Position;
                if (inputPointerData.penInputFlag == PenInputFlag.Begin)
                {
                    stroke = new Stroke()
                    {
                        position = new List<Vector2>(),
                        deltaTime = new List<float>(),
                        presure = new List<float>(),
                        startTime = DateTime.Now
                    };
                    stroke.deltaTime.Add(0);
                    if (CurrentLayout.graph == null)
                    {
                        CurrentLayout.graph = new Graph();
                        CurrentLayout.graph.Initialize();
                    }
                    var graph = CurrentLayout.graph;

                    int startOutput = graph.outputNode;
                    int startIndex = graph.idAllocated;
                    Node lastStrokeNode = null;
                    var drawMode1 = drawMode;
                    if (drawMode == DrawMode.Append)
                    {
                        lastStrokeNode = graph.GetLastNode("strokeNode");
                        if (lastStrokeNode == null) drawMode1 = DrawMode.None;
                    }
                    if (drawMode1 == DrawMode.None)
                    {
                        foreach (var node in currentBrush.nodes)
                        {
                            if (node.name == "stroke")
                            {
                                StrokeNode strokeNode = new StrokeNode();
                                strokeNode.strokes = new List<Stroke> { stroke };
                                var strokeNode1 = new Node { strokeNode = strokeNode };
                                strokeNode1.creationTime = DateTime.Now;
                                graph.AddNodeToEnd(strokeNode1, node.offset);
                            }
                            else
                            {
                                ScriptNode scriptNode = new ScriptNode();
                                scriptNode.nodeName = node.name;
                                strokeNode1 = new Node { scriptNode = scriptNode };
                                strokeNode1.creationTime = DateTime.Now;
                                if (node.parameters != null)
                                    foreach (var param in node.parameters)
                                    {
                                        var param1 = currentBrush.parameters.Find(u => u.name == param.from);
                                        if (param1.type == "float")
                                        {
                                            (strokeNode1.fParams ??= new Dictionary<string, float>())[param.name] = (float)(param1.defaultValue1 ?? 0.0f);
                                        }
                                        if (param1.type == "float2")
                                        {
                                            (strokeNode1.f2Params ??= new Dictionary<string, Vector2>())[param.name] = (Vector2)(param1.defaultValue1 ?? new Vector2());
                                        }
                                        if (param1.type == "float3" || param1.type == "color3")
                                        {
                                            (strokeNode1.f3Params ??= new Dictionary<string, Vector3>())[param.name] = (Vector3)(param1.defaultValue1 ?? new Vector3());
                                        }
                                        if (param1.type == "float4" || param1.type == "color4")
                                        {
                                            (strokeNode1.f4Params ??= new Dictionary<string, Vector4>())[param.name] = (Vector4)(param1.defaultValue1 ?? new Vector4());
                                        }
                                    }
                                graph.AddNodeToEnd(strokeNode1, node.offset);
                            }
                        }
                        foreach (var link in currentBrush.links)
                        {
                            graph.Link(link.outputNode + startIndex, link.outputName, link.inputNode + startIndex, link.inputName);
                        }
                        foreach (var link in currentBrush.attachLinks)
                        {
                            if (graph.Nodes.ContainsKey(graph.outputNode))
                                graph.Link(startOutput, link.outputName, link.inputNode + startIndex, link.inputName);
                        }
                        List<int> removeNodeList = new List<int>();
                        for (int i = 0; i < currentBrush.nodes.Count; i++)
                        {
                            removeNodeList.Add(startIndex + i);
                        }
                        var removeNode = new CMD_Remove_RecoverNodes();
                        removeNode.BuildRemoveNodes(document, CurrentLayout.graph, removeNodeList, CurrentLayout.guid);
                        UndoManager.AddUndoData(removeNode);
                        graph.outputNode = currentBrush.outputNode + startIndex;
                    }
                    else
                    {
                        UndoManager.AddUndoData(new CMD_Remove_AddStroke(graph, lastStrokeNode, null, lastStrokeNode.strokeNode.strokes.Count));
                        lastStrokeNode.strokeNode.strokes.Add(stroke);
                        strokeNode1 = lastStrokeNode;
                    }
                    previousStopWatchValue = stopwatch.ElapsedTicks / 1e7;
                }
                else
                {
                    double current = stopwatch.ElapsedTicks / 1e7;
                    stroke.deltaTime.Add((float)(current - previousStopWatchValue));
                    previousStopWatchValue = current;//not correct
                }
                stroke.position.Add(position);
                stroke.presure.Add(inputPointerData.PointerData.Pressure);
                CurrentLayout.graph.SetNodeCacheInvalid(strokeNode1.Luid);

                if (inputPointerData.penInputFlag == PenInputFlag.End)
                {
                    stroke = null;
                    strokeNode1 = null;
                }

                CurrentLayout.saved = false;

                if (inputPointerData.penInputFlag == PenInputFlag.End)
                {
                    CurrentLayout.generateCache = true;
                }
            }
        }

        public DrawMode drawMode;
        public LivedNekoPainterDocument document;
        /// <summary>
        /// 当前要画的图层
        /// </summary>
        public PictureLayout CurrentLayout { get; set; }

        /// <summary>
        /// 当前使用的笔刷
        /// </summary>
        public Brush currentBrush { get; set; }

        public ConcurrentQueue<InputPointerData> inputPointerDatas = new ConcurrentQueue<InputPointerData>();

        public UndoManager UndoManager;

        public List<Brush> brushes;

        public static PointerData GetBrushData(Vector2 position, NekoPainter.Core.PointerPoint pointerPoint)
        {
            PointerData pointerData = new PointerData()
            {

                Position = position,
                PositionEx = new Vector2(0, 1),
                //XYTilt = new Vector2(pointerPoint.Properties.XTilt, pointerPoint.Properties.YTilt),
                //Twist = pointerPoint.Properties.Twist,
                //Pressure = pointerPoint.Properties.Pressure,
                //Orientation = pointerPoint.Properties.Orientation,
                //ZDistance = (pointerPoint.Properties.ZDistance == null) ? -1.0f : (float)pointerPoint.Properties.ZDistance
                XYTilt = new Vector2(0, 0),
                Twist = 0,
                Pressure = 0.5f,
                Orientation = 0,
                ZDistance = -1
            };
            if (pointerPoint != null)
            {
                pointerData.FrameId = pointerPoint.FrameId;
                pointerData.PointerId = pointerPoint.PointerId;
                pointerData.Timestamp = pointerPoint.Timestamp;
            }
            return pointerData;
        }
    }
    public struct InputPointerData
    {
        public PointerData PointerData;
        public PenInputFlag penInputFlag;
    }

    public struct PointerData
    {
        public uint FrameId;
        public uint PointerId;
        public ulong Timestamp;
        public Vector2 Position;
        public Vector2 PositionEx;
        public Vector2 XYTilt;
        public float Twist;
        public float Pressure;
        public float Orientation;
        public float ZDistance;
        public Vector2 Preserved;
    }
    public enum DrawMode
    {
        None = 0,
        Append = 1,
    }
}
