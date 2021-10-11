using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Numerics;

namespace NekoPainter.Data
{
    public class NPParticles
    {
        public List<Vector3> position;
        public List<Quaternion> rotation;
        public List<Vector3> scale;
        public List<float> life;
        public List<float> lifeRemain;
        public List<Vector3> speed;
        public List<Vector4> color;
        public List<int> variation;
        public float updatedTime;

        static void RemoveX<T>(List<T> list1, int index)
        {
            if (list1 == null) return;
            list1[index] = list1[list1.Count - 1];
            list1.RemoveAt(list1.Count - 1);
        }

        public void RemoveParticleX(int index)
        {
            RemoveX(position, index);
            RemoveX(rotation, index);
            RemoveX(scale, index);
            RemoveX(life, index);
            RemoveX(lifeRemain, index);
            RemoveX(speed, index);
            RemoveX(color, index);
            RemoveX(variation, index);
        }

        public void Clear()
        {
            position?.Clear();
            rotation?.Clear();
            scale?.Clear();
            life?.Clear();
            lifeRemain?.Clear();
            speed?.Clear();
            color?.Clear();
            variation?.Clear();
        }
    }
}
