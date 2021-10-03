using CanvasRendering;
using NekoPainter.Data;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NekoPainter
{
    public class TileIndexCollection2
    {
        Dictionary<Int2, int> _indice;
        TileIndexCollection _collection;

        Rectangle rect;

        public TileIndexCollection2(Rectangle rect)
        {
            this.rect = rect;
            _collection = new TileIndexCollection(rect);
        }

        public TileIndexCollection2(TileIndexCollection2 collection)
        {
            rect = collection.rect;
            if (collection._indice != null)
            {
                _indice = new Dictionary<Int2, int>(collection._indice);
            }
            if (collection._collection != null)
            {
                _collection = new TileIndexCollection(collection._collection);
            }
        }

        public int this[Int2 key]
        {
            get
            {
                if (rect.Contains(key))
                {
                    if (_collection != null)
                    {
                        return _collection[key];
                    }
                    if (_indice != null)
                    {
                        return _indice[key];
                    }
                    return -1;
                    //return _texture[(key.X - rect.minX) / 8 % stride + (key.Y - rect.minY) / 8 * stride];
                }
                else return -1;
            }
            set
            {
                if (rect.Contains(key))
                {
                    if (_collection != null)
                    {
                        _collection[key] = value;
                    }
                    if (_indice != null)
                    {
                        _indice[key] = value;
                    }

                    //_texture[(key.X - rect.minX) / 8 % stride + (key.Y - rect.minY) / 8 * stride] = value;
                }
                else return;
            }
        }

        public void Add(Int2 key, int value)
        {
            if (rect.Contains(key))
            {
                if (_collection != null)
                {
                    _collection.Add(key, value);
                }
                if (_indice != null)
                {
                    _indice.Add(key, value);
                }
                //_texture[(key.X - rect.minX) / 8 % stride + (key.Y - rect.minY) / 8 * stride] = value;
            }
            else return;
        }
    }
    public class TileIndexCollection
    {
        int[] _texture;
        int stride;
        int capacity;
        Rectangle rect;

        public TileIndexCollection(Rectangle rect)
        {
            this.rect = rect;
            stride = (rect.Width) / 8 + 1;
            capacity = stride * ((rect.Height + 8) / 8);
            _texture = new int[capacity];
            for (int i = 0; i < capacity; i++)
            {
                _texture[i] = -1;
            }
        }

        public TileIndexCollection(Rectangle rect, IReadOnlyList<Int2> points)
        {
            this.rect = rect;
            stride = (rect.Width) / 8 + 1;
            capacity = stride * ((rect.Height + 8) / 8);
            _texture = new int[capacity];
            for (int i = 0; i < capacity; i++)
            {
                _texture[i] = -1;
            }
            for (int i = 0; i < points.Count; i++)
            {
                Add(points[i],i);
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
                if (rect.Contains(key))
                {
                    return _texture[(key.X - rect.Left) / 8 % stride + (key.Y - rect.Top) / 8 * stride];
                }
                else return -1;
            }
            set
            {
                if (rect.Contains(key))
                {
                    _texture[(key.X - rect.Left) / 8 % stride + (key.Y - rect.Top) / 8 * stride] = value;
                }
                else return;
            }
        }

        public void Add(Int2 key, int value)
        {
            if (rect.Contains(key))
            {
                _texture[(key.X - rect.Left) / 8 % stride + (key.Y - rect.Top) / 8 * stride] = value;
            }
            else return;
        }

        public bool TryGetValue(Int2 position,out int value)
        {
            int v1 = this[position];
            if(v1==-1)
            {
                value = default(int);
                return false;
            }
            value = v1;
            return true;
        }

        //public void Add(KeyValuePair<Int2, int> item)
        //{
        //    throw new NotImplementedException();
        //}

        //public void Clear()
        //{
        //    for (int i = 0; i < capacity; i++)
        //    {
        //        _texture[i] = -1;
        //    }
        //}

        //public bool ContainsKey(Int2 key)
        //{
        //    if (rect.InRange(key))
        //    {
        //        return true;
        //    }
        //    else return false;
        //}

        //public void CopyTo(KeyValuePair<Int2, int>[] array, int arrayIndex)
        //{
        //    throw new NotImplementedException();
        //}
    }
}
