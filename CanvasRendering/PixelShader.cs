using System;
using System.Collections.Generic;
using System.Text;
using Vortice.D3DCompiler;
using Vortice.Direct3D;
using Vortice.Direct3D11;

namespace CanvasRendering
{
    public class PixelShader : IDisposable
    {
        public static PixelShader CompileAndCreate(byte[] source)
        {
            return CompileAndCreate(source, "main");
        }
        public static PixelShader CompileAndCreate(byte[] source, string entryPoint)
        {
            PixelShader pixelShader = new PixelShader();
            var hr = Compiler.Compile(source, entryPoint, null, "ps_5_0", out pixelShader.data, out Blob errorBlob);


            return pixelShader;
        }
        public ID3D11PixelShader GetPixelShader(DeviceResources deviceResources)
        {
            return pixelShader ??= deviceResources.device.CreatePixelShader(data);
        }
        public Blob data;
        public ID3D11PixelShader pixelShader;

        public void Dispose()
        {
            pixelShader?.Dispose();
            pixelShader = null;
        }
    }
}
