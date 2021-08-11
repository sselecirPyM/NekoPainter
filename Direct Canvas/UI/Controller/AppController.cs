using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CanvasRendering;
using DirectCanvas.UICommand;
using Windows.Storage.Pickers;
using DirectCanvas.Core;
using Windows.Storage;
using DirectCanvas.FileFormat;
using System.IO;
using Windows.Foundation;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using System.Runtime.InteropServices;

namespace DirectCanvas.UI.Controller
{
    public class AppController
    {
        public static AppController Instance { get; private set; }

        public AppController()
        {
            if (Instance != null) throw new Exception("InnerERR: Too Many Controller");
            Instance = this;
            LoadResourceTask = LoadResourceFun();

            fileOpenPicker.FileTypeFilter.Add(".bmp");
            fileOpenPicker.FileTypeFilter.Add(".jpg");
            fileOpenPicker.FileTypeFilter.Add(".jpeg");
            fileOpenPicker.FileTypeFilter.Add(".png");
            fileOpenPicker.FileTypeFilter.Add(".gif");
            fileOpenPicker.FileTypeFilter.Add(".tif");
            fileSavePicker.FileTypeChoices.Add("png图像", new string[] { ".png" });
            fileSavePicker.FileTypeChoices.Add("jpg图像", new string[] { ".jpg" });

            ApplySettings(AppSettings.LoadDefault());
        }

        public async Task CreateDocument(Util.CreateDocumentParameters parameters)
        {
            command_New.Activate = false;
            command_Open.Activate = false;
            await ApplyAllResources();
            var folder = parameters.Folder;
            var name = parameters.Name;
            var width = parameters.Width;
            var height = parameters.Height;

            StorageFolder documentStorageFolder = await folder.CreateFolderAsync(name);
            CurrentDCDocument = new DirectCanvasDocument(graphicsContext.DeviceResources, documentStorageFolder);
            await CurrentDCDocument.CreateAsync(width, height, parameters.CreateDocumentResourcesOption.HasFlag(Util.CreateDocumentResourcesOption.Plugin));
            CurrentCanvasCase = CurrentDCDocument.canvasCase;
            CurrentCanvasCase.Name = name;

            command_Save.Activate = true;
            command_Import.Activate = true;
            command_Export.Activate = true;
            command_Undo.CanvasCase = CurrentCanvasCase;
            command_Redo.CanvasCase = CurrentCanvasCase;

            command_ResetCanvasPosition.Activate = true;
        }

        public async Task OpenDocument(StorageFolder folder)
        {
            command_New.Activate = false;
            command_Open.Activate = false;
            await ApplyAllResources();
            CurrentDCDocument = new DirectCanvasDocument(graphicsContext.DeviceResources, folder);
            await CurrentDCDocument.LoadAsync();
            CurrentCanvasCase = CurrentDCDocument.canvasCase;

            command_Save.Activate = true;
            command_Import.Activate = true;
            command_Export.Activate = true;
            command_Undo.CanvasCase = CurrentCanvasCase;
            command_Redo.CanvasCase = CurrentCanvasCase;

            command_ResetCanvasPosition.Activate = true;
        }

        public async Task CMDSaveDocument()
        {
            await CurrentDCDocument.SaveAsync();
        }

        public async Task CMDImportDocument()
        {
            StorageFile openFile = await fileOpenPicker.PickSingleFileAsync();
            if (openFile == null)
                return;
            var stream = await openFile.OpenStreamForReadAsync();

            if (CurrentCanvasCase.ActivatedLayout != null)
            {
                byte[] imgData = GetImageData(stream, out int width, out int height);
                CurrentCanvasCase.PaintingTexture.ReadImageData1(imgData, width, height, computeShaders["CImport"]);
                CurrentCanvasCase.ActivatedLayout.saved = false;
            }
            CanvasRerender();
            CurrentCanvasCase.PaintingTexture.CopyTo(CurrentCanvasCase.PaintingTextureBackup);
        }

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
        PenInputFlag currentState;
        public void CanvasRerender()
        {
            Input.mousePreviousPos = Input.mousePos;
            if (dcUI_Canvas != null)
            {
                ViewUIs.InputProcess();
            }
            if (LoadResourceTask.Status == TaskStatus.RanToCompletion)
            {
                ViewUIs.Draw();
            }
            if (CurrentCanvasCase != null)
            {
                CurrentCanvasCase.ViewRenderer.RenderAll();
            }
            if (dcUI_Canvas != null)
            {
                if (!Input.uiMouseCapture)
                {
                    dcUI_Canvas.WheelScale(Input.mousePos, Input.deltaWheel);
                }
                if (Input.canvasInputStatus == CanvasInputStatus.Drag)
                {
                    dcUI_Canvas.MoveProcess(Input.mousePos, Input.mousePreviousPos);
                }
                else if (Input.canvasInputStatus == CanvasInputStatus.DragRotate)
                {
                    dcUI_Canvas.RotateProcess(Input.mousePos, Input.mousePreviousPos);
                }
                while (Input.penInputData1.TryDequeue(out var result))
                {
                    var paintAgent = CurrentCanvasCase.PaintAgent;
                    if (!Input.uiMouseCapture || currentState == PenInputFlag.Drawing)
                    {
                        currentState = result.penInputFlag;
                        switch (result.penInputFlag)
                        {
                            case PenInputFlag.Begin:
                                paintAgent.DrawBegin(result);
                                break;
                            case PenInputFlag.Drawing:
                                paintAgent.Draw(result);
                                break;
                            case PenInputFlag.End:
                                paintAgent.DrawEnd(result);
                                break;
                        }
                        paintAgent.Process();
                    }
                }

                graphicsContext.ClearScreen();
                dcUI_Canvas.RenderContent();
                if (LoadResourceTask.Status == TaskStatus.RanToCompletion)
                {
                    ViewUIs.Render();
                }

                graphicsContext.Present();
            }
            Input.penInputData1.Clear();
        }

