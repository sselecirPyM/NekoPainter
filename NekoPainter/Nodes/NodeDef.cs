using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NekoPainter.Nodes
{
    public class NodeDef
    {
        public Dictionary<string, List<SocketDef>> socketDefs;
    }
    public class SocketDef
    {
        public string name;
        public string socketType;
        public string displayName;
    }
}
