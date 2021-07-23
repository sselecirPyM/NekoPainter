using System;
using System.Collections.Generic;
using System.Text;
using Vortice.D3DCompiler;
using Vortice.Direct3D;
using Vortice.Direct3D11;

namespace CanvasRendering
{
    public class PixelShader
    {
        public static PixelShader CompileAndCreate(DeviceResources deviceResources, byte[] source)
        {
            return CompileAndCreate(deviceResources, source, "main");
        }
        public static PixelShader CompileAndCreate(DeviceResources deviceResources, byte[] source, string entryPoint)
        {
            PixelShader pixelShader = new PixelShader();
            var hr = Compiler.Compile(source, entryPoint, null, "ps_5_0", out Blob data, out Blob errorBlob);
            pixelShader.pixelShader = deviceResources.device.CreatePixelShader(data);

            return pixelShader;
        }
        public ID3D11PixelShader pixelShader;
    }
}