        public AppSettings currentAppSettings;

        public void ApplySettings(AppSettings appSettings)
        {
            if (currentAppSettings != null)
                currentAppSettings.PropertyChanged -= AppSettings_PropertyChanged;
            currentAppSettings = appSettings;
            appSettings.PropertyChanged += AppSettings_PropertyChanged;

            var c = appSettings.BackGroundColor;
            graphicsContext.SetClearColor(c);
            CanvasRerender();
        }

        private void AppSettings_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == "BackGroundColor")
            {
                var c = currentAppSettings.BackGroundColor;
                graphicsContext.SetClearColor(c);
                CanvasRerender();
            }
        }

        public DCUI_Canvas dcUI_Canvas;

        public readonly GraphicsContext graphicsContext = new GraphicsContext();

        public readonly Command_Unknow command_New = new Command_Unknow();
        public readonly Command_Unknow command_Open = new Command_Unknow();
        public readonly Command_Unknow command_Save = new Command_Unknow() { Activate = false };
        public readonly Command_Unknow command_Import = new Command_Unknow() { Activate = false };
        public readonly Command_Unknow command_Export = new Command_Unknow() { Activate = false };
        public readonly Command_Undo command_Undo = new Command_Undo();
        public readonly Command_Redo command_Redo = new Command_Redo();

        public readonly Command_Unknow command_ResetCanvasPosition = new Command_Unknow() { Activate = false };
        public readonly FileSavePicker fileSavePicker = new FileSavePicker() { SuggestedStartLocation = PickerLocationId.PicturesLibrary };
        public readonly FileOpenPicker fileOpenPicker = new FileOpenPicker() { SuggestedStartLocation = PickerLocationId.PicturesLibrary };

        public CanvasCase CurrentCanvasCase { get; private set; }
        public DirectCanvasDocument CurrentDCDocument { get; private set; }

        #region Resources
        Task LoadResourceTask;
        async Task LoadResourceFun()
        {
            await Task.WhenAll(Brush.LoadStaticResourcesAsync(), BlendMode.LoadStaticResourcesAsync());
            await LoadVS("default2DVertexShader", "Shaders\\Basic\\default2DVertexShader.hlsl");
            await LoadVS("VSImgui", "Shaders\\Basic\\VSImgui.hlsl");
            await LoadPS("PSImgui", "Shaders\\Basic\\PSImgui.hlsl");
            await LoadPS("PS2DTex1", "Shaders\\Basic\\PS2DTex1.hlsl");
            await LoadCS("Texture2TT", "Shaders\\Basic\\Texture2TT.hlsl");
            await LoadCS("TextureEmptyTest", "Shaders\\Basic\\TextureEmptyTest.hlsl");
            await LoadCS("TT2Texture", "Shaders\\Basic\\TT2Texture.hlsl");
            await LoadCS("TTPartCopy", "Shaders\\Basic\\TTPartCopy.hlsl");
            await LoadCS("TTReplace", "Shaders\\Basic\\TTReplace.hlsl");
            await LoadCS("TexturePartClear", "Shaders\\Basic\\TexturePartClear.hlsl");
            await LoadCS("CExport", "Shaders\\Basic\\CExport.hlsl");
            await LoadCS("CImport", "Shaders\\Basic\\CImport.hlsl");
        }

        async Task LoadVS(string name, string path)
        {
            var stream = await (await StorageFile.GetFileFromApplicationUriAsync(new Uri(string.Format("ms-appx:///{0}", path)))).OpenStreamForReadAsync();
            BinaryReader reader = new BinaryReader(stream);
            vertexShaders[name] = VertexShader.CompileAndCreate(graphicsContext.DeviceResources, reader.ReadBytes((int)stream.Length));
        }
        async Task LoadPS(string name, string path)
        {
            var stream = await (await StorageFile.GetFileFromApplicationUriAsync(new Uri(string.Format("ms-appx:///{0}", path)))).OpenStreamForReadAsync();
            BinaryReader reader = new BinaryReader(stream);
            pixelShaders[name] = PixelShader.CompileAndCreate(graphicsContext.DeviceResources, reader.ReadBytes((int)stream.Length));
        }

        async Task LoadCS(string name, string path)
        {
            var stream = await (await StorageFile.GetFileFromApplicationUriAsync(new Uri(string.Format("ms-appx:///{0}", path)))).OpenStreamForReadAsync();
            BinaryReader reader = new BinaryReader(stream);
            computeShaders[name] = ComputeShader.CompileAndCreate(graphicsContext.DeviceResources, reader.ReadBytes((int)stream.Length));
        }

        async Task ApplyAllResources()
        {
            if (LoadResourceTask.Status != TaskStatus.RanToCompletion)
                await LoadResourceTask;
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
        #endregion

        public MainPage mainPage;
    }
}
