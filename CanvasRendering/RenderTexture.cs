using System;
using System.Collections.Generic;
using System.Text;
using Vortice.Direct3D11;
using Vortice.Direct3D;
using Vortice.DXGI;
using Vortice.Mathematics;
using System.Runtime.InteropServices;

namespace CanvasRendering
{
    public class RenderTexture : IDisposable
    {
        public RenderTexture(DeviceResources device, int width, int height, Format format, bool useMipMap)
        {
            Create(device, width, height, format, useMipMap);
        }

        public RenderTexture()
        {

        }

        public void Create(DeviceResources device, int width, int height, Format format, bool useMipMap)
        {
            this.width = width;
            this.height = height;
            this.format = format;
            this.device = device;
            texture2D = device.device.CreateTexture2D(new Texture2DDescription(format, width, height, 1, 1, BindFlags.ShaderResource | BindFlags.UnorderedAccess));
            srv = device.device.CreateShaderResourceView(texture2D);
            uav = device.device.CreateUnorderedAccessView(texture2D);
        }

        public void Create2<T>(DeviceResources device, int width, int height, Format format, bool useMipMap, T[] data) where T : unmanaged
        {
            this.width = width;
            this.height = height;
            this.format = format;
            this.device = device;
            int bytePerPixel = GetBitPerPixel(format) / 8;

            SubresourceData subresourceData = new SubresourceData();
            subresourceData.DataPointer = Marshal.UnsafeAddrOfPinnedArrayElement(data, 0);
            subresourceData.Pitch = width * bytePerPixel;
            subresourceData.SlicePitch = width * height * bytePerPixel;

            texture2D = device.device.CreateTexture2D(new Texture2DDescription(format, width, height, 1, 1, BindFlags.ShaderResource | BindFlags.UnorderedAccess), new[] { subresourceData });
            srv = device.device.CreateShaderResourceView(texture2D);
            uav = device.device.CreateUnorderedAccessView(texture2D);
        }

        public void UpdateTexture<T>(Span<T> data) where T : unmanaged
        {
            int bytePerPixel = GetBitPerPixel(format) / 8;
            var context = device.d3dContext;
            context.UpdateSubresource(data, texture2D, 0, width * bytePerPixel, width * height * bytePerPixel);
        }

        public void CopyTo(RenderTexture another)
        {
            device.d3dContext.CopyResource(another.texture2D, texture2D);
        }

        public void Clear()
        {
            device.d3dContext.ClearUnorderedAccessView(uav, Color4.Transparent);
        }

        public byte[] GetData()
        {
            var context = device.d3dContext;
            Texture2DDescription tex2dReadbackDesc = new Texture2DDescription(Format.R32G32B32A32_Float, width, height, 1, 1, 0, ResourceUsage.Staging, CpuAccessFlags.Read);
            ID3D11Texture2D tex2dReadBack = device.device.CreateTexture2D(tex2dReadbackDesc);
            context.CopyResource(tex2dReadBack, texture2D);

            Span<byte> cpuRes = context.Map<byte>(tex2dReadBack, 0, 0, MapMode.Read);
            byte[] data = new byte[cpuRes.Length];
            cpuRes.CopyTo(data);
            context.Unmap(tex2dReadBack, 0);
            tex2dReadBack.Dispose();

            return data;
        }

        public DeviceResources GetDevice()
        {
            return device;
        }

        public void Dispose()
        {
            srv?.Dispose();
            uav?.Dispose();
            texture2D?.Dispose();
        }

