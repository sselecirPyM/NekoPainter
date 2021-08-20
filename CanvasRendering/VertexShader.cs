using System;
using System.Collections.Generic;
using System.Text;
using Vortice.Direct3D11;
using Vortice.Direct3D;
using Vortice.D3DCompiler;

namespace CanvasRendering
{
    public class VertexShader : IDisposable
    {
        public static VertexShader CompileAndCreate(DeviceResources deviceResources, byte[] source)
        {
            return CompileAndCreate(deviceResources, source, "main");
        }
        public static VertexShader CompileAndCreate(DeviceResources deviceResources, byte[] source, string entryPoint)
        {
            VertexShader vertexShader = new VertexShader();
            var hr = Compiler.Compile(source, entryPoint, null, "vs_5_0", out vertexShader.data, out Blob errorBlob);
            vertexShader.vertexShader = deviceResources.device.CreateVertexShader(vertexShader.data);

            return vertexShader;
        }

        public ID3D11InputLayout GetInputLayout(DeviceResources deviceResources, UnnamedInputLayout unnamedInputLayout)
        {
            if (unnamedInputLayouts.TryGetValue(unnamedInputLayout, out var inputLayout))
            {
                return inputLayout;
            }
            var inputElementDescriptions = unnamedInputLayout.inputElementDescriptions;

            inputLayout = deviceResources.device.CreateInputLayout(inputElementDescriptions, data);
            unnamedInputLayouts[unnamedInputLayout] = inputLayout;
            return inputLayout;
        }
        public Blob data;
        public ID3D11VertexShader vertexShader;
        public Dictionary<string, ID3D11InputLayout> inputLayouts = new Dictionary<string, ID3D11InputLayout>();
        public Dictionary<UnnamedInputLayout, ID3D11InputLayout> unnamedInputLayouts = new Dictionary<UnnamedInputLayout, ID3D11InputLayout>();
        public void Dispose()
        {
            vertexShader?.Dispose();
            vertexShader = null;
            foreach(var pair in inputLayouts)
            {
                pair.Value.Dispose();
            }
            inputLayouts.Clear();
            data?.Dispose();
            data = null;
        }
    }
}
