using System;
using System.Collections.Generic;
using System.Text;
using System.Numerics;
using Vortice.Direct3D11;

namespace CanvasRendering
{
    public class GraphicsContext
    {
        public GraphicsContext()
        {
            var blendDesc = new BlendDescription
            {
                AlphaToCoverageEnable = false
            };

            blendDesc.RenderTarget[0] = new RenderTargetBlendDescription
            {
                IsBlendEnabled = true,
                SourceBlend = Blend.SourceAlpha,
                DestinationBlend = Blend.InverseSourceAlpha,
                BlendOperation = BlendOperation.Add,
                SourceBlendAlpha = Blend.InverseSourceAlpha,
                DestinationBlendAlpha = Blend.Zero,
                BlendOperationAlpha = BlendOperation.Add,
                RenderTargetWriteMask = ColorWriteEnable.All
            };
            defaultBlendState = DeviceResources.device.CreateBlendState(blendDesc);

            var stencilOpDesc = new DepthStencilOperationDescription(StencilOperation.Keep, StencilOperation.Keep, StencilOperation.Keep, ComparisonFunction.Always);
            var depthDesc = new DepthStencilDescription
            {
                DepthEnable = false,
                DepthWriteMask = DepthWriteMask.All,
                DepthFunc = ComparisonFunction.Always,
                StencilEnable = false,
                FrontFace = stencilOpDesc,
                BackFace = stencilOpDesc
            };

            depthStencilState = DeviceResources.device.CreateDepthStencilState(depthDesc);
        }
        public void ClearScreen()
        {
            var deviceContext = d3dContext;
            deviceContext.RSSetViewport(0, 0, DeviceResources.m_d3dRenderTargetSize.X, DeviceResources.m_d3dRenderTargetSize.Y);
            deviceContext.OMSetRenderTargets(DeviceResources.renderTargetView1);
            deviceContext.ClearRenderTargetView(DeviceResources.renderTargetView1, clearColor);
        }
        public void SetLogicSize(Vector2 size)
        {
            DeviceResources.SetLogicalSize(size);
        }
        public void SetPipelineState(PipelineStateObject pso)
        {
            PipelineStateObject = pso;
            d3dContext.VSSetShader(pso.GetVertexShader(DeviceResources));
            d3dContext.PSSetShader(pso.GetPixelShader(DeviceResources));
        }

        public void SetCBV(ConstantBuffer constantBuffer, int slot, int ofs, int size)
        {
            int[] ofs1 = { ofs / 16 };
            int[] size1 = { size / 16 };
            d3dContext.VSSetConstantBuffer1(slot, constantBuffer.m_buffer, ofs1, size1);
            d3dContext.PSSetConstantBuffer1(slot, constantBuffer.m_buffer, ofs1, size1);
        }
        public void SetCBV(ConstantBuffer constantBuffer, int slot)
        {
            d3dContext.VSSetConstantBuffer(slot, constantBuffer.m_buffer);
            d3dContext.PSSetConstantBuffer(slot, constantBuffer.m_buffer);
        }
        public void SetMesh(Mesh mesh)
        {
            var deviceContext = d3dContext;
            deviceContext.IASetPrimitiveTopology(Vortice.Direct3D.PrimitiveTopology.TriangleList);
            deviceContext.IASetVertexBuffer(0, mesh.vertexBuffer, mesh.stride, 0);
            deviceContext.IASetIndexBuffer(mesh.indexBuffer, Vortice.DXGI.Format.R16_UInt, 0);
            unnamedInputLayout = mesh.unnamedInputLayout;
        }
        public void Present()
        {
            DeviceResources.Present();
        }
        public void SetSRV(RenderTexture texture, int slot)
        {
            d3dContext.PSSetShaderResource(slot, texture.srv);
        }
        public void RSSetScissorRect(Vortice.RawRect rect)
        {
            d3dContext.RSSetScissorRect(rect);
        }
        public void SetScissorRectDefault()
        {
            d3dContext.RSSetScissorRect(new Vortice.RawRect(0, 0, (int)DeviceResources.m_d3dRenderTargetSize.X, (int)DeviceResources.m_d3dRenderTargetSize.Y));
        }
        public void SetSampler(SamplerState samplerState, int slot)
        {
            d3dContext.PSSetSampler(slot, DeviceResources.GetSamplerState(samplerState));
        }
        public void DrawIndexed(int indexCount, int startIndexLocation, int baseVertexLocation)
        {
            var context = d3dContext;
            context.OMSetBlendState(defaultBlendState);
            context.OMSetDepthStencilState(depthStencilState);
            if (samplerState == null)
            {
                samplerState = DeviceResources.device.CreateSamplerState(new SamplerDescription(Filter.MinMagMipLinear, TextureAddressMode.Wrap, TextureAddressMode.Wrap, TextureAddressMode.Wrap));
            }
            if (rasterizerState == null)
            {
                rasterizerState = DeviceResources.device.CreateRasterizerState(new RasterizerDescription(CullMode.None, FillMode.Solid) { ScissorEnable = true });
            }
            context.PSSetSampler(0, samplerState);
            context.RSSetState(rasterizerState);
            context.IASetInputLayout(PipelineStateObject.GetInputLayout(DeviceResources, unnamedInputLayout));

            context.DrawIndexed(indexCount, startIndexLocation, baseVertexLocation);
        }
        public void SetClearColor(Vector4 color) => this.clearColor = new Vortice.Mathematics.Color4(color);
        public Vortice.Mathematics.Color4 clearColor;
        public DeviceResources DeviceResources { get; private set; } = new DeviceResources();
        public ID3D11BlendState defaultBlendState;
        public ID3D11SamplerState samplerState;
        public UnnamedInputLayout unnamedInputLayout;
        ID3D11RasterizerState rasterizerState;

        public PipelineStateObject PipelineStateObject;

        ID3D11DepthStencilState depthStencilState;

        ID3D11DeviceContext3 d3dContext { get => DeviceResources.d3dContext; }
    }
}
