using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NekoPainter.Core;
using System.IO;
using CanvasRendering;
using System.ComponentModel;
using System.Numerics;
using Newtonsoft.Json;

namespace NekoPainter.FileFormat
{
    public class NekoPainterDocument
    {
        public NekoPainterDocument(DeviceResources deviceResources, DirectoryInfo caseFolder)
        {
            Folder = caseFolder;
            this.DeviceResources = deviceResources;
        }

        public DirectoryInfo Folder;
        DeviceResources DeviceResources;
        public LivedNekoPainterDocument livedDocument;
        public DirectoryInfo blendModesFolder;
        public DirectoryInfo brushesFolder;

        public DirectoryInfo layoutsFolder;

        Dictionary<Guid, FileInfo> layoutFileMap = new Dictionary<Guid, FileInfo>();

        public void Create(int width, int height, string name)
        {
            blendModesFolder = Directory.CreateDirectory(Folder + "/BlendModes");
            brushesFolder = Directory.CreateDirectory(Folder + "/Brushes");
            layoutsFolder = Directory.CreateDirectory(Folder + "/Layouts");

            livedDocument = new LivedNekoPainterDocument(DeviceResources, width, height, Folder.FullName);
            livedDocument.DefaultBlendMode = Guid.Parse("9c9f90ac-752c-4db5-bcb5-0880c35c50bf");
            UpdateDCResource();
            LoadBlendmodes();
            LoadBrushes();
            livedDocument.PaintAgent.CurrentLayout = livedDocument.NewStandardLayout(0);
            livedDocument.Name = name;
            Save();
        }

        public void Load()
        {
            blendModesFolder = Directory.CreateDirectory(Folder + "/BlendModes");
            brushesFolder = Directory.CreateDirectory(Folder + "/Brushes");
            layoutsFolder = Directory.CreateDirectory(Folder + "/Layouts");

            LoadDocInfo();

            LoadBlendmodes();
            LoadLayouts();
            LoadBrushes();
        }

