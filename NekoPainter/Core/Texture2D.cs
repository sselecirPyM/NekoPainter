using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Numerics;
using System.Runtime.InteropServices;
using NekoPainter.Data;

namespace NekoPainter.Core
{
    public class Texture2D : ITexture2D
    {
        public int width { get => _texture.width; }
        public int height { get => _texture.height; }
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

        public void UpdateTexture<T>(Span<T> data) where T : unmanaged
        {
            _texture.UpdateTexture<T>(data);
        }
    }
}
