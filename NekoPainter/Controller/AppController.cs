using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CanvasRendering;
using NekoPainter.Core;
using NekoPainter.Core.Nodes;
using NekoPainter.Core.UndoCommand;
using NekoPainter.FileFormat;
using System.IO;
using NekoPainter.UI;
using System.Threading;
using System.Numerics;

namespace NekoPainter.Controller
{
    public class AppController
    {
        public static AppController Instance { get; private set; }

        public AppController()
        {
            if (Instance != null) throw new Exception("InnerERR: Too Many Controller");
            Instance = this;
            LoadResources();

            graphicsContext.SetClearColor(new Vector4(0.2f, 0.2f, 0.2f, 1.0f));
        }

        public ImguiInput imguiInput = new ImguiInput();

        public string language = "zh-cn";

        public void CreateDocument(CreateDocumentParameters parameters)
        {
            ApplyAllResources();
            var folder = parameters.Folder;
            var name = parameters.Name;
            var width = parameters.Width;
            var height = parameters.Height;

            var documentStorageFolder = new DirectoryInfo(folder).CreateSubdirectory(name);
            var document = new NekoPainterDocument(documentStorageFolder);
            document.Create(graphicsContext.DeviceResources, width, height, name);
            livedDocuments.Add(document.folder.FullName, document.livedDocument);
            documents.Add(document.folder.FullName, document);
            CurrentDCDocument = document;
        }

        public void OpenDocument(string folder)
        {
            ApplyAllResources();
            var document = new NekoPainterDocument(new DirectoryInfo(folder));
            document.Load(graphicsContext.DeviceResources);
            livedDocuments.Add(document.folder.FullName, document.livedDocument);
            documents.Add(document.folder.FullName, document);
            CurrentDCDocument = document;
        }

        public DocumentRenderer documentRenderer = new DocumentRenderer();
        public void CanvasRender()
        {
            if (StringTranslations.current != language)
            {
                StringTranslations.Load(language);
            }
            ViewUIs.InputProcess();
            ViewUIs.Draw();

            foreach (var document in documents.Values)
            {
                document.PaintAgent.Process();
                documentRenderer.RenderAll(document, document.Output);
            }

            graphicsContext.ClearScreen();
            ViewUIs.Render();

            graphicsContext.Present();
        }

        public readonly GraphicsContext graphicsContext = new GraphicsContext();

        public void SetSwapChain(IntPtr hwnd, Vector2 initSize)
        {
            graphicsContext.DeviceResources.SetSwapChainPanel(hwnd, initSize);
            graphicsContext.SetClearColor(new Vector4(0.2f, 0.2f, 0.2f, 1));
            graphicsContext.ClearScreen();
            graphicsContext.Present();
        }

        public Dictionary<string, LivedNekoPainterDocument> livedDocuments = new Dictionary<string, LivedNekoPainterDocument>();
        public Dictionary<string, NekoPainterDocument> documents = new Dictionary<string, NekoPainterDocument>();

        public LivedNekoPainterDocument CurrentLivedDocument { get => CurrentDCDocument?.livedDocument; }
        public NekoPainterDocument CurrentDCDocument { get; set; }

        #region Resources
        void LoadResources()
        {
            LoadPSO("Imgui", "Shaders\\Basic\\Imgui.hlsl");
            LoadCS("Texture2TT", "Shaders\\Basic\\Texture2TT.hlsl");
            LoadCS("TextureEmptyTest", "Shaders\\Basic\\TextureEmptyTest.hlsl");
            LoadCS("TT2Texture", "Shaders\\Basic\\TT2Texture.hlsl");
            LoadCS("TTPartCopy", "Shaders\\Basic\\TTPartCopy.hlsl");
            LoadCS("TTReplace", "Shaders\\Basic\\TTReplace.hlsl");
            LoadCS("TexturePartClear", "Shaders\\Basic\\TexturePartClear.hlsl");
        }

        void LoadPSO(string name, string path)
        {
            var stream = new FileStream(path, FileMode.Open);
            BinaryReader reader = new BinaryReader(stream);
            byte[] data = reader.ReadBytes((int)stream.Length);

            psos[name] = PipelineStateObject.CompileAndCreate(data);
        }

        void LoadCS(string name, string path)
        {
            var stream = new FileStream(path, FileMode.Open);
            BinaryReader reader = new BinaryReader(stream);
            computeShaders[name] = ComputeShader.CompileAndCreate(graphicsContext.DeviceResources, reader.ReadBytes((int)stream.Length), "main");
        }

        void ApplyAllResources()
        {
            TiledTexture.Texture2TT = computeShaders["Texture2TT"];
            TiledTexture.TextureEmptyTest = computeShaders["TextureEmptyTest"];
            TiledTexture.TT2Texture = computeShaders["TT2Texture"];
            TiledTexture.TTPartCopy = computeShaders["TTPartCopy"];
        }

        public Dictionary<string, ComputeShader> computeShaders = new Dictionary<string, ComputeShader>();
        public Dictionary<string, PipelineStateObject> psos = new Dictionary<string, PipelineStateObject>();
        public Dictionary<long, RenderTexture> textures = new Dictionary<long, RenderTexture>();
        public Dictionary<string, long> string2Id = new Dictionary<string, long>();
        public RenderTexture GetTexture(string s)
        {
            return textures[GetId(s)];
        }
        public void AddTexture(string s, RenderTexture tex)
        {
            textures[GetId(s)] = tex;
        }
        public long GetId(string s)
        {
            if (string2Id.TryGetValue(s, out long id))
                return id;
            else
            {
                id = string2Id.Count;
                string2Id[s] = id;
                return id;
            }
        }
        #endregion
    }
}
