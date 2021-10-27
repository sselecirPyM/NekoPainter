using CanvasRendering;
using NekoPainter.Data;
using NekoPainter.Core.Util;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using Vortice.Direct3D11;

namespace NekoPainter.Core
{
    public class GPUCompute : IGPUCompute, IDisposable
    {
        public LivedNekoPainterDocument document;

        public DeviceResources deviceResources;

        public LinearPool<StreamedBuffer> tbuffers = new LinearPool<StreamedBuffer>();

        public Dictionary<Int2, LinearPool<Texture2D>> texture2Ds2 = new Dictionary<Int2, LinearPool<Texture2D>>();

        public Dictionary<string, ComputeShdaerCache> shaderCaches = new Dictionary<string, ComputeShdaerCache>();
        public Dictionary<string, object> shaderParameter = new Dictionary<string, object>();
        public Dictionary<SamplerStateDef, ID3D11SamplerState> samplers = new Dictionary<SamplerStateDef, ID3D11SamplerState>();
        public string computeShaderName;

        public void SetComputeShader(string name)
        {
            computeShaderName = name;
        }

        public void SetTexture(string name, ITexture2D texture)
        {
            shaderParameter[name] = texture;
        }

        public void SetBuffer<T>(string name, T[] buffer) where T : unmanaged
        {
            shaderParameter[name] = buffer;
        }

        public void SetParameter(string name, object parameter)
        {
            shaderParameter[name] = parameter;
        }

        public void Copy(ITexture2D target, ITexture2D source)
        {
            ((Texture2D)source)._texture.CopyTo(((Texture2D)target)._texture);
        }

        public void For(int xFrom, int xTo)
        {
            For(xFrom, xTo, 0, 1, 0, 1);
        }

        public void For(int xFrom, int xTo, int yFrom, int yTo)
        {
            For(xFrom, xTo, yFrom, yTo, 0, 1);
        }

