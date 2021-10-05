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
            int bytePerPixel = 4;
            if (format == Format.R32G32B32A32_Float)
                bytePerPixel = 16;
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
            int bytePerPixel = GetBytePerPixel(format);
            var context = device.d3dContext;
            context.UpdateSubresource(data, texture2D, 0, width * bytePerPixel,width * height * bytePerPixel);
        }

        public void CopyTo(RenderTexture another)
        {
            device.d3dContext.CopyResource(another.texture2D, texture2D);
        }

        public void Clear()
        {
            device.d3dContext.ClearUnorderedAccessView(uav, Color4.Transparent);
        }

        public Span<float> GetRawData()
        {
            return new Span<float>();
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

        public byte[] GetData(ComputeShader processor)
        {
            var context = device.d3dContext;
            Texture2DDescription tex2dDesc = new Texture2DDescription(Format.R32G32B32A32_Float, width, height, 1, 1, BindFlags.UnorderedAccess);
            Texture2DDescription tex2dReadbackDesc = new Texture2DDescription(Format.R32G32B32A32_Float, width, height, 1, 1, 0, ResourceUsage.Staging, CpuAccessFlags.Read);

            ID3D11Texture2D tex2d = device.device.CreateTexture2D(tex2dDesc);
            UnorderedAccessViewDescription uavDesc = new UnorderedAccessViewDescription(tex2d, UnorderedAccessViewDimension.Texture2D, Format.R32G32B32A32_Float);
            ID3D11UnorderedAccessView uav2 = device.device.CreateUnorderedAccessView(tex2d, uavDesc);
            ID3D11Texture2D tex2dReadBack = device.device.CreateTexture2D(tex2dReadbackDesc);
            context.CSSetShaderResource(0, srv);
            context.CSSetUnorderedAccessView(0, uav2);
            processor.Dispatch((width + 31) / 32, (height + 31) / 32, 1);
            context.CopyResource(tex2dReadBack, tex2d);

            Span<byte> cpuRes = context.Map<byte>(tex2dReadBack, 0, 0, MapMode.Read);
            byte[] data = new byte[cpuRes.Length];
            cpuRes.CopyTo(data);
            context.Unmap(tex2dReadBack, 0);
            uav2.Dispose();
            tex2d.Dispose();
            tex2dReadBack.Dispose();

            return data;
        }

        public void ReadImageData1<T>(T[] data, int width, int height, ComputeShader processor)
        {
            var context = device.d3dContext;
            ID3D11Texture2D tex2d = null;
            Texture2DDescription tex2dDesc = new Texture2DDescription(Format.R32G32B32A32_Float, width, height, 1, 1, BindFlags.ShaderResource);
            ID3D11ShaderResourceView srv2 = null;
            SubresourceData subresourceData = new SubresourceData();
            subresourceData.DataPointer = Marshal.UnsafeAddrOfPinnedArrayElement(data, 0);
            subresourceData.Pitch = width * 16;
            subresourceData.SlicePitch = width * height * 16;
            tex2d = device.device.CreateTexture2D(tex2dDesc, new[] { subresourceData });
            ShaderResourceViewDescription srvDesc = new ShaderResourceViewDescription(texture2D, ShaderResourceViewDimension.Texture2D, Format.R32G32B32A32_Float);
            srv2 = device.device.CreateShaderResourceView(tex2d, srvDesc);
            context.CSSetShaderResource(0, srv2);
            context.CSSetUnorderedAccessView(0, uav);
            processor.Dispatch((width + 31) / 32, (height + 31) / 32, 1);
            srv2.Dispose();
            tex2d.Dispose();
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

        public static int GetBytePerPixel(Format format)
        {
            switch(format)
            {
                case Format.R8G8B8A8_SInt:
                case Format.R8G8B8A8_SNorm:
                case Format.R8G8B8A8_Typeless:
                case Format.R8G8B8A8_UInt:
                case Format.R8G8B8A8_UNorm:
                case Format.R8G8B8A8_UNorm_SRgb:
                case Format.R8G8_B8G8_UNorm:
                    return 4;
                case Format.R32G32B32A32_Float:
                case Format.R32G32B32A32_SInt:
                case Format.R32G32B32A32_Typeless:
                case Format.R32G32B32A32_UInt:
                    return 16;
                default: return 0;
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
