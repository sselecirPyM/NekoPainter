using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Numerics;
using Newtonsoft.Json;

namespace NekoPainter.FileFormat
{
    public class VectorConverter : JsonConverter
    {
        public override bool CanConvert(Type objectType)
        {
            if (objectType == typeof(Vector2) || objectType == typeof(Vector3) || objectType == typeof(Vector4))
            {
                return true;
            }
            return false;
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            float[] a = (float[])serializer.Deserialize(reader, typeof(float[]));
            if (objectType == typeof(Vector2))
                return new Vector2(a[0], a[1]);
            if (objectType == typeof(Vector3))
                return new Vector3(a[0], a[1], a[2]);
            if (objectType == typeof(Vector4))
                return new Vector4(a[0], a[1], a[2], a[3]);
            throw new NotImplementedException();
        }

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            if (value is Vector2 v2)
                serializer.Serialize(writer, new float[] { v2.X, v2.Y });
            else if (value is Vector3 v3)
                serializer.Serialize(writer, new float[] { v3.X, v3.Y, v3.Z });
            else if (value is Vector4 v4)
                serializer.Serialize(writer, new float[] { v4.X, v4.Y, v4.Z, v4.W });
            else throw new NotImplementedException();
        }
    }
}
