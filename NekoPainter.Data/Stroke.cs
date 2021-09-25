using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Numerics;

namespace NekoPainter.Data
{
    public class Stroke
    {
        public List<Vector2> position;
        public List<float> deltaTime;
        public List<float> presure;
        public DateTime startTime;

        public Stroke Clone()
        {
            Stroke stroke = (Stroke)this.MemberwiseClone();
            stroke.position = new List<Vector2>(position);
            stroke.deltaTime = new List<float>(deltaTime);
            stroke.presure = new List<float>(presure);
            return stroke;
        }
    }
}
