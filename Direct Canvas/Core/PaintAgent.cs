using DirectCanvas.Layout;
using System;
using System.Numerics;
using System.Collections;
using System.Collections.Generic;
using CanvasRendering;
using DirectCanvas.Core;
using Color = System.Numerics.Vector4;
using System.Runtime.InteropServices;
using System.ComponentModel;
using System.Collections.ObjectModel;
using System.Collections.Concurrent;

namespace DirectCanvas
{
    public class PaintAgent : IDisposable
    {
        /// <summary>
        /// 创建一个新的绘画代理。
        /// </summary>
        public PaintAgent(CanvasCase canvasCase)
        {
            CanvasCase = canvasCase;
        }

        public void SetPaintTarget(DeviceResources deviceResources, RenderTexture target, RenderTexture targetBackup)
        {
            PaintingTexture = target;
            PaintingTextureBackup = targetBackup;
            _width = PaintingTexture.width;
            _height = PaintingTexture.height;

            int pTilesX = (_width + 31) / 32;
            int pTilesY = (_height + 31) / 32;
            paintTilesBuffer?.Dispose();
            paintTilesBuffer = new ComputeBuffer(deviceResources, pTilesX * pTilesY, 8);
            brushDataBuffer?.Dispose();
            brushDataBuffer = new ConstantBuffer(deviceResources, 1136);
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
            BrushSize = currentBrush.Size;
            for (int i = 0; i < Brush.c_parameterCount; i++)
            {
                _parameters[i] = brush.Parameters[i];
            }
        }

        public bool DrawBegin(Vector2 position, Windows.UI.Input.PointerPoint pointerPoint)
        {
            if (CurrentLayout == null || currentBrush == null) { return false; }
            inputPointerDatas.Enqueue(new InputPointerData { PointerData = GetBrushData(position, pointerPoint), KeyDown = true });
            return true;
        }
        public bool Draw(Vector2 position, Windows.UI.Input.PointerPoint pointerPoint)
        {
            if (CurrentLayout == null || currentBrush == null) { return false; }
            inputPointerDatas.Enqueue(new InputPointerData { PointerData = GetBrushData(position, pointerPoint) });
            return true;
        }
        public bool DrawEnd(Vector2 position, Windows.UI.Input.PointerPoint pointerPoint)
        {
            if (CurrentLayout == null || currentBrush == null) { return false; }
            inputPointerDatas.Enqueue(new InputPointerData { PointerData = GetBrushData(position, pointerPoint), keyUp = true });
            return true;
        }

