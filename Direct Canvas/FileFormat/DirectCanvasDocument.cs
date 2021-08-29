using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Storage;
using Windows.Storage.Streams;
using Windows.Foundation;
using DirectCanvas.Core;
using System.IO;
using System.Xml;
using CanvasRendering;
using Windows.ApplicationModel;
using System.Xml.Serialization;
using System.ComponentModel;
using System.Numerics;
using System.Collections.Concurrent;

namespace DirectCanvas.FileFormat
{
    public class DirectCanvasDocument
    {
        public DirectCanvasDocument(DeviceResources deviceResources, StorageFolder caseFolder)
        {
            CaseFolder = caseFolder;
            this.DeviceResources = deviceResources;
        }

        static XmlWriterSettings xmlWriterSettings = xmlWriterSettings = new XmlWriterSettings()
        {
            Encoding = Encoding.UTF8,
            Indent = true
        };
        static XmlReaderSettings xmlReaderSettings = new XmlReaderSettings()
        {
            IgnoreComments = true,
        };

        public StorageFolder CaseFolder;
        DeviceResources DeviceResources;
        public CanvasCase canvasCase;
        public StorageFolder blendModesFolder;
        public StorageFolder brushesFolder;
        //public StorageFolder animationsFolder;
        public StorageFolder layoutsFolder;

        Dictionary<Guid, StorageFile> layoutFileMap = new Dictionary<Guid, StorageFile>();

        public async Task CreateAsync(int width, int height, bool extraResources)
        {
            blendModesFolder = await CaseFolder.CreateFolderAsync("BlendModes", CreationCollisionOption.OpenIfExists);
            brushesFolder = await CaseFolder.CreateFolderAsync("Brushes", CreationCollisionOption.OpenIfExists);
            layoutsFolder = await CaseFolder.CreateFolderAsync("Layouts", CreationCollisionOption.OpenIfExists);
            //animationsFolder = await CaseFolder.CreateFolderAsync("Animations", CreationCollisionOption.OpenIfExists);

            canvasCase = new CanvasCase(DeviceResources, width, height);
            canvasCase.DefaultBlendMode = Guid.Parse("9c9f90ac-752c-4db5-bcb5-0880c35c50bf");
            await UpdateDCResource();
            if (extraResources)
                await UpdateDCResourcePlugin();
            await LoadBlendmodes();
            await LoadBrushes();
            canvasCase.PaintAgent.CurrentLayout = canvasCase.NewStandardLayout(0);
            await SaveAsync();
        }

        public async Task LoadAsync()
        {
            blendModesFolder = await CaseFolder.CreateFolderAsync("BlendModes", CreationCollisionOption.OpenIfExists);
            brushesFolder = await CaseFolder.CreateFolderAsync("Brushes", CreationCollisionOption.OpenIfExists);
            layoutsFolder = await CaseFolder.CreateFolderAsync("Layouts", CreationCollisionOption.OpenIfExists);
            //animationsFolder = await CaseFolder.CreateFolderAsync("Animations", CreationCollisionOption.OpenIfExists);

            await LoadDocInfo();

            await LoadBlendmodes();
            await LoadLayouts();
            await LoadBrushes();
        }

