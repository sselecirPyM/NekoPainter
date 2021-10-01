using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using CanvasRendering;
using NekoPainter.Core.UndoCommand;
using System.ComponentModel;
using Vortice.DXGI;
using NekoPainter.Data;
using Microsoft.CodeAnalysis.Scripting;

namespace NekoPainter.Core
{
    public class LivedNekoPainterDocument : System.IDisposable
    {
        public LivedNekoPainterDocument(DeviceResources device, int width, int height, string path)
        {
            DeviceResources = device;

            this.Path = path;
            SizeChange(width, height);

            UndoManager = new UndoManager();
            PaintAgent = new PaintAgent(this);
            PaintAgent.UndoManager = UndoManager;

            Layouts = new List<PictureLayout>();
            ViewRenderer = new ViewRenderer(this);
        }

        public void SizeChange(int width, int height)
        {
            Width = width;
            Height = height;
            Output = new RenderTexture(DeviceResources, width, height, Format.R32G32B32A32_Float, false);
            PaintingTexture = new RenderTexture(DeviceResources, width, height, Format.R32G32B32A32_Float, false);
            Controller.AppController.Instance.AddTexture(string.Format("{0}/Canvas", Path), Output);
        }

        public void SetActivatedLayout(int layoutIndex)
        {
            if (layoutIndex == -1)
            {
                ActivatedLayout = null;
                PaintAgent.CurrentLayout = null;
                return;
            }
            SetActivatedLayout(Layouts[layoutIndex]);
        }

        public void SetActivatedLayout(PictureLayout layout)
        {
            ActivatedLayout = layout;

            LayoutTex.TryGetValue(ActivatedLayout.guid, out TiledTexture tiledTexture);
            //PaintingTexture.Clear();
            //tiledTexture?.UnzipToTexture(PaintingTexture);
            PaintAgent.CurrentLayout = ActivatedLayout;
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
            TiledTexture tiledTexture = null;
            LayoutTex.TryGetValue(pictureLayout.guid, out var standardLayoutTiledTexture);

            if (PaintAgent.CurrentLayout == pictureLayout)
            {
                tiledTexture = new TiledTexture(PaintingTexture);
            }
            else if (standardLayoutTiledTexture != null)
            {
                tiledTexture = new TiledTexture(standardLayoutTiledTexture);
            }

            newPictureLayout = new PictureLayout(pictureLayout)
            {
                Name = string.Format("{0} 复制", pictureLayout.Name),
            };

            LayoutTex[newPictureLayout.guid] = tiledTexture;
            Layouts.Insert(index, newPictureLayout);
            UndoManager.AddUndoData(new CMD_DeleteLayout(newPictureLayout, this, index));
            return newPictureLayout;
        }

        //public PictureLayout CopyBuffer(int insertIndex)
        //{
        //    PictureLayout pictureLayout = new PictureLayout()
        //    {
        //        BlendMode = DefaultBlendMode,
        //        guid = System.Guid.NewGuid(),
        //        Name = string.Format("图层 {0}", Layouts.Count + 1)
        //    };
        //    LayoutTex[pictureLayout.guid] = new TiledTexture(RenderTarget[0]);
        //    Layouts.Insert(insertIndex, pictureLayout);
        //    UndoManager.AddUndoData(new CMD_DeleteLayout(pictureLayout, this, insertIndex));
        //    return pictureLayout;
        //}

        /// <summary>
        /// 设置混合模式并加入撤销
        /// </summary>
        public void SetBlendMode(PictureLayout layout, BlendMode blendMode)
        {
            UndoManager.AddUndoData(new Core.UndoCommand.CMD_BlendModeChange(layout, layout.BlendMode));
            layout.BlendMode = blendMode.Guid;
        }

        public void Dispose()
        {
            for (int i = 0; i < Layouts.Count; i++)
            {
                Layouts[i]?.Dispose();
            }
            UndoManager?.Dispose();
            Output?.Dispose();
            PaintingTexture?.Dispose();
        }
        #region Members
        public int Width { get; private set; }
        public int Height { get; private set; }
        public readonly List<PictureLayout> Layouts;
        //public readonly Dictionary<System.Guid, PictureLayout> LayoutsMap = new Dictionary<System.Guid, PictureLayout>();
        public readonly Dictionary<System.Guid, TiledTexture> LayoutTex = new Dictionary<System.Guid, TiledTexture>();
        /// <summary>
        /// 图像渲染在此进行，并代表图像最终渲染结果。
        /// </summary>
        public RenderTexture Output;
        /// <summary>
        /// 正在绘制的图像
        /// </summary>
        public RenderTexture PaintingTexture;

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
        public string Path = "";

        public readonly List<BlendMode> blendModes = new List<BlendMode>();
        public Dictionary<System.Guid, BlendMode> blendmodesMap = new Dictionary<System.Guid, BlendMode>();
        public Dictionary<string, Brush> brushes = new Dictionary<string, Brush>();

        public Dictionary<string, Brush1> brushes1 = new Dictionary<string, Brush1>();

        public float logicScale = 1.0f;
        public float rotation = 0.0f;
        public System.Numerics.Vector2 position;

        public event System.Action ActivatedLayoutChanged;

        public System.Guid DefaultBlendMode;

        public readonly DeviceResources DeviceResources;

        public Dictionary<string, string> scripts;

        public Dictionary<string, ScriptNodeDef> scriptNodeDefs;

        public Dictionary<string, Script<object>> scriptCache = new Dictionary<string, Script<object>>(System.StringComparer.InvariantCultureIgnoreCase);
    }
}

