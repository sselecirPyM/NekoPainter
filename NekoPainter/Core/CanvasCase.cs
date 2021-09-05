using System.Collections;
using System.Collections.Generic;
using NekoPainter.Core;
using System.IO;
using System.IO.Compression;
using CanvasRendering;
using System.Collections.ObjectModel;
using NekoPainter.Undo;
using NotifyCollectionChangedAction = System.Collections.Specialized.NotifyCollectionChangedAction;
using System.ComponentModel;
using Vortice.DXGI;

namespace NekoPainter
{
    public class CanvasCase : System.IDisposable
    {
        public CanvasCase(DeviceResources device, int canvasWidth, int canvasHeight)
        {
            Width = canvasWidth;
            Height = canvasHeight;

            DeviceResources = device;
            RenderTarget = new RenderTexture[1];

            //SelectionMaskTexture = new RenderTexture(device, canvasWidth, canvasHeight, RenderTextureFormat.RENDERTEXTURE_FORMAT_R8_UNORM, false);
            SizeChange(canvasWidth, canvasHeight);

            UndoManager = new UndoManager();
            PaintAgent = new PaintAgent(this);
            PaintAgent.SetPaintTarget(PaintingTexture, PaintingTextureBackup);
            PaintAgent.UndoManager = UndoManager;

            Layouts = new ObservableCollection<PictureLayout>();
            ViewRenderer = new ViewRenderer(this);
        }

        public void SizeChange(int canvasWidth, int canvasHeight)
        {
            for (int i = 0; i < 1; i++)
            {
                RenderTarget[i] = new RenderTexture(DeviceResources, canvasWidth, canvasHeight, Format.R32G32B32A32_Float, false);
            }
            PaintingTexture = new RenderTexture(DeviceResources, canvasWidth, canvasHeight, Format.R32G32B32A32_Float, false);
            PaintingTextureBackup = new RenderTexture(DeviceResources, canvasWidth, canvasHeight, Format.R32G32B32A32_Float, false);
            PaintingTextureTemp = new RenderTexture(DeviceResources, canvasWidth, canvasHeight, Format.R32G32B32A32_Float, false);
            Controller.AppController.Instance.AddTexture("CurrentCanvas", RenderTarget[0]);
        }

        public void SetActivatedLayout(int layoutIndex)
        {
            if (layoutIndex == -1)
            {
                ActivatedLayout = null;
                PaintAgent.CurrentLayout = null;
                return;
            }
            ActivatedLayout = (PictureLayout)Layouts[layoutIndex];
            LayoutTex.TryGetValue(ActivatedLayout.guid, out TiledTexture tiledTexture);
            PaintingTexture.Clear();
            tiledTexture?.UnzipToTexture(PaintingTexture);
            PaintAgent.CurrentLayout = ActivatedLayout;
            PaintingTexture.CopyTo(PaintingTextureBackup);
            ActivatedLayoutChanged?.Invoke();
        }

        public void SetActivatedLayout(PictureLayout layout)
        {
            ActivatedLayout = layout;

            LayoutTex.TryGetValue(ActivatedLayout.guid, out TiledTexture tiledTexture);
            PaintingTexture.Clear();
            tiledTexture?.UnzipToTexture(PaintingTexture);
            PaintAgent.CurrentLayout = ActivatedLayout;
            PaintingTexture.CopyTo(PaintingTextureBackup);
            ActivatedLayoutChanged?.Invoke();
        }

        public PictureLayout NewStandardLayout(int insertIndex)
        {
            PictureLayout standardLayout = new PictureLayout()
            {
                BlendMode = DefaultBlendMode,
                guid = System.Guid.NewGuid(),
                Name = string.Format("图层 {0}", Layouts.Count + 1)
            };
            Layouts.Insert(insertIndex, standardLayout);
            UndoManager.AddUndoData(new CMD_DeleteLayout(standardLayout, this, insertIndex));

            return standardLayout;
        }

        public void DeleteLayout(int index)
        {
            PictureLayout pictureLayout = Layouts[index];
            if (PaintAgent.CurrentLayout == pictureLayout)
            {
                PaintAgent.CurrentLayout = null;
            }
            Layouts.RemoveAt(index);
            UndoManager.AddUndoData(new CMD_RecoverLayout(pictureLayout, this, index));
        }

        public PictureLayout CopyLayout(int index)
        {
            PictureLayout pictureLayout = Layouts[index];
            PictureLayout newPictureLayout = null;
            if (pictureLayout is PictureLayout standardLayout)
            {
                TiledTexture tiledTexture = null;
                LayoutTex.TryGetValue(standardLayout.guid, out var standardLayouttiledTexture);

                if (PaintAgent.CurrentLayout == standardLayout)
                {
                    tiledTexture = new TiledTexture(PaintingTexture);
                }
                else if (standardLayouttiledTexture != null)
                {
                    tiledTexture = new TiledTexture(standardLayouttiledTexture);
                }

                newPictureLayout = new PictureLayout(standardLayout)
                {
                    Name = string.Format("{0} 复制", standardLayout.Name),
                    //tiledTexture = tiledTexture,
                };

                LayoutTex[newPictureLayout.guid] = tiledTexture;
            }
            Layouts.Insert(index, newPictureLayout);
            UndoManager.AddUndoData(new CMD_DeleteLayout(newPictureLayout, this, index));
            return newPictureLayout;
        }

        public PictureLayout CopyBuffer(int insertIndex)
        {
            PictureLayout pictureLayout = new PictureLayout()
            {
                BlendMode = DefaultBlendMode,
                guid = System.Guid.NewGuid(),
                Name = string.Format("图层 {0}", Layouts.Count + 1)
            };
            LayoutTex[pictureLayout.guid] = new TiledTexture(RenderTarget[0]);
            Layouts.Insert(insertIndex, pictureLayout);
            UndoManager.AddUndoData(new CMD_DeleteLayout(pictureLayout, this, insertIndex));
            return pictureLayout;
        }

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
        public PictureLayout ActivatedLayout { get; private set; }
        public PictureLayout SelectedLayout { get; set; }
        #endregion

        public string Name = "";
        public string Description = "";

        public readonly List<BlendMode> blendModes = new List<BlendMode>();
        public Dictionary<System.Guid, Core.BlendMode> blendmodesMap = new Dictionary<System.Guid, Core.BlendMode>();

        public float logicScale = 1.0f;
        public float rotation = 0.0f;
        public System.Numerics.Vector2 position;

        public event System.Action ActivatedLayoutChanged;

        public System.Guid DefaultBlendMode;

        public readonly DeviceResources DeviceResources;
    }
}

