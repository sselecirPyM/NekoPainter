using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Numerics;

namespace NekoPainter.Core
{
    public class Stroke
    {
        public List<Vector2> position;
        public List<float> deltaTime;
        public List<float> presure;
        public DateTime startTime;
    }
}
