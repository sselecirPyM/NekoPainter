using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NekoPainter.Core;
using System.IO;
using System.Xml;
using CanvasRendering;
using System.Xml.Serialization;
using System.ComponentModel;
using System.Numerics;
using System.Collections.Concurrent;

namespace NekoPainter.FileFormat
{
    public class NekoPainterDocument
    {
        public NekoPainterDocument(DeviceResources deviceResources, DirectoryInfo caseFolder)
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

        public DirectoryInfo CaseFolder;
        DeviceResources DeviceResources;
        public CanvasCase canvasCase;
        public DirectoryInfo blendModesFolder;
        public DirectoryInfo brushesFolder;

        public DirectoryInfo layoutsFolder;

        Dictionary<Guid, FileInfo> layoutFileMap = new Dictionary<Guid, FileInfo>();

        public void CreateAsync(int width, int height, bool extraResources)
        {
            blendModesFolder = Directory.CreateDirectory(CaseFolder + "/BlendModes");
            brushesFolder = Directory.CreateDirectory(CaseFolder + "/Brushes");
            layoutsFolder = Directory.CreateDirectory(CaseFolder + "/Layouts");

            canvasCase = new CanvasCase(DeviceResources, width, height);
            canvasCase.DefaultBlendMode = Guid.Parse("9c9f90ac-752c-4db5-bcb5-0880c35c50bf");
            UpdateDCResource();
            if (extraResources)
                UpdateDCResourcePlugin();
            LoadBlendmodes();
            LoadBrushes();
            canvasCase.PaintAgent.CurrentLayout = canvasCase.NewStandardLayout(0);
            SaveAsync();
        }

        public void LoadAsync()
        {
            blendModesFolder = Directory.CreateDirectory(CaseFolder + "/BlendModes");
            brushesFolder = Directory.CreateDirectory(CaseFolder + "/Brushes");
            layoutsFolder = Directory.CreateDirectory(CaseFolder + "/Layouts");

            LoadDocInfo();

            LoadBlendmodes();
            LoadLayouts();
            LoadBrushes();
        }

        public void SaveAsync()
        {
            SaveDocInfo();

            Stream layoutsInfoStream = new FileStream(CaseFolder + "/Layouts.xml", FileMode.Create, FileAccess.ReadWrite, FileShare.None);

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
                });
            }
            layoutsInfoSerializer.Serialize(layoutsInfoStream, layoutInfos);


            layoutsInfoStream.Dispose();

            foreach (var layout in canvasCase.Layouts)
            {
                if (!layout.saved)
                {
                    if (!layoutFileMap.TryGetValue(layout.guid, out var storageFile))
                    {
                        //storageFile = await layoutsFolder.CreateFileAsync(, CreationCollisionOption.ReplaceExisting);
                        storageFile = new FileInfo(string.Format("{0}.dclf", layout.guid.ToString()));
                        layoutFileMap[layout.guid] = storageFile;
                    }
                    layout.SaveToFileAsync(canvasCase, storageFile);
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
                Guid guid = NekoPainterLayoutFormat.LoadFromFileAsync(canvasCase, layoutFile);
                layoutFileMap[guid] = layoutFile;
            }

            FileInfo layoutSettingsFile = new FileInfo(CaseFolder.FullName + "/Layouts.xml");
            Stream settingsStream = layoutSettingsFile.OpenRead();

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

        private void LoadBlendmodes()
        {
            var BlendmodeFiles = blendModesFolder.GetFiles();
            var blendmodesMap = canvasCase.blendmodesMap;
            foreach (FileInfo blendmodeFile in BlendmodeFiles)
            {
                if (!".dcbm".Equals(blendmodeFile.Extension, StringComparison.CurrentCultureIgnoreCase)) continue;
                var blendmode = Core.BlendMode.LoadFromFileAsync(canvasCase.DeviceResources, blendmodeFile.FullName);
                blendmodesMap.Add(blendmode.Guid, blendmode);
                canvasCase.blendModes.Add(blendmode);
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
            canvasCase.PaintAgent.brushes = new List<Core.Brush>(brushesList);
        }

        private void LoadDocInfo()
        {
            FileInfo layoutSettingsFile = new FileInfo(CaseFolder + "/Document.xml");
            Stream settingsStream = layoutSettingsFile.OpenRead();

            _DCDocument document = (_DCDocument)docInfoSerializer.Deserialize(settingsStream);
            canvasCase = new CanvasCase(DeviceResources, document.Width, document.Height);
            canvasCase.Name = document.Name;
            canvasCase.Description = document.Description;
            canvasCase.DefaultBlendMode = document.DefaultBlendMode;

            settingsStream.Dispose();
        }

        private void SaveDocInfo()
        {
            FileInfo SettingsFile = new FileInfo(CaseFolder + "/Document.xml");
            Stream settingsStream = SettingsFile.OpenWrite();

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

        private void UpdateDCResourcePlugin()
        {
            var dcBrushes = new DirectoryInfo("DCResources\\Plugin\\Brushes");
            var dcBlendModes = new DirectoryInfo("DCResources\\Plugin\\BlendModes");

            foreach (var file in dcBrushes.GetFiles())
            {
                file.CopyTo(brushesFolder.FullName + "/" + file.Name);
            }
            foreach (var file in dcBlendModes.GetFiles())
            {
                file.CopyTo(blendModesFolder.FullName + "/" + file.Name);
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
