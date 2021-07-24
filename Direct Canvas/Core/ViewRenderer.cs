using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using DirectCanvas.Layout;
using CanvasRendering;
using System.Numerics;
using System.Runtime.InteropServices;

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
            for (int i = ManagedLayout.Count - 1; i >= 0; i--)
            {
                PictureLayout selectedLayout = ManagedLayout[i];
                if (LayoutsHiddenData[i] == true) continue;
                if (selectedLayout is StandardLayout standardLayout)
                {
                    Vector4 _color = selectedLayout.Color;
                    colorBuffers[i].UpdateResource<Vector4>(ref _color);
                    RenderTexture[] refs = new RenderTexture[Core.BlendMode.c_refCount];
                    refs[0] = RenderTarget[0];
                    if (standardLayout.PureLayout)
                    {
                        if (CanvasCase.blendmodesMap.TryGetValue(selectedLayout.BlendMode, out var blendMode))
                        {
                            blendMode?.Blend(RenderTarget[0], refs, colorBuffers[i], RenderDataCaches[i]);
                        }
                    }
                    else if (standardLayout.activated)
                    {
                        if (CanvasCase.blendmodesMap.TryGetValue(selectedLayout.BlendMode, out var blendMode))
                        {
                            blendMode?.Blend(PaintingTexture, RenderTarget[0], refs, RenderDataCaches[i]);
                        }
                    }
                    else if (standardLayout.tiledTexture != null && standardLayout.tiledTexture.tilesCount != 0)
                    {
                        if (CanvasCase.blendmodesMap.TryGetValue(selectedLayout.BlendMode, out var blendMode))
                        {
                            blendMode?.Blend(standardLayout.tiledTexture, RenderTarget[0], refs, RenderDataCaches[i]);
                        }
                    }
                }
                else if (selectedLayout is PureLayout pureLayout)
                {
                    Vector4 _color = selectedLayout.Color;
                    colorBuffers[i].UpdateResource<Vector4>(ref _color);
                    RenderTexture[] refs = new RenderTexture[Core.BlendMode.c_refCount];
                    refs[0] = RenderTarget[0];
                    if (CanvasCase.blendmodesMap.TryGetValue(selectedLayout.BlendMode, out var blendMode))
                    {
                        blendMode?.Blend(RenderTarget[0], refs, colorBuffers[i], RenderDataCaches[i]);
                    }
                }
                else
                {
                    throw new System.NotImplementedException();
                }
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
        //        texturePartClear.Dispatch(1, 1, (partCount +15)/16);
        //        buf.Dispose();
        //    }
        //    for (int i = ManagedLayout.Count - 1; i >= 0; i--)
        //    {

        //        PictureLayout selectedLayout = ManagedLayout[i];
        //        if (LayoutsHiddenData[i] == true) continue;
        //        if (selectedLayout is StandardLayout standardLayout)
        //        {
        //            if (standardLayout.RenderBufferNum >= 0 && standardLayout.RenderBufferNum < RenderTarget.Count)
        //            {
        //                RenderTexture[] refs = new RenderTexture[Core.BlendMode.c_refCount];
        //                if (standardLayout.RefBufferNum >= 0 && standardLayout.RefBufferNum < RenderTarget.Count)
        //                {
        //                    refs[0] = RenderTarget[standardLayout.RefBufferNum];
        //                }
        //                if (standardLayout.RenderBufferNum >= 0 && standardLayout.RenderBufferNum < RenderTarget.Count)
        //                {
        //                    if (standardLayout.activated)
        //                    {
        //                        standardLayout.BlendMode?.Blend(PaintingTexture, RenderTarget[standardLayout.RenderBufferNum], part, refs, RenderDataCaches[i]);
        //                    }
        //                    else if (standardLayout.tiledTexture != null && standardLayout.tiledTexture.tileRect.HaveIntersections(filterRect))
        //                    {
        //                        standardLayout.BlendMode?.Blend(standardLayout.tiledTexture, RenderTarget[standardLayout.RenderBufferNum], part, refs, RenderDataCaches[i]);
        //                    }
        //                }
        //            }
        //        }
        //        else if (selectedLayout is PureLayout pureLayout)
        //        {
        //            if (!pureLayout.colorUpdated) pureLayout.UpdateColor();
        //            RenderTexture[] refs = new RenderTexture[Core.BlendMode.c_refCount];
        //            if (pureLayout.RefBufferNum >= 0 && pureLayout.RefBufferNum < RenderTarget.Count)
        //            {
        //                refs[0] = RenderTarget[pureLayout.RefBufferNum];
        //            }
        //            if (pureLayout.RenderBufferNum >= 0 && pureLayout.RenderBufferNum < RenderTarget.Count)
        //            {
        //                pureLayout.BlendMode?.Blend(RenderTarget[pureLayout.RenderBufferNum], part, refs, pureLayout.colorBuffer, RenderDataCaches[i]);
        //            }
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
            while (RenderDataCaches.Count < ManagedLayout.Count)
            {
                RenderDataCaches.Add(new ConstantBuffer(DeviceResources, 256));
                colorBuffers.Add(new ConstantBuffer(DeviceResources, 256));
                LayoutsHiddenData.Add(false);
            }

            int ofs = 0;
            int[] data = new int[Core.BlendMode.c_parameterCount];
            var dataSpan = MemoryMarshal.Cast<int, byte>(data);
            for (int i = ManagedLayout.Count - 1; i >= 0; i--)
            {
                GetData(ManagedLayout[i], data, out bool hidden);
                LayoutsHiddenData[i] = hidden;
                RenderDataCaches[i].UpdateResource<int>(data);
                Vector4 color = ManagedLayout[i].Color;
                MemoryMarshal.Write(new System.Span<byte>(cpuBuffer, ofs, 16), ref color);
                dataSpan.CopyTo(new System.Span<byte>(cpuBuffer, ofs + 16, dataSpan.Length));
                ofs += 256;
            }
            constantBuffer1.UpdateResource(new System.Span<byte>(cpuBuffer));
        }

        public void GetData(PictureLayout layout, int[] outData, out bool hidden)
        {
            //for (int j = 0; j < Core.BlendMode.c_parameterCount; j++)
            //{
            //    outData[j] = layout.Parameters[j].Value;
            //}
            hidden = layout.Hidden;
        }

        public List<bool> LayoutsHiddenData = new List<bool>();
        IReadOnlyList<RenderTexture> RenderTarget { get { return CanvasCase.RenderTarget; } }
        RenderTexture PaintingTexture { get { return CanvasCase.PaintingTexture; } }
        DeviceResources DeviceResources { get { return CanvasCase.DeviceResources; } }
        IReadOnlyList<PictureLayout> ManagedLayout { get { return CanvasCase.Layouts; } }

        public readonly CanvasCase CanvasCase;

        int bufferSize = 0;
        ConstantBuffer constantBuffer1;
        byte[] cpuBuffer;

        private List<ConstantBuffer> RenderDataCaches = new List<ConstantBuffer>();
        private List<ConstantBuffer> colorBuffers = new List<ConstantBuffer>();
    }
}

