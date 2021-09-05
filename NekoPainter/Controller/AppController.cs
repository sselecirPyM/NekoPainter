using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CanvasRendering;
using NekoPainter.Core;
using NekoPainter.FileFormat;
using System.IO;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using System.Runtime.InteropServices;
using NekoPainter.UI;
using System.Threading;

namespace NekoPainter.Controller
{
    public class AppController
    {
        public static AppController Instance { get; private set; }

        public AppController()
        {
            if (Instance != null) throw new Exception("InnerERR: Too Many Controller");
            Instance = this;
            LoadResourceFun();

            //graphicsContext.SetClearColor(new System.Numerics.Vector4(0.392156899f, 0.584313750f, 0.929411829f, 1.000000000f));
            graphicsContext.SetClearColor(new System.Numerics.Vector4(0.2f, 0.2f, 0.2f, 1.000000000f));

            //RenderTask = Task.Factory.StartNew(GameLoop, TaskCreationOptions.LongRunning);
        }

        //private async void Tick1(object sender, object e)
        //{
        //    await UI.UIHelper.OnFrame();
        //}

        public void CreateDocument(Util.CreateDocumentParameters parameters)
        {
            ApplyAllResources();
            var folder = parameters.Folder;
            var name = parameters.Name;
            var width = parameters.Width;
            var height = parameters.Height;

            var documentStorageFolder = new DirectoryInfo(folder).CreateSubdirectory(name);
            CurrentDCDocument = new NekoPainterDocument(graphicsContext.DeviceResources, documentStorageFolder);
            CurrentDCDocument.CreateAsync(width, height, parameters.CreateDocumentResourcesOption.HasFlag(Util.CreateDocumentResourcesOption.Plugin));
            CurrentCanvasCase = CurrentDCDocument.canvasCase;
            CurrentCanvasCase.Name = name;
        }

        public void OpenDocument(string folder)
        {
            ApplyAllResources();
            CurrentDCDocument = new NekoPainterDocument(graphicsContext.DeviceResources,new DirectoryInfo( folder));
            CurrentDCDocument.LoadAsync();
            CurrentCanvasCase = CurrentDCDocument.canvasCase;
        }

        //public async Task CMDSaveDocument()
        //{
        //    await CurrentDCDocument.SaveAsync();
        //}

        //public async Task CMDImportDocument()
        //{
        //    StorageFile openFile = await fileOpenPicker.PickSingleFileAsync();
        //    if (openFile == null)
        //        return;
        //    var stream = await openFile.OpenStreamForReadAsync();

        //    if (CurrentCanvasCase.ActivatedLayout != null)
        //    {
        //        byte[] imgData = GetImageData(stream, out int width, out int height);
        //        CurrentCanvasCase.PaintingTexture.ReadImageData1(imgData, width, height, computeShaders["CImport"]);
        //        CurrentCanvasCase.ActivatedLayout.saved = false;
        //    }
        //    CanvasRender();
        //    CurrentCanvasCase.PaintingTexture.CopyTo(CurrentCanvasCase.PaintingTextureBackup);
        //}

        public static byte[] GetImageData(Stream input, out int width, out int height)
        {
            Image<RgbaVector> image = Image<RgbaVector>.Load<RgbaVector>(input);
            image.Frames[0].TryGetSinglePixelSpan(out var rgbas);
            width = image.Frames[0].Width;
            height = image.Frames[0].Height;
            var span1 = MemoryMarshal.Cast<RgbaVector, byte>(rgbas);
            byte[] bytes = new byte[span1.Length];
            span1.CopyTo(bytes);
            return bytes;
        }

        Task RenderTask;

        public void GameLoop()
        {
            while (true)
            {
                CanvasRender();
                Thread.Sleep(1);
            }
        }

        public void CanvasRender()
        {
            Input.mousePreviousPos = Input.mousePos;
            ViewUIs.InputProcess();
            ViewUIs.Draw();
            if (CurrentCanvasCase != null)
            {
                CurrentCanvasCase.ViewRenderer.RenderAll();
            }
            var paintAgent = CurrentCanvasCase?.PaintAgent;
            paintAgent?.Process();

            graphicsContext.ClearScreen();
            ViewUIs.Render();

            graphicsContext.Present();
            Input.penInputData1.Clear();
        }

        public readonly GraphicsContext graphicsContext = new GraphicsContext();

        public CanvasCase CurrentCanvasCase { get; private set; }
        public NekoPainterDocument CurrentDCDocument { get; private set; }

        #region Resources
        void LoadResourceFun()
        {
            Brush.LoadStaticResourcesAsync();
            BlendMode.LoadStaticResourcesAsync();
            LoadVS("default2DVertexShader", "Shaders\\Basic\\default2DVertexShader.hlsl");
            LoadVS("VSImgui", "Shaders\\Basic\\VSImgui.hlsl");
            LoadPS("PSImgui", "Shaders\\Basic\\PSImgui.hlsl");
            LoadPS("PS2DTex1", "Shaders\\Basic\\PS2DTex1.hlsl");
            LoadCS("Texture2TT", "Shaders\\Basic\\Texture2TT.hlsl");
            LoadCS("TextureEmptyTest", "Shaders\\Basic\\TextureEmptyTest.hlsl");
            LoadCS("TT2Texture", "Shaders\\Basic\\TT2Texture.hlsl");
            LoadCS("TTPartCopy", "Shaders\\Basic\\TTPartCopy.hlsl");
            LoadCS("TTReplace", "Shaders\\Basic\\TTReplace.hlsl");
            LoadCS("TexturePartClear", "Shaders\\Basic\\TexturePartClear.hlsl");
            LoadCS("CExport", "Shaders\\Basic\\CExport.hlsl");
            LoadCS("CImport", "Shaders\\Basic\\CImport.hlsl");
        }

        void LoadVS(string name, string path)
        {
            var stream = new FileStream(path, FileMode.Open);
            BinaryReader reader = new BinaryReader(stream);
            vertexShaders[name] = VertexShader.CompileAndCreate(graphicsContext.DeviceResources, reader.ReadBytes((int)stream.Length));
        }
        void LoadPS(string name, string path)
        {
            var stream = new FileStream(path, FileMode.Open);
            BinaryReader reader = new BinaryReader(stream);
            pixelShaders[name] = PixelShader.CompileAndCreate(graphicsContext.DeviceResources, reader.ReadBytes((int)stream.Length));
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
