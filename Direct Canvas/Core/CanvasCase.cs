using System.Collections;
using System.Collections.Generic;
using DirectCanvas.Layout;
using System.IO;
using System.IO.Compression;
using CanvasRendering;
using DirectCanvas.Core;
using System.Collections.ObjectModel;
using DirectCanvas.Undo;
using NotifyCollectionChangedAction = System.Collections.Specialized.NotifyCollectionChangedAction;
using System.ComponentModel;
using Vortice.DXGI;

namespace DirectCanvas
{
    public class CanvasCase : System.IDisposable, INotifyPropertyChanged
    {
        public CanvasCase(DeviceResources device, int canvasWidth, int canvasHeight, int renderBufferCount)
        {
            Width = canvasWidth;
            Height = canvasHeight;

            DeviceResources = device;
            RenderTarget = new RenderTexture[renderBufferCount];

            for (int i = 0; i < renderBufferCount; i++)
            {
                RenderTarget[i] = new RenderTexture(device, canvasWidth, canvasHeight, Format.R32G32B32A32_Float, false);
            }

            UndoManager = new UndoManager();

            PaintingTexture = new RenderTexture(device, canvasWidth, canvasHeight, Format.R32G32B32A32_Float, false);
            PaintingTextureBackup = new RenderTexture(device, canvasWidth, canvasHeight, Format.R32G32B32A32_Float, false);
            PaintingTextureTemp = new RenderTexture(device, canvasWidth, canvasHeight, Format.R32G32B32A32_Float, false);
            //SelectionMaskTexture = new RenderTexture(device, canvasWidth, canvasHeight, RenderTextureFormat.RENDERTEXTURE_FORMAT_R8_UNORM, false);

            PaintAgent = new PaintAgent(this);
            PaintAgent.SetPaintTarget(device, PaintingTexture, PaintingTextureBackup);
            PaintAgent.UndoManager = UndoManager;

            Layouts = new ObservableCollection<PictureLayout>();
            ViewRenderer = new ViewRenderer(this);

            PaintAgent.ViewRenderer = ViewRenderer;

            Layouts.CollectionChanged += Layouts_Changed;
        }

        int movePreviousIndex;
        private void Layouts_Changed(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            if (!watched) return;
            if (e.Action == NotifyCollectionChangedAction.Add)
            {
                UndoManager.AddUndoData(new CMD_MoveLayout(this, e.NewStartingIndex, movePreviousIndex));
                DirectCanvas.UI.Controller.AppController.Instance.CanvasRerender();
            }
            else if (e.Action == NotifyCollectionChangedAction.Remove)
            {
                movePreviousIndex = e.OldStartingIndex;
            }
        }

        public void SetActivatedLayout(int layoutIndex)
        {
            //ActivatedLayout?.Deactivate(PaintingTexture);
            if (layoutIndex == -1)
            {
                ActivatedLayout = null;
                PaintAgent.CurrentLayout = null;
                return;
            }
            ActivatedLayout = (StandardLayout)Layouts[layoutIndex];
            //ActivatedLayout.Activate(PaintingTexture);
            LayoutTex.TryGetValue(ActivatedLayout.guid, out TiledTexture tiledTexture);
            StandardLayout.Activate(PaintingTexture, tiledTexture);
            PaintAgent.CurrentLayout = ActivatedLayout;
            PaintingTexture.CopyTo(PaintingTextureBackup);
            ActivatedLayoutChanged?.Invoke();
        }

        public void SetActivatedLayout(StandardLayout layout)
        {
            //ActivatedLayout?.Deactivate(PaintingTexture);
            ActivatedLayout = layout;
            //ActivatedLayout.Activate(PaintingTexture);

            LayoutTex.TryGetValue(ActivatedLayout.guid, out TiledTexture tiledTexture);
            StandardLayout.Activate(PaintingTexture, tiledTexture);
            PaintAgent.CurrentLayout = ActivatedLayout;
            PaintingTexture.CopyTo(PaintingTextureBackup);
            ActivatedLayoutChanged?.Invoke();
        }

        public StandardLayout NewStandardLayout(int insertIndex, int RenderBufferNum)
        {
            StandardLayout standardLayout = new StandardLayout()
            {
                BlendMode = DefaultBlendMode.Guid,
                guid = System.Guid.NewGuid(),
                Name = string.Format("图层 {0}", Layouts.Count + 1)
            };
            watched = false;
            Layouts.Insert(insertIndex, standardLayout);
            watched = true;
            UndoManager.AddUndoData(new CMD_DeleteLayout(standardLayout, this, insertIndex));

            return standardLayout;
        }

        public PureLayout NewPureLayout(int insertIndex, int RenderBufferNum)
        {
            PureLayout pureLayout = new PureLayout()
            {
                BlendMode = DefaultBlendMode.Guid,
                guid = System.Guid.NewGuid(),
                Name = string.Format("图层 {0}", Layouts.Count + 1)
            };
            watched = false;
            Layouts.Insert(insertIndex, pureLayout);
            watched = true;
            UndoManager.AddUndoData(new CMD_DeleteLayout(pureLayout, this, insertIndex));

            return pureLayout;
        }

