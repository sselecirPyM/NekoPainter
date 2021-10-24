using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NekoPainter.Data
{
    public struct SamplerStateDef : IEquatable<SamplerStateDef>
    {
        public AddressModeDef AddressU;
        public AddressModeDef AddressV;
        public AddressModeDef AddressW;
        public FilterDef filter;

        public override bool Equals(object obj)
        {
            return obj is SamplerStateDef def && Equals(def);
        }

        public bool Equals(SamplerStateDef other)
        {
            return AddressU == other.AddressU &&
                   AddressV == other.AddressV &&
                   AddressW == other.AddressW &&
                   filter == other.filter;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(AddressU, AddressV, AddressW, filter);
        }

        public static bool operator ==(SamplerStateDef left, SamplerStateDef right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(SamplerStateDef left, SamplerStateDef right)
        {
            return !(left == right);
        }
    }
    public enum FilterDef
    {
        MinMagMipPoint = 0,
        MinMagPointMipLinear = 1,
        MinPointMagLinearMipPoint = 4,
        MinPointMagMipLinear = 5,
        MinLinearMagMipPoint = 16,
        MinLinearMagPointMipLinear = 17,
        MinMagLinearMipPoint = 20,
        MinMagMipLinear = 21,
        Anisotropic = 85,
        ComparisonMinMagMipPoint = 128,
        ComparisonMinMagPointMipLinear = 129,
        ComparisonMinPointMagLinearMipPoint = 132,
        ComparisonMinPointMagMipLinear = 133,
        ComparisonMinLinearMagMipPoint = 144,
        ComparisonMinLinearMagPointMipLinear = 145,
        ComparisonMinMagLinearMipPoint = 148,
        ComparisonMinMagMipLinear = 149,
        ComparisonAnisotropic = 213,
        MinimumMinMagMipPoint = 256,
        MinimumMinMagPointMipLinear = 257,
        MinimumMinPointMagLinearMipPoint = 260,
        MinimumMinPointMagMipLinear = 261,
        MinimumMinLinearMagMipPoint = 272,
        MinimumMinLinearMagPointMipLinear = 273,
        MinimumMinMagLinearMipPoint = 276,
        MinimumMinMagMipLinear = 277,
        MinimumAnisotropic = 341,
        MaximumMinMagMipPoint = 384,
        MaximumMinMagPointMipLinear = 385,
        MaximumMinPointMagLinearMipPoint = 388,
        MaximumMinPointMagMipLinear = 389,
        MaximumMinLinearMagMipPoint = 400,
        MaximumMinLinearMagPointMipLinear = 401,
        MaximumMinMagLinearMipPoint = 404,
        MaximumMinMagMipLinear = 405,
        MaximumAnisotropic = 469
    }

    public enum AddressModeDef
    {
        Wrap = 1,
        Mirror = 2,
        Clamp = 3,
        Border = 4,
        MirrorOnce = 5
    }
}
