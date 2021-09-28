using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NekoPainter.Util
{
    public static class StructExt
    {
        public static bool MakeEqual<T>(ref this T structure1, T another) where T : struct
        {
            if (structure1.Equals(another))
            {
                return false;
            }
            structure1 = another;
            return true;
        }
    }
}
