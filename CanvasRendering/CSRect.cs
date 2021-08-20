using System;
using System.Collections.Generic;
using System.Text;
using Vortice.Direct3D;
using Vortice.Direct3D11;
using System.Numerics;
using System.Runtime.InteropServices;
using Vortice.DXGI;

namespace CanvasRendering
{
    public class CSRect
    {
        public struct vertex
        {
            public Vector3 Position;
            public Vector2 UV;
            public uint Color;
            public vertex(Vector3 position, Vector2 uv)
            {
                this.Position = position;
                this.Color = 0xffffffff;
                this.UV = uv;
            }
        }
        public void Render()
        {
            var context = DeviceResources.d3dContext;
            Matrix4x4 mvp = Matrix4x4.Transpose(
                Matrix4x4.CreateTranslation(0, -1, 0) *
                Matrix4x4.CreateScale(Scale * width, Scale * height, 1) *
                Matrix4x4.CreateRotationZ(Rotation) *
                Matrix4x4.CreateTranslation(Position.X, -Position.Y, 0) *
                Matrix4x4.CreateScale(2.0f / DeviceResources.m_outputSize.X, 2.0f / DeviceResources.m_outputSize.Y, 1) *
                Matrix4x4.CreateTranslation(-1, 1, 0));
            byte[] data = new byte[256];
            MemoryMarshal.Write(data, ref mvp);
            Vector2 size = new Vector2(width, height);
            MemoryMarshal.Write(new Span<byte>(data, 64, 8), ref size);
            context.UpdateSubresource(data, constantBuffer);

            context.VSSetConstantBuffer(0, constantBuffer);
            context.PSSetConstantBuffer(0, constantBuffer);
            context.IASetVertexBuffers(0, new VertexBufferView(vertexBuffer, Marshal.SizeOf(typeof(vertex))));
            context.IASetIndexBuffer(indicesBuffer, Vortice.DXGI.Format.R16_UInt, 0);
            context.IASetInputLayout(vertexShader.GetInputLayout(DeviceResources, unnamedInputLayout));
            context.IASetPrimitiveTopology(PrimitiveTopology.TriangleList);
            context.VSSetShader(vertexShader.vertexShader);
            context.PSSetShader(pixelShader.pixelShader);
            context.PSSetSampler(0, samplerState);
            context.PSSetShaderResource(0, refTextures[0].srv);
            context.DrawIndexed(6, 0, 0);
        }
        public void Initialize(DeviceResources deviceResources, int width, int height)
        {
            this.width = width;
            this.height = height;
            DeviceResources = deviceResources;
            vertex[] vertices = new vertex[]
            {
                new vertex(new Vector3(1,0,0),new Vector2(1,1)),
                new vertex(new Vector3(1,1,0),new Vector2(1,0)),
                new vertex(new Vector3(0,0,0),new Vector2(0,1)),
                new vertex(new Vector3(0,1,0),new Vector2(0,0)),
            };
            ushort[] indices = new ushort[]
            {
                0,2,1,
                1,2,3,
            };
            vertexBuffer = deviceResources.device.CreateBuffer(BindFlags.VertexBuffer, vertices);
            indicesBuffer = deviceResources.device.CreateBuffer(BindFlags.IndexBuffer, indices);
            constantBuffer = deviceResources.device.CreateBuffer(new BufferDescription(256, BindFlags.ConstantBuffer, ResourceUsage.Default));

            samplerState = deviceResources.device.CreateSamplerState(new SamplerDescription(Filter.MinLinearMagPointMipLinear, TextureAddressMode.Clamp, TextureAddressMode.Clamp, TextureAddressMode.Clamp));
        }
        public void SetRefTexture(RenderTexture tex, int slot)
        {
            refTextures[slot] = tex;
        }
        public RenderTexture[] refTextures = new RenderTexture[8];
        public int width;
        public int height;
        public VertexShader vertexShader;
        public PixelShader pixelShader;
        public DeviceResources DeviceResources;
        public ID3D11SamplerState samplerState;
        public ID3D11Buffer vertexBuffer;
        public ID3D11Buffer indicesBuffer;
        public ID3D11Buffer constantBuffer;
        public Vector2 Position;
        public UnnamedInputLayout unnamedInputLayout = new UnnamedInputLayout
        {
            inputElementDescriptions = new InputElementDescription[]
            {
                new InputElementDescription("POSITION",0,Format.R32G32B32_Float,0),
                new InputElementDescription("TEXCOORD",0,Format.R32G32_Float,0),
                new InputElementDescription("COLOR",0,Format.R8G8B8A8_UNorm,0),
            }
        };

        public float Scale = 1.0f;
        public float Rotation;
    }
}
