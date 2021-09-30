using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using NekoPainter.FileFormat;
using Newtonsoft.Json;

namespace NekoPainter.Util
{
    public static class StringConvert
    {
        static JsonConverter[] converters = new[] { new VectorConverter() };
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float GetFloat(string input)
        {
            float.TryParse(input, out float f);
            return f;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector2 GetFloat2(string input)
        {
            if (input == null) return new Vector2();
            return JsonConvert.DeserializeObject<Vector2>(input, converters);
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector3 GetFloat3(string input)
        {
            if (input == null) return new Vector3();
            return JsonConvert.DeserializeObject<Vector3>(input, converters);
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector4 GetFloat4(string input)
        {
            if (input == null) return new Vector4();
            return JsonConvert.DeserializeObject<Vector4>(input, converters);
        }
    }
}
