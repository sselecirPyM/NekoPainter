using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Numerics;
using System.Runtime.InteropServices;

namespace NekoPainter.Data
{
    public class Texture2D
    {
        public int width;
        public int height;
        public string name;
        public float scale = 1.0f;
        [NonSerialized]
        public byte[] data;

        [NonSerialized]
        public CanvasRendering.RenderTexture _texture;

        public Span<Vector4> GetRawTexture()
        {
            data = _texture.GetData();
            return MemoryMarshal.Cast<byte, Vector4>(data);
        }

        public byte[] GetRawTexture1()
        {
            data = _texture.GetData();
            return data;
        }

        public void EndModification()
        {
            _texture.UpdateTexture<byte>(data);
        }
    }
}
