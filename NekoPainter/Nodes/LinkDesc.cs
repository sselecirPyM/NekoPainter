using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NekoPainter.Nodes
{
    public struct LinkDesc
    {
        public int outputNode;
        public string outputSocket;
        public int inputNode;
        public string inputSocket;

        public override bool Equals(object obj)
        {
            return obj is LinkDesc desc &&
                   outputNode == desc.outputNode &&
                   outputSocket == desc.outputSocket &&
                   inputNode == desc.inputNode &&
                   inputSocket == desc.inputSocket;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(outputNode, outputSocket, inputNode, inputSocket);
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
