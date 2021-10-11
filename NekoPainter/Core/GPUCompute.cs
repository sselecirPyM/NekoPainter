using CanvasRendering;
using NekoPainter.Data;
using NekoPainter.Util;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace NekoPainter.Core
{
    public class GPUCompute : IGPUCompute, IDisposable
    {
        public LivedNekoPainterDocument document;

        public DeviceResources deviceResources { get => document.DeviceResources; }

        int dtexCount = 0;
        public List<RenderTexture> _texture2Ds = new List<RenderTexture>();
        public List<Texture2D> texture2Ds = new List<Texture2D>();
        int dtbufCount = 0;
        public List<StreamedBuffer> tbuffers = new List<StreamedBuffer>();

        public Dictionary<string, ComputeShdaerCache> shaderCaches = new Dictionary<string, ComputeShdaerCache>();
        public Dictionary<string, object> shaderParameter = new Dictionary<string, object>();
        public string computeShaderName;

        public void SetComputeShader(string name)
        {
            computeShaderName = name;
        }

        public void SetTexture(string name, ITexture2D texture)
        {
            shaderParameter[name] = texture;
        }

        public void SetBuffer(string name, byte[] buffer)
        {
            shaderParameter[name] = buffer;
        }

        public void SetParameter(string name, object parameter)
        {
            shaderParameter[name] = parameter;
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
                for (int i = 0; i < shaderDef.parameters.Count; i++)
                {
                    var param = shaderDef.parameters[i];
                    if (param.type.Contains("RWTexture2D"))
                    {
                        shaderCache1.uav[param.name] = dUav;
                        code1.Append(param.type);
                        code1.Append(' ');
                        code1.Append(param.name);
                        code1.Append(":register(u");
                        code1.Append(dUav);
                        code1.Append(");\n");
                        dUav++;
                    }
                    else if (param.type.Contains("Texture2D") || param.type.Contains("StructuredBuffer"))
                    {
                        shaderCache1.srv[param.name] = dSrv;
                        code1.Append(param.type);
                        code1.Append(' ');
                        code1.Append(param.name);
                        code1.Append(":register(t");
                        code1.Append(dSrv);
                        code1.Append(");\n");
                        dSrv++;
                    }
                    else if (param.type == "float" || param.type == "float2" || param.type == "float3" || param.type == "float4" ||
                    param.type == "int" || param.type == "int2" || param.type == "int3" || param.type == "int4")
                    {
                        shaderParams.Append(param.type);
                        shaderParams.Append(' ');
                        shaderParams.Append(param.name);
                        shaderParams.Append(";\n");
                        shaderCache1.cbv0.Add(param);
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
                }
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
            foreach (var paramdef in shaderCache.cbv0)
            {
                bool a = shaderParameter.TryGetValue(paramdef.name, out object param1);
                if (paramdef.type == "float")
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
                if (paramdef.type == "int")
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

        public StreamedBuffer GetBuffer(byte[] data, int stride)
        {
            StreamedBuffer buffer = null;
            if (dtbufCount < tbuffers.Count)
            {
                buffer = tbuffers[dtbufCount];
            }
            else
            {
                buffer = new StreamedBuffer();
            }
            dtbufCount++;
            var writer = buffer.Begin();
            writer.Write(data);
            return buffer;
        }

        public ITexture2D GetTemporaryTexture()
        {
            Texture2D tex = null;
            if (dtexCount < texture2Ds.Count)
            {
                tex = texture2Ds[dtexCount];
            }
            else
            {
                RenderTexture _tex = new RenderTexture(deviceResources, document.Width, document.Height, Vortice.DXGI.Format.R32G32B32A32_Float, false);
                _texture2Ds.Add(_tex);
                texture2Ds.Add(new Texture2D() { _texture = _tex, });
                tex = texture2Ds[dtexCount];
            }
            tex._texture.Clear();
            dtexCount++;
            return tex;
        }

        internal void RecycleTemplateTextures()
        {
            dtexCount = 0;
            dtbufCount = 0;
            shaderParameter.Clear();
        }

        public void Dispose()
        {

        }
    }

    public class ComputeShdaerCache
    {
        public Int3 numthreads;
        public string path;
        public Dictionary<string, int> srv = new Dictionary<string, int>();
        public Dictionary<string, int> uav = new Dictionary<string, int>();
        public Dictionary<string, int> cbv = new Dictionary<string, int>();
        public ComputeShader shader;
        public List<ScriptNodeParamDef> cbv0 = new List<ScriptNodeParamDef>();
        public StreamedBuffer cbv0Buffer = new StreamedBuffer();
    }
}
