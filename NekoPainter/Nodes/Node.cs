using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace NekoPainter.Nodes
{
    public class Node
    {
        public int Luid;
        public Vector2 Position;

        public Dictionary<string, NodeSocket> Inputs;
        public Dictionary<string, HashSet<NodeSocket>> Outputs;

        public DateTime creationTime;
        public DateTime modificationTime;

        public bool canCache;

        public StrokeNode strokeNode;
        public Paint2DNode paint2DNode;
        public ScriptNode scriptNode;

        public string GetNodeTypeName()
        {
            if (strokeNode != null) return "strokeNode";
            if (paint2DNode != null) return "paint2DNode";
            if (scriptNode != null) return scriptNode.nodeName;
            return string.Empty;
        }

        public virtual Node Clone()
        {
            Node clone = (Node)MemberwiseClone();
            return clone;
        }
    }
}
