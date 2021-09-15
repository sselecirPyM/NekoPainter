using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace NekoPainter.Nodes
{
    public class Paint2DNode : Node
    {
        public string brushPath;
        public Vector4 color;
        public Vector4 color2;
        public Vector4 color3;
        public Vector4 color4;
        public float size;
        public Dictionary<string, float[]> parameters;

        public override Node Clone()
        {
            Paint2DNode node = (Paint2DNode)base.Clone();

            return node;
        }
    }
}
