using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NekoPainter.Data
{
    public struct Int3 : IEquatable<Int3>
    {
        public int X;
        public int Y;
        public int Z;

        public Int3(int x,int y,int z)
        {
            X = x;
            Y = y;
            Z = z;
        }

        public override bool Equals(object obj)
        {
            return obj is Int3 @int && Equals(@int);
        }

        public bool Equals(Int3 other)
        {
            return X == other.X &&
                   Y == other.Y &&
                   Z == other.Z;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(X, Y, Z);
        }

        public static bool operator ==(Int3 left, Int3 right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(Int3 left, Int3 right)
        {
            return !(left == right);
        }
    }
}
