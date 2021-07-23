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
        public Mesh(DeviceResources deviceResources,int stride)
        {
            this.deviceResources = deviceResources;
            this.stride = stride;
        }

        public void Update(byte[] vertice, byte[] indice)
        {
            vertexBuffer?.Dispose();
            indexBuffer?.Dispose();

            vertexBuffer = deviceResources.device.CreateBuffer(vertice, new BufferDescription(vertice.Length, BindFlags.VertexBuffer, ResourceUsage.Default));
            indexBuffer = deviceResources.device.CreateBuffer(indice, new BufferDescription(indice.Length, BindFlags.IndexBuffer, ResourceUsage.Default));

            InputElementDescription[] inputElementDescriptions = new InputElementDescription[]
            {
                new InputElementDescription("POSITION",0,Vortice.DXGI.Format.R32G32B32_Float,0),
                new InputElementDescription("COLOR",0,Vortice.DXGI.Format.R32G32B32A32_Float,0),
                new InputElementDescription("TEXCOORD",0,Vortice.DXGI.Format.R32G32_Float,0),
            };
        }


        public void Dispose()
        {
            vertexBuffer?.Dispose();
            indexBuffer?.Dispose();
        }
    }
}
