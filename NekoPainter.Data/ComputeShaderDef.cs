using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NekoPainter.Data
{
    public class ComputeShaderDef
    {
        public string name;
        public string displayName;
        public string path;
        public string[] includes;
        public string entry;
        public Dictionary<string, ComputeShaderParamDef> parameters;
    }
    public class ComputeShaderParamDef
    {
        public string type;

    }
}
