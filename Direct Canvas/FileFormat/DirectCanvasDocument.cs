using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Storage;
using Windows.Storage.Streams;
using Windows.Foundation;
using DirectCanvas.Layout;
using System.IO;
using System.Xml;
using CanvasRendering;
using Windows.ApplicationModel;

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
        public StorageFolder animationsFolder;
        public StorageFolder layoutsFolder;

        Dictionary<Guid, StorageFile> layoutFileMap = new Dictionary<Guid, StorageFile>();

        Guid defaultBlendModeGuid;
        public async Task CreateAsync(int width, int height, bool extraResources)
        {
            blendModesFolder = await CaseFolder.CreateFolderAsync("BlendModes", CreationCollisionOption.OpenIfExists);
            brushesFolder = await CaseFolder.CreateFolderAsync("Brushes", CreationCollisionOption.OpenIfExists);
            layoutsFolder = await CaseFolder.CreateFolderAsync("Layouts", CreationCollisionOption.OpenIfExists);
            animationsFolder = await CaseFolder.CreateFolderAsync("Animations", CreationCollisionOption.OpenIfExists);

            canvasCase = new CanvasCase(DeviceResources, width, height);
            defaultBlendModeGuid = Guid.Parse("9c9f90ac-752c-4db5-bcb5-0880c35c50bf");
            await UpdateDCResource();
            if (extraResources)
                await UpdateDCResourcePlugin();
            await LoadBlendmodes();
            await LoadBrushes();
            canvasCase.PaintAgent.CurrentLayout = canvasCase.NewStandardLayout(0, 0);
            await SaveAsync();
        }

        public async Task LoadAsync()
        {
            blendModesFolder = await CaseFolder.CreateFolderAsync("BlendModes", CreationCollisionOption.OpenIfExists);
            brushesFolder = await CaseFolder.CreateFolderAsync("Brushes", CreationCollisionOption.OpenIfExists);
            layoutsFolder = await CaseFolder.CreateFolderAsync("Layouts", CreationCollisionOption.OpenIfExists);
            animationsFolder = await CaseFolder.CreateFolderAsync("Animations", CreationCollisionOption.OpenIfExists);

            await LoadDocInfo();

            await LoadBlendmodes();
            await LoadLayouts();
            await LoadBrushes();
        }

        public async Task SaveAsync()
        {
            await SaveDocInfo();

            StorageFile LayoutSettingsFile = await CaseFolder.CreateFileAsync("Layouts.xml", CreationCollisionOption.ReplaceExisting);
            Stream layoutsInfoStream = (await LayoutSettingsFile.OpenAsync(FileAccessMode.ReadWrite)).AsStream();
            XmlWriter writer = XmlWriter.Create(layoutsInfoStream, xmlWriterSettings);
            writer.WriteStartDocument();
            writer.WriteStartElement("Layouts");
            for (int ia = 0; ia < canvasCase.Layouts.Count; ia++)
            {
                PictureLayout layout = canvasCase.Layouts[ia];
                writer.WriteStartElement("Layout");
                System.Numerics.Vector4 color = layout.Color;
                writer.WriteAttributeString("Color", string.Format("{0} {1} {2} {3}", color.X, color.Y, color.Z, color.W));
                writer.WriteAttributeString("Name", layout.Name);
                writer.WriteAttributeString("UseColor", layout.UseColor.ToString());
                writer.WriteAttributeString("Guid", layout.guid.ToString());
                writer.WriteAttributeString("Hidden", layout.Hidden.ToString());
                writer.WriteAttributeString("Alpha", layout.Alpha.ToString());
                if (layout.BlendMode != null)
                    writer.WriteAttributeString("BlendModeGuid", layout.BlendMode.ToString());

                writer.WriteEndElement();
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
            writer.WriteEndDocument();
            writer.Flush();
            layoutsInfoStream.Dispose();
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
            Stream settingsStream = (await layoutSettingsFile.OpenAsync(FileAccessMode.ReadWrite)).AsStream();
            XmlReader xmlReader = XmlReader.Create(settingsStream, xmlReaderSettings);
            while (xmlReader.Read())
            {
                if (xmlReader.NodeType == XmlNodeType.Element)
                {
                    Guid guid;
                    switch (xmlReader.Name)
                    {
                        case "Layout":
                            {
                                guid = Guid.Parse(xmlReader.GetAttribute("Guid"));
                                PictureLayout standardLayout = new PictureLayout() { guid = guid };
                                canvasCase.LayoutsMap[guid] = standardLayout;
                                LoadLayoutInfo(xmlReader, standardLayout);

                                string colorString = xmlReader.GetAttribute("Color");
                                if (!string.IsNullOrEmpty(colorString))
                                {
                                    string[] colorG = colorString.Split(" ");
                                    float.TryParse(colorG[0], out float cR);
                                    float.TryParse(colorG[1], out float cG);
                                    float.TryParse(colorG[2], out float cB);
                                    float.TryParse(colorG[3], out float cA);
                                    standardLayout.Color = new System.Numerics.Vector4(cR, cG, cB, cA);
                                }

                                canvasCase.Layouts.Add(standardLayout);
                                break;
                            }
                    }
                }
            }
            settingsStream.Dispose();
        }
        private void LoadLayoutInfo(XmlReader xmlReader, PictureLayout layout)
        {
            if (Guid.TryParse(xmlReader.GetAttribute("BlendModeGuid"), out Guid blendmodeGuid))
                layout.BlendMode = blendmodeGuid;
            if (bool.TryParse(xmlReader.GetAttribute("Hidden"), out bool hidden))
                layout.Hidden = hidden;
            if (float.TryParse(xmlReader.GetAttribute("Alpha"), out float alpha))
                layout.Alpha = alpha;
            if (bool.TryParse(xmlReader.GetAttribute("UseColor"), out bool useColor))
                layout.UseColor = useColor;

            layout.Name = xmlReader.GetAttribute("Name");
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
            if (defaultBlendModeGuid != Guid.Empty)
                canvasCase.DefaultBlendMode = blendmodesMap[defaultBlendModeGuid];
        }

        private async Task LoadBrushes()
        {
            IReadOnlyList<StorageFile> brushFiles = await brushesFolder.GetFilesAsync();


            List<Task> asyncOperations = new List<Task>();
            List<Core.Brush> brushesList = new List<Core.Brush>();
            foreach (StorageFile brushFile in brushFiles)
            {
                if (!".dcbf".Equals(brushFile.FileType, StringComparison.CurrentCultureIgnoreCase)) continue;
                var op2 = Core.Brush.LoadFromFileAsync(canvasCase.DeviceResources, brushFile).ContinueWith((_) =>
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
            XmlReader xmlReader = XmlReader.Create(settingsStream);
            while (xmlReader.Read())
            {
                if (xmlReader.NodeType == XmlNodeType.Element)
                {
                    switch (xmlReader.Name)
                    {
                        case "DCDocument":
                            int width = int.Parse(xmlReader.GetAttribute("Width"));
                            int height = int.Parse(xmlReader.GetAttribute("Height"));
                            canvasCase = new CanvasCase(DeviceResources, width, height);
                            while (xmlReader.Read())
                            {
                                if (xmlReader.NodeType == XmlNodeType.Element)
                                {
                                    switch (xmlReader.Name)
                                    {
                                        case "Name":
                                            canvasCase.Name = xmlReader.ReadElementContentAsString();
                                            continue;
                                        case "Description":
                                            canvasCase.Description = xmlReader.ReadElementContentAsString();
                                            continue;
                                        case "DefaultBlendModeGuid":
                                            if (Guid.TryParse(xmlReader.GetAttribute("Value"), out Guid guid))
                                                defaultBlendModeGuid = guid;
                                            break;
                                    }
                                    xmlReader.Skip();
                                }
                                else if (xmlReader.NodeType == XmlNodeType.EndElement && xmlReader.Name == "DCDocument")
                                    break;
                            }
                            break;
                    }
                }
            }
        }

        private async Task SaveDocInfo()
        {
            StorageFile SettingsFile = await CaseFolder.CreateFileAsync("Document.xml", CreationCollisionOption.ReplaceExisting);
            Stream settingsStream = (await SettingsFile.OpenAsync(FileAccessMode.ReadWrite)).AsStream();
            XmlWriter writer = XmlWriter.Create(settingsStream, xmlWriterSettings);
            writer.WriteStartDocument();
            writer.WriteStartElement("DCDocument");
            writer.WriteAttributeString("Width", canvasCase.Width.ToString());
            writer.WriteAttributeString("Height", canvasCase.Height.ToString());

            if (!string.IsNullOrEmpty(canvasCase.Name))
            {
                writer.WriteStartElement("Name");
                writer.WriteString(canvasCase.Name);
                writer.WriteEndElement();
            }
            if (!string.IsNullOrEmpty(canvasCase.Description))
            {
                writer.WriteStartElement("Description");
                writer.WriteString(canvasCase.Name);
                writer.WriteEndElement();
            }
            if (canvasCase.DefaultBlendMode != null)
            {
                writer.WriteStartElement("DefaultBlendModeGuid");
                writer.WriteAttributeString("Value", canvasCase.DefaultBlendMode.Guid.ToString());
                writer.WriteEndElement();
            }

            writer.WriteEndDocument();
            writer.Flush();
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
    }
}
