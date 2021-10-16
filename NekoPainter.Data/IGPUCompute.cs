using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NekoPainter.Data
{
    public interface IGPUCompute
    {
        public ITexture2D GetTemporaryTexture();
        public void SetComputeShader(string name);
        public void SetParameter(string name, object parameter);
        public void SetBuffer<T>(string name, T[] buffer) where T : unmanaged;
        public void SetTexture(string name, ITexture2D texture);
        public void Copy(ITexture2D target, ITexture2D source);

        public void For(int xFrom, int xTo);
        public void For(int xFrom, int xTo, int yFrom, int yTo);
        public void For(int xFrom, int xTo, int yFrom, int yTo, int zFrom, int zTo);
    }
}
