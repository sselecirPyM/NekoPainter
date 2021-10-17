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
        public FileNode fileNode;
        public ScriptNode scriptNode;

        public Dictionary<string, float> fParams;
        public Dictionary<string, int> iParams;
        public Dictionary<string, Vector2> f2Params;
        public Dictionary<string, Vector3> f3Params;
        public Dictionary<string, Vector4> f4Params;
        public Dictionary<string, bool> bParams;
        public Dictionary<string, string> sParams;

        public string GetNodeTypeName()
        {
            if (strokeNode != null) return "strokeNode";
            if (fileNode != null) return "fileNode";
            if (scriptNode != null) return scriptNode.nodeName;
            return string.Empty;
        }

        public virtual Node Clone()
        {
            Node clone = (Node)MemberwiseClone();
            clone.fParams = GetClone(clone.fParams);
            clone.iParams = GetClone(clone.iParams);
            clone.f2Params = GetClone(clone.f2Params);
            clone.f3Params = GetClone(clone.f3Params);
            clone.f4Params = GetClone(clone.f4Params);
            clone.bParams = GetClone(clone.bParams);
            clone.sParams = GetClone(clone.sParams);

            return clone;
        }

        static Dictionary<string, T> GetClone<T>(Dictionary<string, T> a)
        {
            if (a != null) return new Dictionary<string, T>(a);
            else return null;
        }
    }
}
