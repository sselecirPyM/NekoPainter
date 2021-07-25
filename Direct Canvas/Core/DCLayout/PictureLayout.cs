using System;
using System.Collections;
using System.Collections.Generic;
using CanvasRendering;
using DirectCanvas.Core;
using System.ComponentModel;
using System.Numerics;

namespace DirectCanvas.Layout
{
    /// <summary>
    /// 所有图层的基类
    /// </summary>
    public abstract class PictureLayout : IDisposable
    {
        /// <summary>
        /// 渲染到目标RenderTexture
        /// </summary>
        /// <param name="target"></param>

        public Guid guid;

        public PictureLayout() { }
        public bool Hidden { get; set; }
        /// <summary>
        /// 图层的名称，用来标识图层。
        /// </summary>
        public string Name;

        /// <summary>
        /// 图层的Alpha值
        /// </summary>
        public float Alpha = 1.0f;

        /// <summary>
        /// 图层的混合模式
        /// </summary>
        public Guid BlendMode { get; set; }


        public Vector4 Color = Vector4.One;

        public abstract void Dispose();

        public bool blendModeUsedDataUpdated = false;

        public bool IsPureLayout = false;
    }
}