        public void DeleteLayout(int index)
        {
            PictureLayout pictureLayout = Layouts[index];
            watched = false;
            //if (pictureLayout is StandardLayout standardLayout)
            //    standardLayout.Deactivate(PaintingTexture);
            if (PaintAgent.CurrentLayout == pictureLayout)
            {
                PaintAgent.CurrentLayout = null;
            }
            Layouts.RemoveAt(index);
            watched = true;
            UndoManager.AddUndoData(new CMD_RecoverLayout(pictureLayout, this, index));
        }

        public PictureLayout CopyLayout(int index)
        {
            watched = false;
            PictureLayout pictureLayout = Layouts[index];
            PictureLayout newPictureLayout = null;
            if (pictureLayout is StandardLayout standardLayout)
            {
                TiledTexture tiledTexture = null;
                LayoutTex.TryGetValue(standardLayout.guid, out var standardLayouttiledTexture);

                if (PaintAgent.CurrentLayout== standardLayout)
                {
                    tiledTexture = new TiledTexture(PaintingTexture);
                }
                else if (standardLayouttiledTexture != null)
                {
                    tiledTexture = new TiledTexture(standardLayouttiledTexture);
                }

                newPictureLayout = new StandardLayout(standardLayout)
                {
                    Name = string.Format("{0} 复制", standardLayout.Name),
                    //tiledTexture = tiledTexture,
                };

                LayoutTex[newPictureLayout.guid] = tiledTexture;
            }
            else if (pictureLayout is PureLayout pureLayout)
            {
                newPictureLayout = new PureLayout(pureLayout)
                {
                    Name = string.Format("{0} 复制", pureLayout.Name)
                };
            }
            Layouts.Insert(index, newPictureLayout);
            watched = true;
            UndoManager.AddUndoData(new CMD_DeleteLayout(newPictureLayout, this, index));
            return newPictureLayout;
        }

        //public StandardLayout CopyBuffer(int insertIndex, int RenderBufferNum)
        //{
        //    StandardLayout standardLayout = new StandardLayout(RenderTarget[0])
        //    {
        //        BlendMode = DefaultBlendMode.Guid,
        //        guid = System.Guid.NewGuid(),
        //        Name = string.Format("图层 {0}", Layouts.Count + 1)
        //    };
        //    watched = false;
        //    Layouts.Insert(insertIndex, standardLayout);
        //    watched = true;
        //    UndoManager.AddUndoData(new CMD_DeleteLayout(standardLayout, this, insertIndex));

        //    return standardLayout;
        //}

        /// <summary>
        /// 设置混合模式并加入撤销
        /// </summary>
        public void SetBlendMode(PictureLayout layout, BlendMode blendMode)
        {
            UndoManager.AddUndoData(new Undo.CMD_BlendModeChange(layout, layout.BlendMode));
            layout.BlendMode = blendMode.Guid;
        }

        public void Dispose()
        {
            PaintAgent?.Dispose();
            for (int i = 0; i < Layouts.Count; i++)
            {
                Layouts[i]?.Dispose();
            }
            UndoManager?.Dispose();
            for (int i = 0; i < RenderTarget.Length; i++)
                RenderTarget[i]?.Dispose();
            PaintingTexture?.Dispose();
            PaintingTextureBackup?.Dispose();
            PaintingTextureTemp?.Dispose();
        }
        #region Members
        public int Width { get; private set; }
        public int Height { get; private set; }
        public readonly ObservableCollection<PictureLayout> Layouts;
        public readonly Dictionary<System.Guid, PictureLayout> LayoutsMap = new Dictionary<System.Guid, PictureLayout>();
        public readonly Dictionary<System.Guid, TiledTexture> LayoutTex = new Dictionary<System.Guid, TiledTexture>();
        /// <summary>
        /// 图像渲染在此进行，并代表图像最终渲染结果。
        /// </summary>
        public RenderTexture[] RenderTarget;
        /// <summary>
        /// 正在绘制的图像
        /// </summary>
        public RenderTexture PaintingTexture;
        /// <summary>
        /// 用来暂存备份的图层
        /// </summary>
        public RenderTexture PaintingTextureBackup;
        public RenderTexture PaintingTextureTemp;

        //public RenderTexture SelectionMaskTexture;
        /// <summary>
        /// 此案例中的撤销/重做管理器
        /// </summary>
        public UndoManager UndoManager { get; }
        /// <summary>
        /// 此案例中的绘图代理
        /// </summary>
        public PaintAgent PaintAgent { get; }
        /// <summary>
        /// 图像渲染器
        /// </summary>
        public ViewRenderer ViewRenderer;
        /// <summary>
        /// 正在激活的图层
        /// </summary>
        public StandardLayout ActivatedLayout { get; private set; }
        public PictureLayout SelectedLayout { get; set; }
        #endregion

        #region Properties 
        public string Name
        {
            get { return _name; }
            set
            {
                _name = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("Name"));
            }
        }
        string _name = "";
        public string Description
        {
            get { return _description; }
            set
            {
                _description = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("Description"));
            }
        }
        string _description = "";

        public event PropertyChangedEventHandler PropertyChanged;
        #endregion

        public readonly ObservableCollection<BlendMode> blendModes = new ObservableCollection<BlendMode>();
        public Dictionary<System.Guid, Core.BlendMode> blendmodesMap = new Dictionary<System.Guid, Core.BlendMode>();

        public event System.Action ActivatedLayoutChanged;

        public BlendMode DefaultBlendMode;

        public readonly DeviceResources DeviceResources;

        public bool watched;
    }
}

