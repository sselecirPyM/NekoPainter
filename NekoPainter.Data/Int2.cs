﻿using System;
using System.Collections.Generic;
using System.Text;

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
    }
}