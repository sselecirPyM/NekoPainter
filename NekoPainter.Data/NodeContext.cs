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
        public int frame;
        public float frameInterval;
        public float deltaTime;
        public float currentTime;
        public PlayMode playMode;
        public IGPUCompute gpuCompute;
    }
    public enum PlayMode
    {
        Edit,
        Play,
        Record,
    }
}
