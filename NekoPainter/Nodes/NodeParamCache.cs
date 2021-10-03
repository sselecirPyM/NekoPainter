using NekoPainter.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NekoPainter.Nodes
{
    public class NodeParamCache
    {
        public Dictionary<string, object> outputCache = new Dictionary<string, object>();
        public bool valid;
    }
}
