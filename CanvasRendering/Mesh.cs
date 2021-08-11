using System;
using System.Collections.Generic;
using System.Text;
using Vortice.Direct3D11;
using Vortice.Direct3D;
using Vortice.D3DCompiler;

namespace CanvasRendering
{
    public class Mesh : IDisposable
    {
        public ID3D11Buffer vertexBuffer;
        public ID3D11Buffer indexBuffer;
        public DeviceResources deviceResources;
        public int stride;
        public string inputLayout;
        public Mesh(DeviceResources deviceResources, int stride, string inputLayout)
        {
            this.deviceResources = deviceResources;
            this.stride = stride;
            this.inputLayout = inputLayout;
        }

        public void Update(byte[] vertice, byte[] indice)
        {
            vertexBuffer?.Dispose();
            indexBuffer?.Dispose();

            vertexBuffer = deviceResources.device.CreateBuffer(vertice, new BufferDescription(vertice.Length, BindFlags.VertexBuffer, ResourceUsage.Default));
            indexBuffer = deviceResources.device.CreateBuffer(indice, new BufferDescription(indice.Length, BindFlags.IndexBuffer, ResourceUsage.Default));
        }


        public void Dispose()
        {
            vertexBuffer?.Dispose();
            indexBuffer?.Dispose();
        }
    }
}