        public static int GetBitPerPixel(Format format)
        {
            switch (format)
            {
                case Format.R32G32B32A32_Typeless:
                case Format.R32G32B32A32_Float:
                case Format.R32G32B32A32_UInt:
                case Format.R32G32B32A32_SInt:
                    return 128;

                case Format.R32G32B32_Typeless:
                case Format.R32G32B32_Float:
                case Format.R32G32B32_UInt:
                case Format.R32G32B32_SInt:
                    return 96;

                case Format.R16G16B16A16_Typeless:
                case Format.R16G16B16A16_Float:
                case Format.R16G16B16A16_UNorm:
                case Format.R16G16B16A16_UInt:
                case Format.R16G16B16A16_SNorm:
                case Format.R16G16B16A16_SInt:
                case Format.R32G32_Typeless:
                case Format.R32G32_Float:
                case Format.R32G32_UInt:
                case Format.R32G32_SInt:
                case Format.R32G8X24_Typeless:
                case Format.D32_Float_S8X24_UInt:
                case Format.R32_Float_X8X24_Typeless:
                case Format.X32_Typeless_G8X24_UInt:
                case Format.Y416:
                case Format.Y210:
                case Format.Y216:
                    return 64;

                case Format.R10G10B10A2_Typeless:
                case Format.R10G10B10A2_UNorm:
                case Format.R10G10B10A2_UInt:
                case Format.R11G11B10_Float:
                case Format.R8G8B8A8_Typeless:
                case Format.R8G8B8A8_UNorm:
                case Format.R8G8B8A8_UNorm_SRgb:
                case Format.R8G8B8A8_UInt:
                case Format.R8G8B8A8_SNorm:
                case Format.R8G8B8A8_SInt:
                case Format.R16G16_Typeless:
                case Format.R16G16_Float:
                case Format.R16G16_UNorm:
                case Format.R16G16_UInt:
                case Format.R16G16_SNorm:
                case Format.R16G16_SInt:
                case Format.R32_Typeless:
                case Format.D32_Float:
                case Format.R32_Float:
                case Format.R32_UInt:
                case Format.R32_SInt:
                case Format.R24G8_Typeless:
                case Format.D24_UNorm_S8_UInt:
                case Format.R24_UNorm_X8_Typeless:
                case Format.X24_Typeless_G8_UInt:
                case Format.R9G9B9E5_SharedExp:
                case Format.R8G8_B8G8_UNorm:
                case Format.G8R8_G8B8_UNorm:
                case Format.B8G8R8A8_UNorm:
                case Format.B8G8R8X8_UNorm:
                case Format.R10G10B10_Xr_Bias_A2_UNorm:
                case Format.B8G8R8A8_Typeless:
                case Format.B8G8R8A8_UNorm_SRgb:
                case Format.B8G8R8X8_Typeless:
                case Format.B8G8R8X8_UNorm_SRgb:
                case Format.AYUV:
                case Format.Y410:
                case Format.YUY2:
                    return 32;

                case Format.P010:
                case Format.P016:
                    return 24;

                case Format.R8G8_Typeless:
                case Format.R8G8_UNorm:
                case Format.R8G8_UInt:
                case Format.R8G8_SNorm:
                case Format.R8G8_SInt:
                case Format.R16_Typeless:
                case Format.R16_Float:
                case Format.D16_UNorm:
                case Format.R16_UNorm:
                case Format.R16_UInt:
                case Format.R16_SNorm:
                case Format.R16_SInt:
                case Format.B5G6R5_UNorm:
                case Format.B5G5R5A1_UNorm:
                case Format.A8P8:
                case Format.B4G4R4A4_UNorm:
                    return 16;

                case Format.NV12:
                //case Format.420_OPAQUE:
                case Format.Opaque420:
                case Format.NV11:
                    return 12;

                case Format.R8_Typeless:
                case Format.R8_UNorm:
                case Format.R8_UInt:
                case Format.R8_SNorm:
                case Format.R8_SInt:
                case Format.A8_UNorm:
                case Format.AI44:
                case Format.IA44:
                case Format.P8:
                    return 8;

                case Format.R1_UNorm:
                    return 1;

                case Format.BC1_Typeless:
                case Format.BC1_UNorm:
                case Format.BC1_UNorm_SRgb:
                case Format.BC4_Typeless:
                case Format.BC4_UNorm:
                case Format.BC4_SNorm:
                    return 4;

                case Format.BC2_Typeless:
                case Format.BC2_UNorm:
                case Format.BC2_UNorm_SRgb:
                case Format.BC3_Typeless:
                case Format.BC3_UNorm:
                case Format.BC3_UNorm_SRgb:
                case Format.BC5_Typeless:
                case Format.BC5_UNorm:
                case Format.BC5_SNorm:
                case Format.BC6H_Typeless:
                case Format.BC6H_Uf16:
                case Format.BC6H_Sf16:
                case Format.BC7_Typeless:
                case Format.BC7_UNorm:
                case Format.BC7_UNorm_SRgb:
                    return 8;

                default:
                    return 0;
            }
        }

        public ID3D11Texture2D texture2D { get; private set; }
        public ID3D11ShaderResourceView srv { get; private set; }
        public ID3D11UnorderedAccessView uav { get; private set; }
        public Format format { get; private set; }
        public DeviceResources device { get; private set; }
        public int width { get; private set; }
        public int height { get; private set; }
    }
}
