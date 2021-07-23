using System;
using System.Collections.Generic;
using System.Text;
using Vortice.Direct3D11;
using Vortice.Direct3D;
using Vortice.D3DCompiler;

namespace CanvasRendering
{
    public class VertexShader
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
            InputElementDescription[] inputElementDescriptions = new InputElementDescription[]
            {
                new InputElementDescription("POSITION",0,Vortice.DXGI.Format.R32G32B32_Float,0),
                new InputElementDescription("TEXCOORD",0,Vortice.DXGI.Format.R32G32_Float,0),
                new InputElementDescription("COLOR",0,Vortice.DXGI.Format.R8G8B8A8_UNorm,0),
            };
            vertexShader.inputLayout = deviceResources.device.CreateInputLayout(inputElementDescriptions, vertexShader.data);

            return vertexShader;
        }
        public Blob data;
        public ID3D11VertexShader vertexShader;
        public ID3D11InputLayout inputLayout;
        public ID3D11InputLayout inputLayoutImgui;
    }
}
