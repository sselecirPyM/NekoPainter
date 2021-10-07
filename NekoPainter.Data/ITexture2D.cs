using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace NekoPainter.Data
{
    public interface ITexture2D
    {
        int width { get; }
        int height { get; }

        public Span<Vector4> GetRawTexture();

        public byte[] GetRawTexture1();

        public void EndModification();

        public void UpdateTexture<T>(Span<T> data) where T : unmanaged;
    }
}
