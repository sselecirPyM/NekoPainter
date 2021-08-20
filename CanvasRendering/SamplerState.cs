using System;
using System.Collections.Generic;
using System.Text;

namespace CanvasRendering
{
    public struct SamplerState : IEquatable<SamplerState>
    {
        public int magFilter;
        public int minFilter;
        public int wrapS;
        public int wrapT;

        public override bool Equals(object obj)
        {
            return obj is SamplerState state && Equals(state);
        }

        public bool Equals(SamplerState other)
        {
            return magFilter == other.magFilter &&
                   minFilter == other.minFilter &&
                   wrapS == other.wrapS &&
                   wrapT == other.wrapT;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(magFilter, minFilter, wrapS, wrapT);
        }

        public static bool operator ==(SamplerState left, SamplerState right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(SamplerState left, SamplerState right)
        {
            return !(left == right);
        }
    }
}