        public async Task SaveAsync()
        {
            await SaveDocInfo();

            StorageFile LayoutSettingsFile = await CaseFolder.CreateFileAsync("Layouts.xml", CreationCollisionOption.ReplaceExisting);
            Stream layoutsInfoStream = await LayoutSettingsFile.OpenStreamForWriteAsync();


            var layoutInfos = new _PictureLayouts();
            layoutInfos._PictureLayoutSaves = new List<_PictureLayoutSave>();
            foreach (PictureLayout layout in canvasCase.Layouts)
            {
                layoutInfos._PictureLayoutSaves.Add(new _PictureLayoutSave()
                {
                    Alpha = layout.Alpha,
                    BlendMode = layout.BlendMode,
                    Color = layout.Color,
                    DataSource = layout.DataSource,
                    Guid = layout.guid,
                    Hidden = layout.Hidden,
                    Name = layout.Name,
                    Parameters = layout.parameters.Count > 0 ? new List<Core.ParameterN>(layout.parameters.Values) : null,
                }); ;
            }
            layoutsInfoSerializer.Serialize(layoutsInfoStream, layoutInfos);


            layoutsInfoStream.Dispose();

            foreach (var layout in canvasCase.Layouts)
            {
                if (!layout.saved)
                {
                    if (!layoutFileMap.TryGetValue(layout.guid, out StorageFile storageFile))
                    {
                        storageFile = await layoutsFolder.CreateFileAsync(string.Format("{0}.dclf", layout.guid.ToString(), CreationCollisionOption.ReplaceExisting));
                        layoutFileMap[layout.guid] = storageFile;
                    }
                    await layout.SaveToFileAsync(canvasCase, storageFile);
                }
            }
            HashSet<Guid> existLayoutGuids = new HashSet<Guid>();
            foreach (var layout in canvasCase.Layouts)
            {
                existLayoutGuids.Add(layout.guid);
            }
            foreach (var cmd in canvasCase.UndoManager.undoStack)
            {
                if (cmd is Undo.CMD_DeleteLayout delLCmd)
                    existLayoutGuids.Add(delLCmd.layout.guid);
                else if (cmd is Undo.CMD_RecoverLayout recLCmd)
                    existLayoutGuids.Add(recLCmd.layout.guid);
            }
            foreach (var cmd in canvasCase.UndoManager.redoStack)
            {
                if (cmd is Undo.CMD_DeleteLayout delLCmd)
                    existLayoutGuids.Add(delLCmd.layout.guid);
                else if (cmd is Undo.CMD_RecoverLayout recLCmd)
                    existLayoutGuids.Add(recLCmd.layout.guid);
            }
            List<Guid> delFileGuids = new List<Guid>();
            foreach (var pair in layoutFileMap)
            {
                if (!existLayoutGuids.Contains(pair.Key))
                {
                    delFileGuids.Add(pair.Key);
                    await pair.Value.DeleteAsync();
                }
            }
            foreach (Guid guid in delFileGuids)
            {
                layoutFileMap.Remove(guid);
            }
        }

        private async Task LoadLayouts()
        {
            IReadOnlyList<StorageFile> layoutFiles = await layoutsFolder.GetFilesAsync(Windows.Storage.Search.CommonFileQuery.OrderByName);

            foreach (StorageFile layoutFile in layoutFiles)
            {
                if (!".dclf".Equals(layoutFile.FileType, StringComparison.CurrentCultureIgnoreCase)) continue;
                Guid guid = await DirectCanvasLayoutFormat.LoadFromFileAsync(canvasCase, layoutFile);
                layoutFileMap[guid] = layoutFile;
            }

            StorageFile layoutSettingsFile = await CaseFolder.GetFileAsync("Layouts.xml");
            Stream settingsStream = await layoutSettingsFile.OpenStreamForReadAsync();

            _PictureLayouts layouts = (_PictureLayouts)layoutsInfoSerializer.Deserialize(settingsStream);

            if (layouts._PictureLayoutSaves != null)
            {
                foreach (var layout in layouts._PictureLayoutSaves)
                {
                    PictureLayout pictureLayout = new PictureLayout
                    {
                        Alpha = layout.Alpha,
                        BlendMode = layout.BlendMode,
                        Color = layout.Color,
                        DataSource = layout.DataSource,
                        guid = layout.Guid,
                        Hidden = layout.Hidden,
                        Name = layout.Name,
                        saved = true,
                    };
                    if (layout.Parameters != null)
                        foreach (var parameter in layout.Parameters)
                        {
                            pictureLayout.parameters[parameter.Name] = parameter;
                        }
                    canvasCase.Layouts.Add(pictureLayout);
                }
            }

            settingsStream.Dispose();
        }

        private async Task LoadBlendmodes()
        {
            var BlendmodeFiles = await blendModesFolder.GetFilesAsync(Windows.Storage.Search.CommonFileQuery.OrderByName);
            var blendmodesMap = canvasCase.blendmodesMap;
            foreach (StorageFile blendmodeFile in BlendmodeFiles)
            {
                if (!".dcbm".Equals(blendmodeFile.FileType, StringComparison.CurrentCultureIgnoreCase)) continue;
                var blendmode = await Core.BlendMode.LoadFromFileAsync(canvasCase.DeviceResources, blendmodeFile);
                blendmodesMap.Add(blendmode.Guid, blendmode);
                canvasCase.blendModes.Add(blendmode);
            }
        }

