using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NekoPainter.Core.Nodes
{
    public struct LinkDesc
    {
        public int outputNode;
        public string outputName;
        public int inputNode;
        public string inputName;

        public override bool Equals(object obj)
        {
            return obj is LinkDesc desc &&
                   outputNode == desc.outputNode &&
                   outputName == desc.outputName &&
                   inputNode == desc.inputNode &&
                   inputName == desc.inputName;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(outputNode, outputName, inputNode, inputName);
        }

        public static bool operator ==(LinkDesc left, LinkDesc right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(LinkDesc left, LinkDesc right)
        {
            return !(left == right);
        }
    }
}