        public void Save()
        {
            SaveDocInfo();

            Stream layoutsInfoStream = new FileStream(Folder + "/Layouts.json", FileMode.Create, FileAccess.ReadWrite, FileShare.None);

            List<_PictureLayoutSave> layoutInfos = new List<_PictureLayoutSave>();
            foreach (PictureLayout layout in livedDocument.Layouts)
            {
                layoutInfos.Add(new _PictureLayoutSave()
                {
                    Alpha = layout.Alpha,
                    BlendMode = layout.BlendMode,
                    Color = layout.Color,
                    DataSource = layout.DataSource,
                    Guid = layout.guid,
                    Hidden = layout.Hidden,
                    Name = layout.Name,
                    Parameters = layout.parameters.Count > 0 ? new List<Core.ParameterN>(layout.parameters.Values) : null,
                });
            }
            JsonSerializerSettings jsonSerializerSettings = new JsonSerializerSettings();
            jsonSerializerSettings.NullValueHandling = NullValueHandling.Ignore;
            string json = JsonConvert.SerializeObject(layoutInfos, jsonSerializerSettings);
            StreamWriter writer1 = new StreamWriter(layoutsInfoStream);
            writer1.Write(json);
            writer1.Dispose();
            layoutsInfoStream.Dispose();

            foreach (var layout in livedDocument.Layouts)
            {
                if (!layout.saved)
                {
                    if (!layoutFileMap.TryGetValue(layout.guid, out var storageFile))
                    {
                        //storageFile = await layoutsFolder.CreateFileAsync(, CreationCollisionOption.ReplaceExisting);
                        storageFile = new FileInfo(Path.Combine(layoutsFolder.FullName, string.Format("{0}.dclf", layout.guid.ToString())));
                        layoutFileMap[layout.guid] = storageFile;
                    }
                    layout.SaveToFile(livedDocument, storageFile);
                }
            }
            HashSet<Guid> existLayoutGuids = new HashSet<Guid>();
            foreach (var layout in livedDocument.Layouts)
            {
                existLayoutGuids.Add(layout.guid);
            }
            foreach (var cmd in livedDocument.UndoManager.undoStack)
            {
                if (cmd is Undo.CMD_DeleteLayout delLCmd)
                    existLayoutGuids.Add(delLCmd.layout.guid);
                else if (cmd is Undo.CMD_RecoverLayout recLCmd)
                    existLayoutGuids.Add(recLCmd.layout.guid);
            }
            foreach (var cmd in livedDocument.UndoManager.redoStack)
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
                    pair.Value.Delete();
                }
            }
            foreach (Guid guid in delFileGuids)
            {
                layoutFileMap.Remove(guid);
            }
        }

        private void LoadLayouts()
        {
            var layoutFiles = layoutsFolder.GetFiles();

            foreach (var layoutFile in layoutFiles)
            {
                if (!".dclf".Equals(layoutFile.Extension, StringComparison.CurrentCultureIgnoreCase)) continue;
                Guid guid = NekoPainterLayoutFormat.LoadFromFileAsync(livedDocument, layoutFile);
                layoutFileMap[guid] = layoutFile;
            }

            FileInfo layoutSettingsFile = new FileInfo(Folder.FullName + "/Layouts.json");
            Stream settingsStream = layoutSettingsFile.OpenRead();


            var reader = new StreamReader(settingsStream);

            List<_PictureLayoutSave> layouts = JsonConvert.DeserializeObject<List<_PictureLayoutSave>>(reader.ReadToEnd());

            if (layouts != null)
            {
                foreach (var layout in layouts)
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
                    livedDocument.Layouts.Add(pictureLayout);
                }
            }

            settingsStream.Dispose();
        }

        private void LoadBlendmodes()
        {
            var BlendmodeFiles = blendModesFolder.GetFiles();
            var blendmodesMap = livedDocument.blendmodesMap;
            foreach (FileInfo blendmodeFile in BlendmodeFiles)
            {
                if (!".dcbm".Equals(blendmodeFile.Extension, StringComparison.CurrentCultureIgnoreCase)) continue;
                var blendmode = Core.BlendMode.LoadFromFileAsync(livedDocument.DeviceResources, blendmodeFile.FullName);
                blendmodesMap.Add(blendmode.Guid, blendmode);
                livedDocument.blendModes.Add(blendmode);
            }
        }

        private void LoadBrushes()
        {
            var brushFiles = brushesFolder.GetFiles();

            List<Core.Brush> brushesList = new List<Core.Brush>();
            foreach (var brushFile in brushFiles)
            {
                if (!".dcbf".Equals(brushFile.Extension, StringComparison.CurrentCultureIgnoreCase)) continue;
                var brushes = Core.Brush.LoadFromFileAsync(brushFile);
                lock (brushesList)
                {
                    brushesList.AddRange(brushes);
                }
            }
            brushesList.Sort();
            livedDocument.PaintAgent.brushes = new List<Core.Brush>(brushesList);
        }

        private void LoadDocInfo()
        {
            FileInfo layoutSettingsFile = new FileInfo(Folder + "/Document.json");
            Stream settingsStream = layoutSettingsFile.OpenRead();
            StreamReader reader = new StreamReader(settingsStream);
            _DCDocument document = JsonConvert.DeserializeObject<_DCDocument>(reader.ReadToEnd());
            livedDocument = new LivedNekoPainterDocument(DeviceResources, document.Width, document.Height, Folder.FullName);
            livedDocument.Name = document.Name;
            livedDocument.Description = document.Description;
            livedDocument.DefaultBlendMode = document.DefaultBlendMode;

            settingsStream.Dispose();
        }

        private void SaveDocInfo()
        {
            JsonSerializerSettings jsonSerializerSettings = new JsonSerializerSettings();
            jsonSerializerSettings.NullValueHandling = NullValueHandling.Ignore;

            FileInfo SettingsFile = new FileInfo(Folder + "/Document.json");
            Stream settingsStream = SettingsFile.OpenWrite();
            StreamWriter streamWriter = new StreamWriter(settingsStream);
            streamWriter.Write(JsonConvert.SerializeObject(new _DCDocument()
            {
                Name = livedDocument.Name,
                Description = livedDocument.Description,
                DefaultBlendMode = livedDocument.DefaultBlendMode,
                Width = livedDocument.Width,
                Height = livedDocument.Height,
            }, jsonSerializerSettings));
            streamWriter.Dispose();
            settingsStream.Dispose();
        }

        private void UpdateDCResource()
        {
            var dcBrushes = new DirectoryInfo("DCResources\\Base\\Brushes");
            var dcBlendModes = new DirectoryInfo("DCResources\\Base\\BlendModes");

            foreach (var file in dcBrushes.GetFiles())
            {
                file.CopyTo(brushesFolder.FullName + "/" + file.Name);
            }
            foreach (var file in dcBlendModes.GetFiles())
            {
                file.CopyTo(blendModesFolder.FullName + "/" + file.Name);
            }
        }
    }
    public class _DCDocument
    {
        public string Name;
        public string Description;
        public int Width;
        public int Height;
        public Guid DefaultBlendMode;

    }

    public class _PictureLayoutSave
    {
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
