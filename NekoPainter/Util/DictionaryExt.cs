using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NekoPainter.Util
{
    public static class DictionaryExt
    {
        public static T GetOrCreate<T1, T>(this Dictionary<T1, T> dict, T1 key) where T :new()
        {
            if(dict.TryGetValue(key, out T v))
            {
                return v;
            }
            v = new T();
            dict[key] = v;
            return v;
        }
    }
}
