using NekoPainter.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NekoPainter.Core
{
    public class GPUCompute
    {

        public Dictionary<string, object> shaderParameter = new Dictionary<string, object>();
        public string computeShaderName;

        public void SetTexture(Texture2D texture, string name)
        {

        }
        public void SetComputeShader(string name)
        {

        }

        public void SetBuffer(byte[] buffer, string name)
        {

        }

        public void Dispatch(int x, int y, int z)
        {

        }
    }
}
