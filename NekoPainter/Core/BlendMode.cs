using NekoPainter.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NekoPainter.Core
{
    public class BlendMode
    {
        public string name;
        public string displayName;
        public string description = "";
        public Guid guid;
        public List<ScriptNodeParamDef> parameters;
        public string script;
    }
}
