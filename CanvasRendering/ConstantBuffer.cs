using System;
using System.Collections.Generic;
using System.Text;
using Vortice.Direct3D11;

namespace CanvasRendering
{
    public class ConstantBuffer
    {
        public ConstantBuffer(DeviceResources deviceResources, int bufferSize)
        {
            DeviceResources = deviceResources;
            this.size = bufferSize;
            m_buffer = deviceResources.device.CreateBuffer(new BufferDescription(bufferSize, BindFlags.ConstantBuffer, ResourceUsage.Default));
        }
        public void UpdateResource<T>(Span<T> ts) where T : unmanaged
        {
            DeviceResources.d3dContext.UpdateSubresource<T>(ts, m_buffer);
        }
        public void UpdateResource<T>(in T ts) where T : unmanaged
        {
            DeviceResources.d3dContext.UpdateSubresource<T>(in ts, m_buffer);
        }
        public void Dispose()
        {
            m_buffer.Dispose();
        }
        public DeviceResources DeviceResources;
        public ID3D11Buffer m_buffer;
        public int size;
    }
}
