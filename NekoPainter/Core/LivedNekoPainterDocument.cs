using System.Collections;
using System.Collections.Generic;
using System.IO;
using CanvasRendering;
using NekoPainter.Core.UndoCommand;
using Vortice.DXGI;
using NekoPainter.Data;
using Microsoft.CodeAnalysis.Scripting;

namespace NekoPainter.Core
{
    public class LivedNekoPainterDocument : System.IDisposable
    {
        public LivedNekoPainterDocument(int width, int height)
        {
            Width = width;
            Height = height;

            Layouts = new List<PictureLayout>();
        }

        public void Dispose()
        {
        }

        public int Width { get; private set; }
        public int Height { get; private set; }
        public List<PictureLayout> Layouts;
        //public readonly Dictionary<System.Guid, PictureLayout> LayoutsMap = new Dictionary<System.Guid, PictureLayout>();
        public readonly Dictionary<System.Guid, TiledTexture> LayoutTex = new Dictionary<System.Guid, TiledTexture>();

        /// <summary>
        /// 正在激活的图层
        /// </summary>
        public PictureLayout ActivatedLayout { get; set; }
        public PictureLayout SelectedLayout { get; set; }

        public string Name = "";
        public string Description = "";

        public readonly List<BlendMode> blendModes = new List<BlendMode>();

        public Dictionary<System.Guid, BlendMode> blendModesMap = new Dictionary<System.Guid, BlendMode>();

        public Dictionary<string, Brush> brushes = new Dictionary<string, Brush>();

        public System.Guid DefaultBlendMode;

        public Dictionary<string, string> scripts = new Dictionary<string, string>(System.StringComparer.InvariantCultureIgnoreCase);

        public Dictionary<string, ScriptNodeDef> scriptNodeDefs = new Dictionary<string, ScriptNodeDef>(System.StringComparer.InvariantCultureIgnoreCase);

        public Dictionary<string, string> shaders = new Dictionary<string, string>(System.StringComparer.InvariantCultureIgnoreCase);

        public Dictionary<string, ComputeShaderDef> shaderDefs = new Dictionary<string, ComputeShaderDef>(System.StringComparer.InvariantCultureIgnoreCase);

        public Dictionary<string, Script<object>> scriptCache = new Dictionary<string, Script<object>>(System.StringComparer.InvariantCultureIgnoreCase);
    }
}

