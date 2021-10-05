using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NekoPainter.Data
{
    public class NodeContext
    {
        public Dictionary<string, string> CustomData;
        public int width;
        public int height;
        public Action<string> setComputeShader;
        public Action<byte[], string> setBuffer;
        public Action<Texture2D, string> setTexture;
        public Action<int, int, int> dispatch;
    }
}
