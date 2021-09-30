﻿using System;
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

        public Dictionary<string, float> fParams;
        public Dictionary<string, int> iParams;
        public Dictionary<string, Vector2> f2Params;
        public Dictionary<string, Vector3> f3Params;
        public Dictionary<string, Vector4> f4Params;

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