        public void For(int xFrom, int xTo, int yFrom, int yTo, int zFrom, int zTo)
        {
            if (xFrom >= xTo || yFrom >= yTo || zFrom >= zTo)
            {
                return;
            }
            var shaderDef = document.shaderDefs[computeShaderName];
            string shaderCode = document.shaders[shaderDef.path];

            int x = xTo - xFrom;
            int y = yTo - yFrom;
            int z = zTo - zFrom;

            ComputeShdaerCache shaderCache = shaderCaches.GetOrCreate(computeShaderName, () =>
            {
                var shaderCache1 = new ComputeShdaerCache();
                StringBuilder micros = new StringBuilder();
                micros.Append("#define NPGetDimensions(PARAM1) (_NP_DIMENSIONS_##PARAM1)\n");
                StringBuilder shaderParams = new StringBuilder();
                shaderParams.Append("cbuffer NP_PARAMS : register (b0)\n{\nint3 NP_WORKSIZE;\nint3 NP_WORKOFFSET;\nint NP_UNDEFINE1;\n");
                StringBuilder code1 = new StringBuilder();
                int dUav = 0;
                int dSrv = 0;
                int dSampler = 0;
                for (int i = 0; i < shaderDef.parameters.Count; i++)
                {
                    var paramDef = shaderDef.parameters[i];
                    if (paramDef.type.Contains("RWTexture2D"))
                    {
                        shaderCache1.uav[paramDef.name] = dUav;
                        code1.Append(paramDef.type);
                        code1.Append(' ');
                        code1.Append(paramDef.name);
                        code1.Append(":register(u");
                        code1.Append(dUav);
                        code1.Append(");\n");
                        shaderParams.Append("int4 _NP_DIMENSIONS_");
                        shaderParams.Append(paramDef.name);
                        shaderParams.Append(";\n");
                        dUav++;
                    }
                    else if (paramDef.type.Contains("Texture2D") || paramDef.type.Contains("StructuredBuffer"))
                    {
                        shaderCache1.srv[paramDef.name] = dSrv;
                        code1.Append(paramDef.type);
                        code1.Append(' ');
                        code1.Append(paramDef.name);
                        code1.Append(":register(t");
                        code1.Append(dSrv);
                        code1.Append(");\n");
                        shaderParams.Append("int4 _NP_DIMENSIONS_");
                        shaderParams.Append(paramDef.name);
                        shaderParams.Append(";\n");
                        dSrv++;
                    }
                    else if (paramDef.type == "SamplerState")
                    {
                        shaderCache1.sampler[paramDef.name] = dSampler;
                        code1.Append(paramDef.type);
                        code1.Append(' ');
                        code1.Append(paramDef.name);
                        code1.Append(":register(s");
                        code1.Append(dSampler);
                        code1.Append(");\n");
                        dSampler++;
                    }
                    else if (paramDef.type == "float" || paramDef.type == "float2" || paramDef.type == "float3" || paramDef.type == "float4" ||
                    paramDef.type == "int" || paramDef.type == "int2" || paramDef.type == "int3" || paramDef.type == "int4")
                    {
                        shaderParams.Append(paramDef.type);
                        shaderParams.Append(' ');
                        shaderParams.Append(paramDef.name);
                        shaderParams.Append(";\n");
                        //shaderCache1.cbv0.Add(paramDef);
                    }
                }
                shaderParams.Append("\n}\n");
                code1.Append(shaderCode);
                code1.Append(string.Format("[numthreads(16,1,1)]\nvoid main(int3 DTid : SV_DispatchThreadID){0}if(DTid.x < {4}.x&&DTid.y < {4}.y&&DTid.z < {4}.z){2}(DTid + {3});{1}", '{', '}', shaderDef.entry, "NP_WORKOFFSET", "NP_WORKSIZE"));
                string finalCode = micros + shaderParams.ToString() + code1.ToString();
                shaderCache1.shader = ComputeShader.CompileAndCreate(deviceResources, Encoding.UTF8.GetBytes(finalCode), "main");
                shaderCache1.numthreads = new Int3(16, 1, 1);
                return shaderCache1;
            });
            ComputeShader shader = shaderCache.shader;
            foreach (var paramdef in shaderCache.uav)
            {
                if (shaderParameter.TryGetValue(paramdef.Key, out object uav1))
                {
                    if (uav1 is Texture2D tex2d)
                        shader.SetUAV(tex2d._texture, paramdef.Value);
                }
            }
            foreach (var paramdef in shaderCache.srv)
            {
                if (shaderParameter.TryGetValue(paramdef.Key, out object srv1))
                {
                    if (srv1 is Texture2D tex2d)
                        shader.SetSRV(tex2d._texture, paramdef.Value);
                    else if (srv1 is byte[] bbuffer)
                    {
                        int stride = shaderDef.parameters.Find(u => u.name == paramdef.Key).stride;
                        var buf = GetBuffer(bbuffer, stride);
                        shader.SetSRV(buf.GetComputeBuffer(deviceResources, stride), paramdef.Value);
                    }
                    else if (srv1 is float[] fbuffer)
                    {
                        int stride = shaderDef.parameters.Find(u => u.name == paramdef.Key).stride;
                        var buf = GetBuffer(fbuffer, stride);
                        shader.SetSRV(buf.GetComputeBuffer(deviceResources, stride), paramdef.Value);
                    }
                    else if (srv1 is int[] ibuffer)
                    {
                        int stride = shaderDef.parameters.Find(u => u.name == paramdef.Key).stride;
                        var buf = GetBuffer(ibuffer, stride);
                        shader.SetSRV(buf.GetComputeBuffer(deviceResources, stride), paramdef.Value);
                    }
                }
            }
            foreach (var paramdef in shaderCache.sampler)
            {
                SamplerStateDef samplerStateDef = new SamplerStateDef();
                if (shaderParameter.TryGetValue(paramdef.Key, out object sampler1))
                {
                    samplerStateDef = (SamplerStateDef)sampler1;
                }
                var samplerState1 = GetSampler(samplerStateDef);
                deviceResources.d3dContext.CSSetSampler(paramdef.Value, samplerState1);
            }
            var cbv0Buffer = shaderCache.cbv0Buffer;
            var writer = cbv0Buffer.Begin();
            writer.Write(x);
            writer.Write(y);
            writer.Write(z);
            writer.Write(0);
            writer.Write(xFrom);
            writer.Write(yFrom);
            writer.Write(zFrom);
            writer.Write(0);
            int currentOffset = 0;
            void GetSpacing(int sizeX)
            {
                int c = (currentOffset & 15);
                if (c != 0 && c + sizeX > 16)
                {
                    int d = 16 - c;
                    for (int i = 0; i < d; i++)
                        writer.Write((byte)0);
                    currentOffset += d;
                }
                currentOffset += sizeX;
            }
            foreach (var paramdef in shaderDef.parameters)
            {
                bool a = shaderParameter.TryGetValue(paramdef.name, out object param1);
                if (paramdef.type.Contains("Texture2D"))
                {
                    GetSpacing(16);
                    writer.Write(((ITexture2D)param1).width);
                    writer.Write(((ITexture2D)param1).height);
                    writer.Write((int)1);
                    writer.Write((int)1);
                }
                else if (paramdef.type.Contains("StructuredBuffer"))
                {
                    GetSpacing(16);
                    writer.Write((int)1);
                    writer.Write((int)1);
                    writer.Write((int)1);
                    writer.Write((int)1);
                }
                else if (paramdef.type == "float")
                {
                    GetSpacing(4);
                    writer.Write((float)(a ? param1 : paramdef.defaultValue1));
                }
                else if (paramdef.type == "float2")
                {
                    GetSpacing(8);
                    writer.Write((Vector2)(a ? param1 : paramdef.defaultValue1));
                }
                else if (paramdef.type == "float3")
                {
                    GetSpacing(12);
                    writer.Write((Vector3)(a ? param1 : paramdef.defaultValue1));
                }
                else if (paramdef.type == "float4")
                {
                    GetSpacing(16);
                    writer.Write((Vector4)(a ? param1 : paramdef.defaultValue1));
                }
                else if (paramdef.type == "int")
                {
                    GetSpacing(4);
                    writer.Write((int)(a ? param1 : paramdef.defaultValue1));
                }
                else if (paramdef.type == "int2")
                {
                    GetSpacing(8);
                    writer.Write((Int2)(a ? param1 : paramdef.defaultValue1));
                }
                else if (paramdef.type == "int3")
                {
                    GetSpacing(12);
                    writer.Write((Int3)(a ? param1 : paramdef.defaultValue1));
                }
                else if (paramdef.type == "int4")
                {
                    GetSpacing(16);
                    writer.Write((Int4)(a ? param1 : paramdef.defaultValue1));
                }
            }
            shader.SetCBV(cbv0Buffer.GetBuffer(deviceResources), 0);
            Int3 nts = shaderCache.numthreads;
            shader.Dispatch((x + nts.X - 1) / nts.X, (y + nts.Y - 1) / nts.Y, (z + nts.Z - 1) / nts.Z);
        }

