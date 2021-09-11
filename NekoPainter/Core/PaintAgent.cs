﻿using NekoPainter.Core;
using System;
using System.Numerics;
using System.Collections;
using System.Collections.Generic;
using CanvasRendering;
using Color = System.Numerics.Vector4;
using System.Runtime.InteropServices;
using System.ComponentModel;
using System.Collections.ObjectModel;
using System.Collections.Concurrent;
using NekoPainter.UI;
using NekoPainter.Util;
using System.IO;

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
            MemoryStream memoryStream = new MemoryStream(brushData1);
            brushDataWriter = new BinaryWriterPlus(memoryStream);
        }

        public void SetPaintTarget(RenderTexture target, RenderTexture targetBackup)
        {
            PaintingTexture = target;
            PaintingTextureBackup = targetBackup;
            _width = PaintingTexture.width;
            _height = PaintingTexture.height;

            int pTilesX = (_width + 31) / 32;
            int pTilesY = (_height + 31) / 32;
            paintTilesBuffer?.Dispose();
            paintTilesBuffer = new ComputeBuffer(document.DeviceResources, pTilesX * pTilesY, 8);
            brushDataBuffer?.Dispose();
            brushDataBuffer = new ConstantBuffer(document.DeviceResources, brushData1.Length);
            _mapForUndoStride = (_width + 7) / 8 + 4;
            _mapForUndoCount = _mapForUndoStride * (((_height + 7) / 8) + 4);
            mapForUndo = new BitArray(_mapForUndoCount);
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

        public bool DrawBegin(PenInputData penInputData)
        {
            if (CurrentLayout == null || currentBrush == null) { return false; }
            inputPointerDatas.Enqueue(new InputPointerData { PointerData = GetBrushData(penInputData.point, penInputData.pointerPoint), KeyDown = true });
            return true;
        }
        public bool Draw(PenInputData penInputData)
        {
            if (CurrentLayout == null || currentBrush == null) { return false; }
            inputPointerDatas.Enqueue(new InputPointerData { PointerData = GetBrushData(penInputData.point, penInputData.pointerPoint) });
            return true;
        }
        public bool DrawEnd(PenInputData penInputData)
        {
            if (CurrentLayout == null || currentBrush == null) { return false; }
            inputPointerDatas.Enqueue(new InputPointerData { PointerData = GetBrushData(penInputData.point, penInputData.pointerPoint), keyUp = true });
            return true;
        }

        public void Process()
        {
            while (inputPointerDatas.TryDequeue(out var inputPointerData))
            {
                Vector2 position = inputPointerData.PointerData.Position;
                if (inputPointerData.KeyDown)
                {
                    drawPrevPos = position;
                    FillPointerData(inputPointerData);
                }
                UpdateBrushData2(inputPointerData);
                CurrentLayout.saved = false;
                List<Int2> tilesCovered = GetPaintingTiles(drawPrevPos, position, out TileRect coveredRect);
                currentBrush.CheckBrush(document.DeviceResources);
                if (tilesCovered.Count != 0)
                {
                    if (inputPointerData.KeyDown)
                        ComputeBrush(currentBrush.cBegin, tilesCovered);
                    else if (inputPointerData.keyUp)
                        ComputeBrush(currentBrush.cEnd, tilesCovered);
                    else
                        ComputeBrush(currentBrush.cDoing, tilesCovered);
                }
                if (inputPointerData.keyUp)
                {
                    List<Int2> paintCoveredTiles = new List<Int2>();
                    for (int i = 0; i < _mapForUndoCount; i++)
                    {
                        if (mapForUndo[i])
                        {
                            paintCoveredTiles.Add(new Int2((i % _mapForUndoStride) * 8, (i / _mapForUndoStride) * 8));
                        }
                    }
                    if (paintCoveredTiles.Count != 0)
                        UndoManager.AddUndoData(new Undo.CMD_TileReplace(CurrentLayout, new TiledTexture(PaintingTextureBackup, paintCoveredTiles), document));
                    mapForUndo.SetAll(false);
                    PaintingTexture.CopyTo(PaintingTextureBackup);

                    if (document.LayoutTex.TryGetValue(CurrentLayout.guid, out var tiledTexture1)) tiledTexture1.Dispose();
                    var tiledTexture = new TiledTexture(PaintingTexture);
                    document.LayoutTex[CurrentLayout.guid] = tiledTexture;
                }

                //drawPrevPos = Vector2.Zero;
                drawPrevPos = position;
            }
        }

        public void Dispose()
        {
            brushDataBuffer?.Dispose();
            paintTilesBuffer?.Dispose();
        }

        byte[] brushData1 = new byte[64 * 8 + 208];

        BinaryWriterPlus brushDataWriter;

        ConstantBuffer brushDataBuffer;

        PointerData[] pointerDatas = new PointerData[8];

        void FillPointerData(InputPointerData inputPointerData)
        {
            for (int i = 0; i < pointerDatas.Length; i++)
            {
                pointerDatas[i] = inputPointerData.PointerData;
            }
        }

        /// <summary>
        /// 正在绘制的纹理。
        /// </summary>
        public RenderTexture PaintingTexture;
        /// <summary>
        /// 绘制的纹理的备份，用于支持撤销重做。
        /// </summary>
        public RenderTexture PaintingTextureBackup;

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

        private ComputeBuffer paintTilesBuffer;

        public UndoManager UndoManager;

        public List<Brush> brushes;

        int _mapForUndoStride;
        int _mapForUndoCount;
        BitArray mapForUndo;
        List<Int2> inRangeTiles = new List<Int2>(2048);
        List<Int2> GetPaintingTiles(Vector2 start, Vector2 end, out TileRect rect)
        {
            inRangeTiles.Clear();
            int minx = Math.Max((int)MathF.Min(start.X - BrushSize, end.X - BrushSize), 0);
            int miny = Math.Max((int)MathF.Min(start.Y - BrushSize, end.Y - BrushSize), 0);
            minx &= -32;
            miny &= -32;
            int maxx = Math.Min((int)MathF.Max(start.X + BrushSize, end.X + BrushSize), _width);
            int maxy = Math.Min((int)MathF.Max(start.Y + BrushSize, end.Y + BrushSize), _height);

            rect = new TileRect() { minX = minx, minY = miny, maxX = maxx + 32, maxY = maxy + 32 };

            Vector2 NS2E = start - end;
            Vector2 OSS2 = new Vector2(4.0f, 4.0f) - start;
            Vector2 normalizedRS2E = Vector2.Normalize(new Vector2(NS2E.Y, -NS2E.X));

            float sRange2 = BrushSize + 6f;//6大于4*sqrt2=5.656854

            for (int x = minx; x < maxx; x += 8)
                for (int y = miny; y < maxy; y += 8)
                {
                    if (MathF.Abs(Vector2.Dot(new Vector2(x, y) + OSS2, normalizedRS2E)) > sRange2) continue;
                    Int2 vx = new Int2(x, y);
                    mapForUndo[(vx.X / 8) % _mapForUndoStride + vx.Y / 8 * _mapForUndoStride] = true;
                    inRangeTiles.Add(vx);
                }
            return inRangeTiles;
        }

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

        void UpdateBrushData2(InputPointerData inputPointerData)
        {
            brushDataWriter.Seek(0, SeekOrigin.Begin);
            brushDataWriter.Write(_color);
            brushDataWriter.Write(_color2);
            brushDataWriter.Write(_color3);
            brushDataWriter.Write(_color4);
            brushDataWriter.Write(BrushSize);
            brushDataWriter.Write(new Vector3());

            int ofs = 80;

            for (int i = pointerDatas.Length - 1; i >= 1; i--)
            {
                pointerDatas[i] = pointerDatas[i - 1];
            }
            pointerDatas[0] = inputPointerData.PointerData;
            MemoryMarshal.Cast<PointerData, byte>(pointerDatas).CopyTo(new Span<byte>(brushData1, ofs, 512));
            ofs += 512;
            brushDataWriter.Seek(592, SeekOrigin.Begin);

            if (currentBrush.Parameters != null)
                for (int i = 0; i < currentBrush.Parameters.Length; i++)
                {
                    if (currentBrush.Parameters[i].IsFloat)
                        brushDataWriter.Write((float)currentBrush.Parameters[i].Value);
                    else
                        brushDataWriter.Write((int)currentBrush.Parameters[i].Value);
                }
            else
            {

            }

            brushDataBuffer.UpdateResource<byte>(brushData1);

        }
        void ComputeBrush(ComputeShader c, List<Int2> tilesCovered)
        {
            ComputeBuffer tilesPos = new ComputeBuffer(PaintingTexture.GetDeviceResources(), tilesCovered.Count, 8, tilesCovered.ToArray());
            c.SetSRV(tilesPos, 0);
            c.SetCBV(brushDataBuffer, 0);
            c.SetUAV(PaintingTexture, 0);

            c.Dispatch(1, 1, tilesCovered.Count);
            tilesPos.Dispose();
        }

        int _width;
        int _height;

        public bool UseSelection = false;
        public struct InputPointerData
        {
            public PointerData PointerData;
            public bool KeyDown;
            public bool keyUp;
        }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi, Pack = 1)]
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
}