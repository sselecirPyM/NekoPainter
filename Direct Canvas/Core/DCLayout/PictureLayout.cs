using System;
using System.Collections;
using System.Collections.Generic;
using CanvasRendering;
using DirectCanvas.Core;
using System.ComponentModel;

namespace DirectCanvas.Layout
{
    /// <summary>
    /// 所有图层的基类
    /// </summary>
    public abstract class PictureLayout : IDisposable, INotifyPropertyChanged
    {
        /// <summary>
        /// 渲染到目标RenderTexture
        /// </summary>
        /// <param name="target"></param>

        public Guid guid;

        public PictureLayout() { }
        public abstract bool Hidden { get; set; }
        protected bool _hidden = false;
        /// <summary>
        /// 图层的名称，用来标识图层。
        /// </summary>
        public virtual string Name { get => _name; set { _name = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("Name")); } }
        string _name;

        /// <summary>
        /// 图层的Alpha值
        /// </summary>
        public abstract float Alpha { get; set; }
        protected float _alpha = 1.0f;

        /// <summary>
        /// 图层的混合模式
        /// </summary>
        public abstract Guid BlendMode { get; set; }
        protected Guid _blendMode;
        protected void PropChange(string propName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propName));
        }

        public abstract void Dispose();
        
        public bool blendModeUsedDataUpdated = false;
        
        public event PropertyChangedEventHandler PropertyChanged;
    }
}
