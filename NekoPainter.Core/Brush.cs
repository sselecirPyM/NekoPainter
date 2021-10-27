using NekoPainter.Data;
using NekoPainter.Core.Nodes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace NekoPainter.Core
{
    public class Brush
    {
        public string name;
        public string displayName;
        public string description = "";
        public List<ScriptNodeParamDef> parameters;
        public List<LinkDesc> links;
        public List<LinkDesc> attachLinks;
        public List<BrushNodeDesc> nodes;
        public int outputNode;
    }
    public class BrushNodeDesc
    {
        public string name;
        public Vector2 offset;
        public List<BrushNodeParam> parameters;
    }
    public class BrushNodeParam
    {
        public string from;
        public string name;
    }
}
