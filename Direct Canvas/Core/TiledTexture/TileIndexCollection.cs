using CanvasRendering;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DirectCanvas
{
    public class TileIndexCollection
    {
        int[] _texture;
        int stride;
        int capacity;
        TileRect rect;
        public TileIndexCollection(TileRect rect)
        {
            this.rect = rect;
            stride = (rect.maxX - rect.minX) / 8 + 1;
            capacity = stride * ((rect.maxY - rect.minY + 8) / 8);
            _texture = new int[capacity];
            for (int i = 0; i < capacity; i++)
            {
                _texture[i] = -1;
            }
        }

        public TileIndexCollection(TileIndexCollection collection)
        {
            rect = collection.rect;
            stride = collection.stride;
            capacity = collection.capacity;
            _texture = new int[capacity];
            for (int i = 0; i < capacity; i++)
            {
                _texture[i] = collection._texture[i];
            }
        }

        public int this[Int2 key]
        {
            get
            {
                if (rect.InRange(key))
                {
                    return _texture[(key.X - rect.minX) / 8 % stride + (key.Y - rect.minY) / 8 * stride];
                }
                else return -1;
            }
            set
            {
                if (rect.InRange(key))
                {
                    _texture[(key.X - rect.minX) / 8 % stride + (key.Y - rect.minY) / 8 * stride] = value;
                }
                else return;
            }
        }

        public void Add(Int2 key, int value)
        {
            if (rect.InRange(key))
            {
                _texture[(key.X - rect.minX) / 8 % stride + (key.Y - rect.minY) / 8 * stride] = value;
            }
            else return;
        }

        public void Add(KeyValuePair<Int2, int> item)
        {
            throw new NotImplementedException();
        }

        public void Clear()
        {
            for (int i = 0; i < capacity; i++)
            {
                _texture[i] = -1;
            }
        }

        public bool ContainsKey(Int2 key)
        {
            if (rect.InRange(key))
            {
                return true;
            }
            else return false;
        }

        public void CopyTo(KeyValuePair<Int2, int>[] array, int arrayIndex)
        {
            throw new NotImplementedException();
        }
    }
}
