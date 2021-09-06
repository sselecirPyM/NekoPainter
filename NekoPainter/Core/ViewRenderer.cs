using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using NekoPainter.Core;
using CanvasRendering;
using System.Numerics;
using System.Runtime.InteropServices;
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
                livedDocument.LayoutTex.TryGetValue(selectedLayout.guid, out var tiledTexture);
                if (livedDocument.blendmodesMap.TryGetValue(selectedLayout.BlendMode, out var blendMode))
                {
                    if (selectedLayout.DataSource == PictureDataSource.Color)
                    {
                        blendMode?.BlendPure(RenderTarget[0], constantBuffer1, ofs, 256);
                    }
                    else if (livedDocument.PaintAgent.CurrentLayout == selectedLayout)
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
            if (livedDocument.blendmodesMap.TryGetValue(layout.BlendMode, out var blendMode) && blendMode.Paramerters != null)
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

        IReadOnlyList<RenderTexture> RenderTarget { get { return livedDocument.RenderTarget; } }
        RenderTexture PaintingTexture { get { return livedDocument.PaintingTexture; } }
        DeviceResources DeviceResources { get { return livedDocument.DeviceResources; } }
        IReadOnlyList<PictureLayout> ManagedLayout { get { return livedDocument.Layouts; } }

        public readonly LivedNekoPainterDocument livedDocument;

        int bufferSize = 0;
        ConstantBuffer constantBuffer1;
        byte[] cpuBuffer;
    }
}

