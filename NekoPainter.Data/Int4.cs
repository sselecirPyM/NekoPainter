using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NekoPainter.Data
{
    public struct Int4 : IEquatable<Int4>
    {
        public int X;
        public int Y;
        public int Z;
        public int W;

        public Int4(int x,int y,int z,int w)
        {
            X = x;
            Y = y;
            Z = z;
            W = w;
        }

        public override bool Equals(object obj)
        {
            return obj is Int4 @int && Equals(@int);
        }

        public bool Equals(Int4 other)
        {
            return X == other.X &&
                   Y == other.Y &&
                   Z == other.Z &&
                   W == other.W;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(X, Y, Z, W);
        }

        public static bool operator ==(Int4 left, Int4 right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(Int4 left, Int4 right)
        {
            return !(left == right);
        }
    }
}
