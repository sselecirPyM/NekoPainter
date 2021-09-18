using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace NekoPainter.Nodes
{
    public class Paint2DNode
    {
        public string brushPath;
        public Vector4 color;
        public Vector4 color2;
        public Vector4 color3;
        public Vector4 color4;
        public float size;
        public Dictionary<string, float[]> parameters;

        public Paint2DNode Clone()
        {
            Paint2DNode node = (Paint2DNode)MemberwiseClone();
            return node;
        }
    }
}
