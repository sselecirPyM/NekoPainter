using System;
using System.Collections.Generic;
using System.Text;
using Vortice.Direct3D11;
using Vortice.Direct3D;
using Vortice.D3DCompiler;

namespace CanvasRendering
{
    public class ComputeShader
    {
        public static ComputeShader CompileAndCreate(DeviceResources deviceResources, byte[] source)
        {
            return CompileAndCreate(deviceResources, source, "main");
        }
        public static ComputeShader CompileAndCreate(DeviceResources deviceResources, byte[] source, string entryPoint)
        {
            ComputeShader computeShader = new ComputeShader();
            var hr = Compiler.Compile(source, entryPoint, null, "cs_5_0", out Blob data, out Blob errorBlob);
            computeShader.computeShader = deviceResources.device.CreateComputeShader(data);
            computeShader.DeviceResources = deviceResources;
            return computeShader;
        }

        public void SetSRV(ComputeBuffer buffer, int slot)
        {
            DeviceResources.d3dContext.CSSetShaderResource(slot, buffer.srv);
        }

        public void SetSRV(RenderTexture renderTexture, int slot)
        {
            DeviceResources.d3dContext.CSSetShaderResource(slot, renderTexture.srv);
        }

        public void SetUAV(ComputeBuffer buffer, int slot)
        {
            DeviceResources.d3dContext.CSSetUnorderedAccessView(slot, buffer.uav);
        }

        public void SetUAV(RenderTexture renderTexture, int slot)
        {
            DeviceResources.d3dContext.CSSetUnorderedAccessView(slot, renderTexture.uav);
        }

        public void SetCBV(ConstantBuffer constantBuffer, int slot)
        {
            DeviceResources.d3dContext.CSSetConstantBuffer(slot, constantBuffer.m_buffer);
        }

        public void SetCBV(ConstantBuffer constantBuffer, int slot, int ofs, int size)
        {
            DeviceResources.d3dContext.CSSetConstantBuffer1(slot, constantBuffer.m_buffer, new[] { ofs / 16 }, new[] { size / 16 });
        }

        public void Dispatch(int x, int y, int z)
        {
            DeviceResources.d3dContext.CSSetShader(computeShader);
            DeviceResources.d3dContext.Dispatch(x, y, z);
            //DeviceResources.d3dContext.CSSetShaderResource(0, null);
            DeviceResources.d3dContext.CSSetUnorderedAccessView(0, null);

        }

        public void Dispose()
        {

        }

        public ID3D11ComputeShader computeShader;
        public DeviceResources DeviceResources { get; private set; }
    }
}
