using System;
using System.Collections.Generic;
using System.Text;
using Vortice.D3DCompiler;
using Vortice.Direct3D;
using Vortice.Direct3D11;

namespace CanvasRendering
{
    public class PipelineStateObject : IDisposable
    {
        public static PipelineStateObject CompileAndCreate(byte[] source)
        {
            PipelineStateObject pipelineStateObject = new PipelineStateObject();
            var hr = Compiler.Compile(source, "vsmain", null, "vs_5_0", out pipelineStateObject.bVertexShader, out var errorBlob);
            errorBlob?.Dispose();
            if (hr.Failure)
                throw new NotImplementedException();
            hr = Compiler.Compile(source, "psmain", null, "ps_5_0", out pipelineStateObject.bPixelShader, out errorBlob);
            errorBlob?.Dispose();
            if (hr.Failure)
                throw new NotImplementedException();

            return pipelineStateObject;
        }

        public ID3D11InputLayout GetInputLayout(DeviceResources deviceResources, UnnamedInputLayout unnamedInputLayout)
        {
            if (unnamedInputLayouts.TryGetValue(unnamedInputLayout, out var inputLayout))
            {
                return inputLayout;
            }
            var inputElementDescriptions = unnamedInputLayout.inputElementDescriptions;

            inputLayout = deviceResources.device.CreateInputLayout(inputElementDescriptions, bVertexShader);
            unnamedInputLayouts[unnamedInputLayout] = inputLayout;
            return inputLayout;
        }

        public ID3D11VertexShader GetVertexShader(DeviceResources deviceResources)
        {
            return vertexShader ??= deviceResources.device.CreateVertexShader(bVertexShader);
        }
        public ID3D11PixelShader GetPixelShader(DeviceResources deviceResources)
        {
            return pixelShader ??= deviceResources.device.CreatePixelShader(bPixelShader);
        }

        public Blob bVertexShader;
        public Blob bPixelShader;

        public ID3D11VertexShader vertexShader;
        public ID3D11PixelShader pixelShader;
        public ID3D11InputLayout inputLayout;
        public Dictionary<UnnamedInputLayout, ID3D11InputLayout> unnamedInputLayouts = new Dictionary<UnnamedInputLayout, ID3D11InputLayout>();

        public void Dispose()
        {
            vertexShader?.Dispose();
            vertexShader = null;
            pixelShader?.Dispose();
            pixelShader = null;
            inputLayout?.Dispose();
            inputLayout = null;
        }
    }
}
