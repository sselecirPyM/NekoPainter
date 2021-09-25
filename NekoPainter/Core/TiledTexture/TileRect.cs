using CanvasRendering;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Numerics;
using NekoPainter.Data;

namespace NekoPainter
{
    public struct TileRect
    {
        public int maxX;
        public int maxY;
        public int minX;
        public int minY;

        public TileRect(Int2 initializationValue)
        {
            maxX = initializationValue.X;
            maxY = initializationValue.Y;
            minX = initializationValue.X;
            minY = initializationValue.Y;
        }
        public TileRect(List<Int2> parts)
        {
            if (parts == null || parts.Count == 0)
            {
                maxX = 0;
                maxY = 0;
                minX = 0;
                minY = 0;
                return;
            }
            else
            {
                maxX = parts[0].X;
                maxY = parts[0].Y;
                minX = parts[0].X;
                minY = parts[0].Y;
            }
            for (int i = 0; i < parts.Count; i++)
            {
                Expand(parts[i]);
            }
        }

        public bool HaveIntersections(TileRect other)
        {
            int dMaxX = maxX < other.maxX ? maxX : other.maxX;
            int dMaxY = maxY < other.maxY ? maxY : other.maxY;
            int dMinX = minX > other.minX ? minX : other.minX;
            int dMinY = minY > other.minY ? minY : other.minY;
            if (dMinX > dMaxX || dMinY > dMaxY) return false;
            return true;
        }

        public bool InRange(Int2 position)
        {
            if (position.X >= minX && position.X <= maxX && position.Y >= minY && position.Y <= maxY) return true;
            return false;
        }

        public void Expand(Int2 position)
        {
            if (minX > position.X) minX = position.X;
            if (minY > position.Y) minY = position.Y;
            if (maxX < position.X) maxX = position.X;
            if (maxY < position.Y) maxY = position.Y;
        }
    }
}
