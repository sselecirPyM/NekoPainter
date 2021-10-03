using System;
using System.Collections.Generic;
using System.Text;
using System.Drawing;

namespace NekoPainter.Data
{
    public struct Int2
    {
        public int X;
        public int Y;
        public Int2(int x,int y)
        {
            X = x;
            Y = y;
        }

        public override bool Equals(object obj)
        {
            return obj is Int2 @int &&
                   X == @int.X &&
                   Y == @int.Y;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(X, Y);
        }

        public static bool operator ==(Int2 left, Int2 right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(Int2 left, Int2 right)
        {
            return !(left == right);
        }

        public static implicit operator Point(Int2 i2)
        {
            return new Point(i2.X, i2.Y);
        }

        public static implicit operator Int2(Point point)
        {
            return new Int2(point.X, point.Y);
        }
    }
}
