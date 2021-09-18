using NekoPainter.Core;
using System;
using System.Numerics;
using System.Collections;
using System.Collections.Generic;
using CanvasRendering;
using Color = System.Numerics.Vector4;
using System.Runtime.InteropServices;
using System.Collections.Concurrent;
using NekoPainter.UI;
using NekoPainter.Util;
using System.IO;
using NekoPainter.Nodes;
using NekoPainter.Core.UndoCommand;

namespace NekoPainter
{
    public class PaintAgent : IDisposable
    {
        /// <summary>
        /// 创建一个新的绘画代理。
        /// </summary>
        public PaintAgent(LivedNekoPainterDocument document)
        {
            this.document = document;
        }

        public void SetPaintTarget(RenderTexture target)
        {
            PaintingTexture = target;
        }
        /// <summary>
        /// 设置当前使用的笔刷。
        /// </summary>
        public void SetBrush(Brush brush)
        {
            if (currentBrush != null)
                currentBrush.Size = BrushSize;
            currentBrush = brush;
            currentBrush.CheckBrush(document.DeviceResources);
            BrushSize = currentBrush.Size;
        }

        public bool Draw(PenInputData penInputData)
        {
            if (CurrentLayout == null || currentBrush == null) { return false; }
            inputPointerDatas.Enqueue(new InputPointerData { PointerData = GetBrushData(penInputData.point, penInputData.pointerPoint), penInputFlag = penInputData.penInputFlag });
            return true;
        }
        Stroke stroke;
        System.Diagnostics.Stopwatch stopwatch = System.Diagnostics.Stopwatch.StartNew();
        double previousStopWatchValue;
        public void Process()
        {
            while (inputPointerDatas.TryDequeue(out var inputPointerData))
            {
                Vector2 position = inputPointerData.PointerData.Position;
                if (inputPointerData.penInputFlag == PenInputFlag.Begin)
                {
                    drawPrevPos = position;
                    stroke = new Stroke()
                    {
                        position = new List<Vector2>(),
                        deltaTime = new List<float>(),
                        presure = new List<float>(),
                        startTime = DateTime.Now
                    };
                    if (CurrentLayout.graph == null)
                    {
                        CurrentLayout.graph = new Graph();
                        CurrentLayout.graph.Initialize();
                    }
                    var graph = CurrentLayout.graph;
                    Paint2DNode paint2dNode = new Paint2DNode();
                    paint2dNode.color = _color;
                    paint2dNode.color2 = _color2;
                    paint2dNode.color3 = _color3;
                    paint2dNode.color4 = _color4;
                    paint2dNode.size = BrushSize;
                    paint2dNode.brushPath = currentBrush.path;
                    var paint2dNode1 = new Node { paint2DNode = paint2dNode };
                    StrokeNode strokeNode = new StrokeNode();
                    strokeNode.stroke = stroke;
                    var strokeNode1 = new Node { strokeNode = strokeNode };
                    graph.AddNode(paint2dNode1);
                    graph.AddNode(strokeNode1);
                    graph.Link(strokeNode1, "context", paint2dNode1, "stroke");
                    if (graph.Nodes.ContainsKey(graph.outputNode))
                        graph.Link(graph.Nodes[graph.outputNode], "context", paint2dNode1, "context");

                    var removeNode = new CMD_Remove_RecoverNodes();
                    removeNode.graph = CurrentLayout.graph;
                    removeNode.removeNodes = new List<int>() { paint2dNode1.Luid, strokeNode1.Luid };
                    removeNode.setOutputNode = graph.outputNode;
                    removeNode.layoutGuid = CurrentLayout.guid;
                    removeNode.document = document;
                    UndoManager.AddUndoData(removeNode);

                    graph.outputNode = paint2dNode1.Luid;
                    stroke.deltaTime.Add(0);
                    previousStopWatchValue = stopwatch.ElapsedTicks / 1e7;
                }
                else
                {
                    double current = stopwatch.ElapsedTicks / 1e7;
                    stroke.deltaTime.Add((float)(current - previousStopWatchValue));
                    previousStopWatchValue = current;//not correct
                }
                stroke.position.Add(position);
                stroke.presure.Add(0.5f);
                if (inputPointerData.penInputFlag == PenInputFlag.End)
                {
                    document.Strokes.Add(stroke);

                    stroke = null;
                }

                CurrentLayout.saved = false;


                currentBrush.CheckBrush(document.DeviceResources);

                if (inputPointerData.penInputFlag == PenInputFlag.End)
                {
                    CurrentLayout.generatePicture = true;
                }

                //drawPrevPos = Vector2.Zero;
                drawPrevPos = position;
            }
        }

        public void Dispose()
        {

        }

        /// <summary>
        /// 正在绘制的纹理。
        /// </summary>
        public RenderTexture PaintingTexture;

        public LivedNekoPainterDocument document;
        /// <summary>
        /// 当前要画的图层
        /// </summary>
        public PictureLayout CurrentLayout { get; set; }

        /// <summary>
        /// 当前使用的笔刷
        /// </summary>
        public Brush currentBrush { get; set; }
        /// <summary>
        /// 笔刷尺寸
        /// </summary>
        public float BrushSize;

        public Color _color = new Color(1, 1, 1, 1);
        public Color _color2 = new Color(1, 0.5f, 0.5f, 1);
        public Color _color3 = new Color(1, 1, 1, 1);
        public Color _color4 = new Color(1, 1, 1, 1);

        public ConcurrentQueue<InputPointerData> inputPointerDatas = new ConcurrentQueue<InputPointerData>();

        private Vector2 drawPrevPos;

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

        public bool UseSelection = false;
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
}