        public void Process()
        {
            while (inputPointerDatas.TryDequeue(out var inputPointerData))
            {
                UpdateBrushData2(inputPointerData);
                Vector2 position = inputPointerData.PointerData.Position;
                if (inputPointerData.KeyDown)
                {
                    drawPrevPos = position;
                    FillPointerData();
                    brushDataBuffer.UpdateResource<byte>(brushData1);
                }
                CurrentLayout.saved = false;
                List<Int2> tilesCovered = GetPaintingTiles(drawPrevPos, position, out TileRect coveredRect);
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
                        UndoManager.AddUndoData(new Undo.CMD_TileReplace(CurrentLayout, new TiledTexture(PaintingTextureBackup, paintCoveredTiles), CanvasCase));
                    mapForUndo.SetAll(false);
                    PaintingTexture.CopyTo(PaintingTextureBackup);

                    if (CanvasCase.LayoutTex.TryGetValue(CurrentLayout.guid, out var tiledTexture1)) tiledTexture1.Dispose();
                    var tiledTexture = new TiledTexture(PaintingTexture);
                    CanvasCase.LayoutTex[CurrentLayout.guid] = tiledTexture;
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

        byte[] brushData1 = new byte[64 * 16 + 208];

        ConstantBuffer brushDataBuffer;

        void FillPointerData()
        {
            for (int i = 1; i < 15; i++)
            {
                Array.Copy(brushData1, 208, brushData1, i * 64 + 208, 64);
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

        public CanvasCase CanvasCase;
        /// <summary>
        /// 当前要画的图层
        /// </summary>
        public StandardLayout CurrentLayout
        {
            get => _currentLayout; set
            {
                _currentLayout = value;
            }
        }
        StandardLayout _currentLayout;
        /// <summary>
        /// 当前使用的笔刷
        /// </summary>
        public Brush currentBrush { get; set; }
        /// <summary>
        /// 笔刷尺寸
        /// </summary>
        public float BrushSize
        {
            get { return _brushSize; }
            set
            {
                _brushSize = value;
            }
        }
        public float _brushSize;

        public Color _color = new Color(1, 1, 1, 1);
        public Color _color2 = new Color(1, 1, 1, 1);
        public Color _color3 = new Color(1, 1, 1, 1);
        public Color _color4 = new Color(1, 1, 1, 1);

        public ObservableCollection<DCParameter> _parameters = new ObservableCollection<DCParameter>(new DCParameter[Brush.c_parameterCount]);

        public ConcurrentQueue<InputPointerData> inputPointerDatas = new ConcurrentQueue<InputPointerData>();

        private Vector2 drawPrevPos;

        private ComputeBuffer paintTilesBuffer;

        public UndoManager UndoManager;

        public ViewRenderer ViewRenderer;

        public List<Brush> brushes;

        int _mapForUndoStride;
        int _mapForUndoCount;
        BitArray mapForUndo;
        List<Int2> inRangeTiles = new List<Int2>(2048);
        List<Int2> GetPaintingTiles(Vector2 start, Vector2 end, out TileRect rect)
        {
            inRangeTiles.Clear();
            int minx = Math.Max((int)MathF.Min(start.X - _brushSize, end.X - _brushSize), 0);
            int miny = Math.Max((int)MathF.Min(start.Y - _brushSize, end.Y - _brushSize), 0);
            minx &= -32;
            miny &= -32;
            int maxx = Math.Min((int)MathF.Max(start.X + _brushSize, end.X + _brushSize), _width);
            int maxy = Math.Min((int)MathF.Max(start.Y + _brushSize, end.Y + _brushSize), _height);

            rect = new TileRect() { minX = minx, minY = miny, maxX = maxx + 32, maxY = maxy + 32 };

            Vector2 NS2E = start - end;
            Vector2 OSS2 = new Vector2(4.0f, 4.0f) - start;
            Vector2 normalizedRS2E = Vector2.Normalize(new Vector2(NS2E.Y, -NS2E.X));

            float sRange2 = _brushSize + 6f;//6大于4*sqrt2=5.656854

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

        void Write1(Span<byte> target, Vector4 vec4, ref int ofs)
        {
            MemoryMarshal.Write(target, ref vec4);
            ofs += 16;
        }
        void Write1(Span<byte> target, int val, ref int ofs)
        {
            MemoryMarshal.Write(target, ref val);
            ofs += 4;
        }
        void Write1(Span<byte> target, float val, ref int ofs)
        {
            MemoryMarshal.Write(target, ref val);
            ofs += 4;
        }

        public static PointerData GetBrushData(Vector2 position, Windows.UI.Input.PointerPoint pointerPoint)
        {
            PointerData pointerData = new PointerData()
            {
                FrameId = pointerPoint.FrameId,
                PointerId = pointerPoint.PointerId,
                Timestamp = pointerPoint.Timestamp,
                Position = position,
                PositionEx = new Vector2(0, 1),
                XYTilt = new Vector2(pointerPoint.Properties.XTilt, pointerPoint.Properties.YTilt),
                Twist = pointerPoint.Properties.Twist,
                Pressure = pointerPoint.Properties.Pressure,
                Orientation = pointerPoint.Properties.Orientation,
                ZDistance = (pointerPoint.Properties.ZDistance == null) ? -1.0f : (float)pointerPoint.Properties.ZDistance
            };
            return pointerData;
        }

        void UpdateBrushData2(InputPointerData inputPointerData)
        {
            int ofs = 0;
            Write1(new Span<byte>(brushData1, ofs, 16), _color, ref ofs);
            Write1(new Span<byte>(brushData1, ofs, 16), _color2, ref ofs);
            Write1(new Span<byte>(brushData1, ofs, 16), _color3, ref ofs);
            Write1(new Span<byte>(brushData1, ofs, 16), _color4, ref ofs);
            for (int i = 0; i < Brush.c_parameterCount; i++)
            {
                if (currentBrush.Parameters[i].IsFloat)
                    Write1(new Span<byte>(brushData1, ofs, 4), currentBrush.Parameters[i]._fValue, ref ofs);
                else
                    Write1(new Span<byte>(brushData1, ofs, 4), currentBrush.Parameters[i]._value, ref ofs);
            }
            Write1(new Span<byte>(brushData1, ofs, 4), _brushSize, ref ofs);


            new Span<byte>(brushData1, 208, 15 * 64).CopyTo(new Span<byte>(brushData1, 208 + 64, 15 * 64));
            MemoryMarshal.Write(new Span<byte>(brushData1, 208, 64), ref inputPointerData.PointerData);
            brushDataBuffer.UpdateResource<byte>(brushData1);

        }
        void ComputeBrush(ComputeShader c, List<Int2> tilesCovered)
        {
            ComputeBuffer tilesPos = new ComputeBuffer(PaintingTexture.GetDeviceResources(), tilesCovered.Count, 8, tilesCovered.ToArray());
            c.SetSRV(tilesPos, 0);
            c.SetCBV(brushDataBuffer, 0);
            c.SetUAV(PaintingTexture, 0);
            for (int i = 0; i < Core.Brush.c_refTextureCount; i++)
                if (currentBrush.refTexture[i] != null)
                    c.SetSRV(currentBrush.refTexture[i], 1 + i);
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
