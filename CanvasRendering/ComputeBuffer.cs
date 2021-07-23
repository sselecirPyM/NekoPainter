using System;
using System.Collections.Generic;
using System.Text;
using Vortice.Direct3D11;
using Vortice.Direct3D;
using Vortice.Mathematics;
using System.Runtime.InteropServices;

namespace CanvasRendering
{
    public class ComputeBuffer : IDisposable
    {
        public ComputeBuffer(DeviceResources deviceResources, int count, int stride)
        {
            Create(deviceResources, count, stride);
        }
        public ComputeBuffer(ComputeBuffer another)
        {
            Create(another.DeviceResources, another.count, another.stride);
            DeviceResources.d3dContext.CopyResource(buffer, another.buffer);
        }
        public ComputeBuffer(DeviceResources deviceResources, int count, int stride, Span<byte> data)
        {
            Create(deviceResources, count, stride);
            SetData(data);
        }
        public ComputeBuffer(DeviceResources deviceResources, int count, int stride, Span<Int2> data)
        {
            Create(deviceResources, count, stride);
            SetData(data);
        }
        public ComputeBuffer(DeviceResources deviceResources, int count, int stride, Span<int> data)
        {
            Create(deviceResources, count, stride);
            SetData(data);
        }

        void Create(DeviceResources deviceResources, int count, int stride)
        {
            this.stride = stride;
            this.count = count;
            size = count * stride;
            DeviceResources = deviceResources;
            buffer = deviceResources.device.CreateBuffer(new BufferDescription(size, ResourceUsage.Default, BindFlags.ShaderResource | BindFlags.UnorderedAccess, CpuAccessFlags.None, ResourceOptionFlags.BufferStructured, stride));
            srv = deviceResources.device.CreateShaderResourceView(buffer);
            uav = deviceResources.device.CreateUnorderedAccessView(buffer);
        }

        public void GetData<T>(Span<T> output) where T : unmanaged
        {
            BufferDescription bufferDesc2 = new BufferDescription(size, ResourceUsage.Staging, BindFlags.None, CpuAccessFlags.Read);

            ID3D11Buffer tempBuffer = DeviceResources.device.CreateBuffer(bufferDesc2);

            DeviceResources.d3dContext.CopyResource(tempBuffer, buffer);

            MappedSubresource mappedSubresource = DeviceResources.d3dContext.Map(tempBuffer, MapMode.Read);
            int c = Marshal.SizeOf(typeof(T));
            var data = mappedSubresource.AsSpan<T>(output.Length * c);
            //var data = DeviceResources.d3dContext.Map<T>(tempBuffer, 0, 0,MapMode.Read,MapFlags.None);

            data.CopyTo(output);

            DeviceResources.d3dContext.Unmap(tempBuffer, 0);
            tempBuffer.Dispose();
        }
        public void SetData<T>(Span<T> input) where T : unmanaged
        {
            DeviceResources.d3dContext.UpdateSubresource<T>(input, buffer);
            //MappedSubresource mappedSubresource = DeviceResources.d3dContext.Map(buffer, MapMode.Write);
            //var data = mappedSubresource.AsSpan<T>(input.Length);
            //input.CopyTo(data);
        }

        public ID3D11ShaderResourceView srv;
        public ID3D11UnorderedAccessView uav;
        public ID3D11Buffer buffer;
        public int size;
        public int count;
        public int stride;
        public DeviceResources DeviceResources;
        private bool disposedValue;

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    // TODO: 释放托管状态(托管对象)
                }
                srv?.Dispose();
                uav?.Dispose();
                buffer?.Dispose();
                srv = null;
                uav = null;
                buffer = null;
                // TODO: 释放未托管的资源(未托管的对象)并重写终结器
                // TODO: 将大型字段设置为 null
                disposedValue = true;
            }
        }

        // TODO: 仅当“Dispose(bool disposing)”拥有用于释放未托管资源的代码时才替代终结器
        ~ComputeBuffer()
        {
            // 不要更改此代码。请将清理代码放入“Dispose(bool disposing)”方法中
            Dispose(disposing: false);
        }

        public void Dispose()
        {
            // 不要更改此代码。请将清理代码放入“Dispose(bool disposing)”方法中
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
    }
}
