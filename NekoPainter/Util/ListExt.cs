using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NekoPainter.Util
{
    public static class ListExt
    {
        public static void Move<T>(this IList<T> list, int from, int to)
        {
            var p1 = list[from];
            if (from < to)
            {
                for (int i = from; i < to; i++)
                {
                    list[i] = list[i + 1];
                }
            }
            if (from > to)
            {
                for (int i = from; i > to; i--)
                {
                    list[i] = list[i - 1];
                }
            }

            list[to] = p1;
        }
    }
}
