﻿using System;
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
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
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

            //graphicsContext.SetClearColor(new System.Numerics.Vector4(0.392156899f, 0.584313750f, 0.929411829f, 1.000000000f));
            graphicsContext.SetClearColor(new System.Numerics.Vector4(0.2f, 0.2f, 0.2f, 1.0f));

            //RenderTask = Task.Factory.StartNew(GameLoop, TaskCreationOptions.LongRunning);
        }

        public void CreateDocument(CreateDocumentParameters parameters)
        {
            ApplyAllResources();
            var folder = parameters.Folder;
            var name = parameters.Name;
            var width = parameters.Width;
            var height = parameters.Height;

            var documentStorageFolder = new DirectoryInfo(folder).CreateSubdirectory(name);
            CurrentDCDocument = new NekoPainterDocument(documentStorageFolder);
            CurrentDCDocument.Create(graphicsContext.DeviceResources, width, height, name);
            CurrentLivedDocument = CurrentDCDocument.livedDocument;
            livedDocuments.Add(CurrentDCDocument.Folder.FullName, CurrentDCDocument.livedDocument);
            documents.Add(CurrentDCDocument.Folder.FullName, CurrentDCDocument);
        }

        public void OpenDocument(string folder)
        {
            ApplyAllResources();
            CurrentDCDocument = new NekoPainterDocument(new DirectoryInfo(folder));
            CurrentDCDocument.Load(graphicsContext.DeviceResources);
            CurrentLivedDocument = CurrentDCDocument.livedDocument;
            livedDocuments.Add(CurrentDCDocument.Folder.FullName, CurrentDCDocument.livedDocument);
            documents.Add(CurrentDCDocument.Folder.FullName, CurrentDCDocument);
        }

        public void ImportDocument(string path)
        {
            if (CurrentLivedDocument?.ActivatedLayout == null) return;
            var layout = CurrentLivedDocument.ActivatedLayout;
            if (layout.graph == null)
            {
                layout.graph = new Graph();
                layout.graph.Initialize();
            }
            var fileNode = new Node();
            fileNode.fileNode = new FileNode()
            {
                path = path
            };
            var scriptNode = new Node();
            scriptNode.scriptNode = new ScriptNode();
            scriptNode.scriptNode.nodeName = "ImageImport.json";

            layout.graph.AddNodeToEnd(fileNode, new System.Numerics.Vector2(10, -20));
            layout.graph.AddNodeToEnd(scriptNode, new System.Numerics.Vector2(70, 0));
            layout.graph.Link(fileNode.Luid, "bytes", scriptNode.Luid, "file");
            CMD_Remove_RecoverNodes cmd = new CMD_Remove_RecoverNodes();
            cmd.BuildRemoveNodes(CurrentLivedDocument, layout.graph, new List<int>() { fileNode.Luid, scriptNode.Luid }, layout.guid);
            CurrentDCDocument.UndoManager.AddUndoData(cmd);
        }
        public void ExportDocument(string path)
        {
            var output = CurrentDCDocument.Output;
            var rawdata = output.GetData();
            Image<RgbaVector> image = Image.WrapMemory<RgbaVector>(rawdata, output.width, output.height);
            for (int y = 0; y < image.Height; y++)
                for (int x = 0; x < image.Width; x++)
                {
                    var color = image[x, y];
                    color.A = Math.Clamp(color.A, 0, 1);
                    image[x, y] = color;
                }
            string extension = Path.GetExtension(path);
            var ignoreCase = StringComparison.InvariantCultureIgnoreCase;
            if (".png".Equals(extension, ignoreCase))
                image.SaveAsPng(path);
            if (".jpg".Equals(extension, ignoreCase))
                image.SaveAsJpeg(path);
            if (".tga".Equals(extension, ignoreCase))
                image.SaveAsTga(path);
            image.Dispose();
        }

        //Task RenderTask;

        //public void GameLoop()
        //{
        //    while (true)
        //    {
        //        CanvasRender();
        //        Thread.Sleep(1);
        //    }
        //}

        public string language = "zh-cn";

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
                document.ViewRenderer.RenderAll();
            }

            graphicsContext.ClearScreen();
            ViewUIs.Render();

            graphicsContext.Present();
        }

        public readonly GraphicsContext graphicsContext = new GraphicsContext();

        public void SetSwapChain(IntPtr hwnd,Vector2 initSize)
        {
            graphicsContext.DeviceResources.SetSwapChainPanel(hwnd, initSize);
            graphicsContext.SetClearColor(new Vector4(0.2f, 0.2f, 0.2f, 1));
            graphicsContext.ClearScreen();
            graphicsContext.Present();
        }

        public Dictionary<string, LivedNekoPainterDocument> livedDocuments = new Dictionary<string, LivedNekoPainterDocument>();
        public Dictionary<string, NekoPainterDocument> documents = new Dictionary<string, NekoPainterDocument>();

        public LivedNekoPainterDocument CurrentLivedDocument { get; set; }
        public NekoPainterDocument CurrentDCDocument { get; set; }

        #region Resources
        void LoadResources()
        {
            LoadVS("VSImgui", "Shaders\\Basic\\VSImgui.hlsl");
            LoadPS("PSImgui", "Shaders\\Basic\\PSImgui.hlsl");
            LoadCS("Texture2TT", "Shaders\\Basic\\Texture2TT.hlsl");
            LoadCS("TextureEmptyTest", "Shaders\\Basic\\TextureEmptyTest.hlsl");
            LoadCS("TT2Texture", "Shaders\\Basic\\TT2Texture.hlsl");
            LoadCS("TTPartCopy", "Shaders\\Basic\\TTPartCopy.hlsl");
            LoadCS("TTReplace", "Shaders\\Basic\\TTReplace.hlsl");
            LoadCS("TexturePartClear", "Shaders\\Basic\\TexturePartClear.hlsl");
        }

        void LoadVS(string name, string path)
        {
            var stream = new FileStream(path, FileMode.Open);
            BinaryReader reader = new BinaryReader(stream);
            vertexShaders[name] = VertexShader.CompileAndCreate(reader.ReadBytes((int)stream.Length));
        }
        void LoadPS(string name, string path)
        {
            var stream = new FileStream(path, FileMode.Open);
            BinaryReader reader = new BinaryReader(stream);
            pixelShaders[name] = PixelShader.CompileAndCreate(reader.ReadBytes((int)stream.Length));
        }

        void LoadCS(string name, string path)
        {
            var stream = new FileStream(path, FileMode.Open);
            BinaryReader reader = new BinaryReader(stream);
            computeShaders[name] = ComputeShader.CompileAndCreate(graphicsContext.DeviceResources, reader.ReadBytes((int)stream.Length));
        }

        void ApplyAllResources()
        {
            TiledTexture.Texture2TT = computeShaders["Texture2TT"];
            TiledTexture.TextureEmptyTest = computeShaders["TextureEmptyTest"];
            TiledTexture.TT2Texture = computeShaders["TT2Texture"];
            TiledTexture.TTPartCopy = computeShaders["TTPartCopy"];
            TiledTexture.TTReplace = computeShaders["TTReplace"];
            TiledTexture.TexturePartClear = computeShaders["TexturePartClear"];
        }

        public Dictionary<string, ComputeShader> computeShaders = new Dictionary<string, ComputeShader>();
        public Dictionary<string, VertexShader> vertexShaders = new Dictionary<string, VertexShader>();
        public Dictionary<string, PixelShader> pixelShaders = new Dictionary<string, PixelShader>();
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
