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
    public abstract class PictureLayout : IDisposable/*, INotifyPropertyChanged*/
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
        public virtual string Name { get => _name; set { _name = value; /*PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("Name"));*/ } }
        string _name;

        /// <summary>
        /// 图层的Alpha值
        /// </summary>
        public float Alpha { get; set; } = 1.0f;

        /// <summary>
        /// 图层的混合模式
        /// </summary>
        public Guid BlendMode { get; set; }
        //protected void PropChange(string propName)
        //{
        //    //PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propName));
        //}


        public Vector4 Color { get; set; }

        public abstract void Dispose();
        
        public bool blendModeUsedDataUpdated = false;
        
        //public event PropertyChangedEventHandler PropertyChanged;
    }
}