        public StreamedBuffer GetBuffer<T>(T[] data, int stride) where T : unmanaged
        {
            StreamedBuffer buffer = tbuffers.Get(() =>
            {
                return new StreamedBuffer();
            });
            var writer = buffer.Begin();
            writer.Write(data);
            return buffer;
        }

        public ITexture2D GetTemporaryTexture()
        {
            return GetTemporaryTexture(document.Width, document.Height);
        }

        public ITexture2D GetTemporaryTexture(int width, int height)
        {
            var pool = texture2Ds2.GetOrCreate(new Int2(width, height));
            Texture2D tex = pool.Get(() =>
            {
                RenderTexture _tex = new RenderTexture(deviceResources, width, height, Vortice.DXGI.Format.R32G32B32A32_Float, false);
                return new Texture2D() { _texture = _tex, };
            });
            tex._texture.Clear();
            return tex;
        }

        internal ID3D11SamplerState GetSampler(SamplerStateDef samplerStateDef)
        {
            return samplers.GetOrCreate(samplerStateDef, u =>
            {
                var state = deviceResources.device.CreateSamplerState(new SamplerDescription((Filter)u.filter, (TextureAddressMode)u.AddressU, (TextureAddressMode)u.AddressV, (TextureAddressMode)u.AddressW));
                return state;
            });
        }

        internal void RecycleTemplateTextures()
        {
            tbuffers.Reset();
            shaderParameter.Clear();
            foreach (var texPool in texture2Ds2)
            {
                texPool.Value.Reset();
            }
        }

        public void Dispose()
        {
            foreach (var texPool in texture2Ds2)
            {
                foreach (var tex in texPool.Value.list1)
                {
                    tex._texture.Dispose();
                }
            }
            texture2Ds2.Clear();
            foreach (var state in samplers)
            {
                state.Value.Dispose();
            }
            samplers.Clear();
        }
    }

    public class ComputeShdaerCache
    {
        public Int3 numthreads;
        public string path;
        public Dictionary<string, int> srv = new Dictionary<string, int>();
        public Dictionary<string, int> uav = new Dictionary<string, int>();
        public Dictionary<string, int> cbv = new Dictionary<string, int>();
        public Dictionary<string, int> sampler = new Dictionary<string, int>();
        public ComputeShader shader;
        //public List<ScriptNodeParamDef> cbv0 = new List<ScriptNodeParamDef>();
        public StreamedBuffer cbv0Buffer = new StreamedBuffer();
    }
}
