using System;
using System.Collections;
using System.Collections.Generic;
using System.Numerics;
using NekoPainter.Nodes;

namespace NekoPainter.Core
{
    public class PictureLayout : IDisposable
    {
        public Guid guid;

        public PictureLayout() { }

        public PictureLayout(PictureLayout pictureLayout)
        {
            BlendMode = pictureLayout.BlendMode;
            graph = pictureLayout.graph.Clone();

            guid = Guid.NewGuid();
            generateCache = true;
            fParams = GetClone(pictureLayout.fParams);
            iParams = GetClone(pictureLayout.iParams);
            f2Params = GetClone(pictureLayout.f2Params);
            f3Params = GetClone(pictureLayout.f3Params);
            f4Params = GetClone(pictureLayout.f4Params);
            bParams = GetClone(pictureLayout.bParams);
            sParams = GetClone(pictureLayout.sParams);
        }
        public bool Hidden;
        /// <summary>
        /// 图层的名称，用来标识图层。
        /// </summary>
        public string Name;

        /// <summary>
        /// 图层的混合模式
        /// </summary>
        public Guid BlendMode { get; set; }

        public Dictionary<string, float> fParams;
        public Dictionary<string, int> iParams;
        public Dictionary<string, Vector2> f2Params;
        public Dictionary<string, Vector3> f3Params;
        public Dictionary<string, Vector4> f4Params;
        public Dictionary<string, bool> bParams;
        public Dictionary<string, string> sParams;

        public void Dispose()
        {

        }

        static Dictionary<string, T> GetClone<T>(Dictionary<string, T> a)
        {
            if (a != null) return new Dictionary<string, T>(a);
            else return null;
        }

        public Graph graph;

        [NonSerialized]
        public bool saved = false;
        [NonSerialized]
        public bool generateCache = false;
    }
}