        private async Task LoadBrushes()
        {
            IReadOnlyList<StorageFile> brushFiles = await brushesFolder.GetFilesAsync();


            List<Task> asyncOperations = new List<Task>();
            List<Core.Brush> brushesList = new List<Core.Brush>();
            foreach (StorageFile brushFile in brushFiles)
            {
                if (!".dcbf".Equals(brushFile.FileType, StringComparison.CurrentCultureIgnoreCase)) continue;
                var op2 = Core.Brush.LoadFromFileAsync(brushFile).ContinueWith((_) =>
                {
                    lock (brushesList)
                    {
                        brushesList.AddRange(_.Result);
                    }
                });
                asyncOperations.Add(op2);
            }
            await Task.WhenAll(asyncOperations);
            brushesList.Sort();
            canvasCase.PaintAgent.brushes = new List<Core.Brush>(brushesList);
        }

        private async Task LoadDocInfo()
        {
            StorageFile layoutSettingsFile = await CaseFolder.GetFileAsync("Document.xml");
            Stream settingsStream = await layoutSettingsFile.OpenStreamForReadAsync();

            _DCDocument document = (_DCDocument)docInfoSerializer.Deserialize(settingsStream);
            canvasCase = new CanvasCase(DeviceResources, document.Width, document.Height);
            canvasCase.Name = document.Name;
            canvasCase.Description = document.Description;
            canvasCase.DefaultBlendMode = document.DefaultBlendMode;


            settingsStream.Dispose();
        }

        private async Task SaveDocInfo()
        {
            StorageFile SettingsFile = await CaseFolder.CreateFileAsync("Document.xml", CreationCollisionOption.ReplaceExisting);
            Stream settingsStream = await SettingsFile.OpenStreamForWriteAsync();

            docInfoSerializer.Serialize(settingsStream, new _DCDocument()
            {
                Name = canvasCase.Name,
                Description = canvasCase.Description,
                DefaultBlendMode = canvasCase.DefaultBlendMode,
                Width = canvasCase.Width,
                Height = canvasCase.Height,
            });

            settingsStream.Dispose();
        }

        private async Task UpdateDCResource()
        {
            StorageFolder dcBrushes = await Package.Current.InstalledLocation.GetFolderAsync("DCResources\\Base\\Brushes");
            StorageFolder dcBlendModes = await Package.Current.InstalledLocation.GetFolderAsync("DCResources\\Base\\BlendModes");

            foreach (StorageFile file in await dcBrushes.GetFilesAsync())
            {
                await file.CopyAsync(brushesFolder);
            }
            foreach (StorageFile file in await dcBlendModes.GetFilesAsync())
            {
                await file.CopyAsync(blendModesFolder);
            }
        }

        private async Task UpdateDCResourcePlugin()
        {
            StorageFolder dcBrushes = await Package.Current.InstalledLocation.GetFolderAsync("DCResources\\Plugin\\Brushes");
            StorageFolder dcBlendModes = await Package.Current.InstalledLocation.GetFolderAsync("DCResources\\Plugin\\BlendModes");

            foreach (StorageFile file in await dcBrushes.GetFilesAsync())
            {
                await file.CopyAsync(brushesFolder);
            }
            foreach (StorageFile file in await dcBlendModes.GetFilesAsync())
            {
                await file.CopyAsync(blendModesFolder);
            }
        }
        public static XmlSerializer layoutsInfoSerializer = new XmlSerializer(typeof(_PictureLayouts));
        public static XmlSerializer docInfoSerializer = new XmlSerializer(typeof(_DCDocument));
    }
    [XmlType("DCDocument")]
    public class _DCDocument
    {
        [XmlAttribute]
        public string Name;
        public string Description;
        public int Width;
        public int Height;
        public Guid DefaultBlendMode;

    }

    [XmlType("Layouts")]
    public class _PictureLayouts
    {
        [XmlElement("Layout")]
        public List<_PictureLayoutSave> _PictureLayoutSaves;
    }
    [XmlType("Layout")]
    public class _PictureLayoutSave
    {
        [XmlAttribute]
        public string Name = "";

        public Guid Guid;

        public Guid BlendMode;
        [DefaultValue(false)]
        public bool Hidden;
        [DefaultValue(1.0f)]
        public float Alpha = 1.0f;
        [DefaultValue(PictureDataSource.Default)]
        public PictureDataSource DataSource;

        public Vector4 Color;

        public List<Core.ParameterN> Parameters;
    }
}
