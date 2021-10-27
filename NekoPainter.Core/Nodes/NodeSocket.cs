using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NekoPainter.Core.Nodes
{
    public struct NodeSocket
    {
        public int targetUid;
        public string targetSocket;

        public override bool Equals(object obj)
        {
            return obj is NodeSocket socket &&
                   targetUid == socket.targetUid &&
                   targetSocket == socket.targetSocket;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(targetUid, targetSocket);
        }
    }
}
