using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using DirectCanvas.Layout;
using CanvasRendering;
using System.Numerics;
using System.Runtime.InteropServices;
using System;

namespace DirectCanvas
{
    public class ViewRenderer
    {
        public ViewRenderer(CanvasCase canvasCase)
        {
            CanvasCase = canvasCase;
        }

        public void RenderAll()
        {
            PrepareRenderData();
            for (int i = 0; i < RenderTarget.Count; i++)
                RenderTarget[i].Clear();
            int ofs = 0;
            for (int i = ManagedLayout.Count - 1; i >= 0; i--)
            {
                PictureLayout selectedLayout = ManagedLayout[i];
                if (ManagedLayout[i].Hidden)
                {
                    ofs += 256;
                    continue;
                }
                CanvasCase.LayoutTex.TryGetValue(selectedLayout.guid, out var tiledTexture);
                if (CanvasCase.blendmodesMap.TryGetValue(selectedLayout.BlendMode, out var blendMode))
                {
                    if (selectedLayout.DataSource == PictureDataSource.Color)
                    {
                        blendMode?.BlendPure(RenderTarget[0], constantBuffer1, ofs, 256);
                    }
                    else if (CanvasCase.PaintAgent.CurrentLayout == selectedLayout)
                    {
                        blendMode?.Blend(PaintingTexture, RenderTarget[0], constantBuffer1, ofs, 256);
                    }
                    else if (tiledTexture != null && tiledTexture.tilesCount != 0)
                    {
                        blendMode?.Blend(tiledTexture, RenderTarget[0], constantBuffer1, ofs, 256);
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
        //    for (int i = ManagedLayout.Count - 1; i >= 0; i--)
        //    {

        //        PictureLayout selectedLayout = ManagedLayout[i];
        //        if (LayoutsHiddenData[i] == true) continue;
        //        if (selectedLayout is StandardLayout standardLayout)
        //        {
        //            RenderTexture[] refs = new RenderTexture[Core.BlendMode.c_refCount];
        //            refs[0] = RenderTarget[0];
        //            if (standardLayout.activated)
        //            {
        //                standardLayout.BlendMode?.Blend(PaintingTexture, RenderTarget[0], part, refs, RenderDataCaches[i]);
        //            }
        //            else if (standardLayout.tiledTexture != null && standardLayout.tiledTexture.tileRect.HaveIntersections(filterRect))
        //            {
        //                standardLayout.BlendMode?.Blend(standardLayout.tiledTexture, RenderTarget[0], part, refs, RenderDataCaches[i]);
        //            }
        //        }
        //        else if (selectedLayout is PureLayout pureLayout)
        //        {
        //            RenderTexture[] refs = new RenderTexture[Core.BlendMode.c_refCount];
        //            refs[0] = RenderTarget[0];
        //            pureLayout.BlendMode?.BlendColor(RenderTarget[0], part, refs, pureLayout.colorBuffer, RenderDataCaches[i]);
        //        }
        //        else
        //        {
        //            throw new System.NotImplementedException();
        //        }
        //    }
        //}

        void PrepareRenderData()
        {
            if (bufferSize < ManagedLayout.Count * 256 || constantBuffer1 == null)
            {
                bufferSize = (ManagedLayout.Count * 256 + 65535) & (~65535);
                constantBuffer1?.Dispose();
                constantBuffer1 = new ConstantBuffer(DeviceResources, bufferSize);
                cpuBuffer = new byte[bufferSize];
            }

            int ofs = 0;
            for (int i = ManagedLayout.Count - 1; i >= 0; i--)
            {
                Vector4 color = ManagedLayout[i].Color;
                MemoryMarshal.Write(new Span<byte>(cpuBuffer, ofs, 16), ref color);
                GetData(ManagedLayout[i], new Span<byte>(cpuBuffer, ofs + 16, 128));
                ofs += 256;
            }
            constantBuffer1.UpdateResource(new Span<byte>(cpuBuffer));
        }

        public void GetData(PictureLayout layout, Span<byte> outData)
        {
            if (CanvasCase.blendmodesMap.TryGetValue(layout.BlendMode, out var blendMode) && blendMode.Paramerters != null)
            {
                int ofs = 0;
                for (int i = 0; i < blendMode.Paramerters.Length; i++)
                {
                    if (layout.parameters.TryGetValue(blendMode.Paramerters[i].Name, out var value))
                    {
                        float X = (float)value.X;
                        Write(outData.Slice(ofs, 4), X);
                    }
                    else
                    {
                        Write(outData.Slice(ofs, 4), 0);
                    }
                    ofs += 4;
                }
            }
        }

        void Write<T>(Span<byte> target, T value) where T : struct
        {
            MemoryMarshal.Write(target, ref value);
        }

        IReadOnlyList<RenderTexture> RenderTarget { get { return CanvasCase.RenderTarget; } }
        RenderTexture PaintingTexture { get { return CanvasCase.PaintingTexture; } }
        DeviceResources DeviceResources { get { return CanvasCase.DeviceResources; } }
        IReadOnlyList<PictureLayout> ManagedLayout { get { return CanvasCase.Layouts; } }

        public readonly CanvasCase CanvasCase;

        int bufferSize = 0;
        ConstantBuffer constantBuffer1;
        byte[] cpuBuffer;
    }
}

