using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace NekoPainter.Nodes
{
    public class Node
    {
        public int Luid;
        public Vector2 Position;

        public Dictionary<string, NodeSocket> Inputs;
        public Dictionary<string, HashSet<NodeSocket>> Outputs;

        public bool canCache;

        public StrokeNode strokeNode;
        public Paint2DNode paint2DNode;

        public virtual Node Clone()
        {
            Node clone = (Node)MemberwiseClone();
            return clone;
        }
    }
}
