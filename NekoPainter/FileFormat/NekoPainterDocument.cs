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
using NekoPainter.Core.UndoCommand;
using NekoPainter.Nodes;
using NekoPainter.Data;
using NekoPainter.Util;

namespace NekoPainter.FileFormat
{
    public class NekoPainterDocument
    {
        public NekoPainterDocument(DeviceResources deviceResources, DirectoryInfo folder)
        {
            this.DeviceResources = deviceResources;
            Folder = folder;
        }

        DeviceResources DeviceResources;
        public LivedNekoPainterDocument livedDocument;
        public DirectoryInfo Folder;
        public DirectoryInfo blendModesFolder;
        public DirectoryInfo brushesFolder;
        public DirectoryInfo cacheFolder;
        public DirectoryInfo nodeFolder;

        Dictionary<Guid, FileInfo> layoutFileMap = new Dictionary<Guid, FileInfo>();

        public static JsonConverter[] jsonConverters = new JsonConverter[]
        {
            new Newtonsoft.Json.Converters.StringEnumConverter(),
            new VectorConverter(),
        };

        public void Create(int width, int height, string name)
        {
            FolderDefs();
            InitializeResource();

            livedDocument = new LivedNekoPainterDocument(DeviceResources, width, height, Folder.FullName);
            livedDocument.DefaultBlendMode = Guid.Parse("9c9f90ac-752c-4db5-bcb5-0880c35c50bf");
            livedDocument.PaintAgent.CurrentLayout = livedDocument.NewStandardLayout(0);
            livedDocument.Name = name;
            Save();
            LoadDocRes();
        }

        public void Load()
        {
            FolderDefs();
            LoadDocInfo();
            LoadLayouts();
            LoadDocRes();
        }
        private void FolderDefs()
        {
            blendModesFolder = Folder.CreateSubdirectory("BlendModes");
            brushesFolder = Folder.CreateSubdirectory("Brushes");
            cacheFolder = Folder.CreateSubdirectory("Caches");
            nodeFolder = Folder.CreateSubdirectory("Nodes");

        }

        private void LoadDocRes()
        {
            LoadBlendmodes();
            LoadBrushes();
            LoadNodeDefs();
            foreach (var pair in ReadJsonStream<Dictionary<string, ScriptNodeDef>>(new FileStream("Nodes/NodeDef.json", FileMode.Open, FileAccess.Read)))
            {
                livedDocument.scriptNodeDefs.Add(pair.Key, pair.Value);
            }
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
                    graph = layout.graph,
                    Guid = layout.guid,
                    Hidden = layout.Hidden,
                    Name = layout.Name,
                    Parameters = layout.parameters.Count > 0 ? new List<Core.ParameterN>(layout.parameters.Values) : null,
                });
            }

            WriteJsonStream(layoutsInfoStream, layoutInfos);
            layoutsInfoStream.Dispose();

            foreach (var layout in livedDocument.Layouts)
            {
                if (!layout.saved)
                {
                    if (!layoutFileMap.TryGetValue(layout.guid, out var storageFile))
                    {
                        storageFile = new FileInfo(Path.Combine(cacheFolder.FullName, string.Format("{0}.dclf", layout.guid.ToString())));
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
                if (cmd is CMD_DeleteLayout delLCmd)
                    existLayoutGuids.Add(delLCmd.layout.guid);
                else if (cmd is CMD_RecoverLayout recLCmd)
                    existLayoutGuids.Add(recLCmd.layout.guid);
            }
            foreach (var cmd in livedDocument.UndoManager.redoStack)
            {
                if (cmd is CMD_DeleteLayout delLCmd)
                    existLayoutGuids.Add(delLCmd.layout.guid);
                else if (cmd is CMD_RecoverLayout recLCmd)
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
            var layoutFiles = cacheFolder.GetFiles();

            foreach (var layoutFile in layoutFiles)
            {
                if (!".dclf".Equals(layoutFile.Extension, StringComparison.CurrentCultureIgnoreCase)) continue;
                Guid guid = NekoPainterLayoutFormat.LoadFromFile(livedDocument, layoutFile);
                layoutFileMap[guid] = layoutFile;
            }

            FileInfo layoutSettingsFile = new FileInfo(Folder.FullName + "/Layouts.json");
            Stream settingsStream = layoutSettingsFile.OpenRead();

            List<_PictureLayoutSave> layouts = ReadJsonStream<List<_PictureLayoutSave>>(settingsStream);

            settingsStream.Dispose();

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
                        graph = layout.graph,
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
            foreach (var layout in livedDocument.Layouts)
            {
                if (!livedDocument.LayoutTex.ContainsKey(layout.guid))
                {
                    layout.generateCache = true;
                    layout.saved = false;
                }
            }
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

            foreach (var brushFile in brushFiles)
            {
                if (".json".Equals(brushFile.Extension, StringComparison.CurrentCultureIgnoreCase))
                {
                    var brush1 = ReadJsonStream<Brush>(brushFile.OpenRead());
                    livedDocument.brushes[brushFile.FullName] = brush1;
                }
            }
            livedDocument.PaintAgent.brushes = new List<Brush>(livedDocument.brushes.Values);
            foreach (var nodeDef in livedDocument.PaintAgent.brushes)
            {
                if (nodeDef.parameters != null)
                    foreach (var param in nodeDef.parameters)
                    {
                        GenerateDefaultVaue(param);
                    }
            }

        }

        private void LoadNodeDefs()
        {
            var nodeFiles = nodeFolder.GetFiles();
            livedDocument.scripts = new Dictionary<string, string>(StringComparer.InvariantCultureIgnoreCase);
            livedDocument.scriptNodeDefs = new Dictionary<string, ScriptNodeDef>(StringComparer.InvariantCultureIgnoreCase);

            foreach (var nodeFile in nodeFiles)
            {
                string relatePath = Path.GetRelativePath(nodeFolder.FullName, nodeFile.FullName);
                if (".cs".Equals(nodeFile.Extension, StringComparison.CurrentCultureIgnoreCase))
                {
                    livedDocument.scripts[relatePath] = File.ReadAllText(nodeFile.FullName);
                }
                if (".json".Equals(nodeFile.Extension, StringComparison.CurrentCultureIgnoreCase))
                {
                    using (var filestream = nodeFile.OpenRead())
                    {
                        livedDocument.scriptNodeDefs[relatePath] = ReadJsonStream<ScriptNodeDef>(filestream);
                    }
                }
            }

            foreach (var nodeDef in livedDocument.scriptNodeDefs)
            {
                if (nodeDef.Value.parameters != null)
                    foreach (var param in nodeDef.Value.parameters)
                    {
                        GenerateDefaultVaue(param);
                    }
            }
        }

        static void GenerateDefaultVaue(ScriptNodeParamDef paramDef)
        {
            if (paramDef.type == "float")
            {
                paramDef.defaultValue1 ??= StringConvert.GetFloat(paramDef.defaultValue);
            }
            if (paramDef.type == "float2")
            {
                paramDef.defaultValue1 ??= StringConvert.GetFloat2(paramDef.defaultValue);
            }
            if (paramDef.type == "float3" || paramDef.type == "color3")
            {
                paramDef.defaultValue1 ??= StringConvert.GetFloat3(paramDef.defaultValue);
            }
            if (paramDef.type == "float4" || paramDef.type == "color4")
            {
                paramDef.defaultValue1 ??= StringConvert.GetFloat4(paramDef.defaultValue);
            }
            if (paramDef.type == "bool")
            {
                paramDef.defaultValue1 ??= bool.Parse(paramDef.defaultValue);
            }
        }

        private void LoadDocInfo()
        {
            FileInfo layoutSettingsFile = new FileInfo(Folder + "/Document.json");
            Stream settingsStream = layoutSettingsFile.OpenRead();

            _DCDocument document = ReadJsonStream<_DCDocument>(settingsStream);
            livedDocument = new LivedNekoPainterDocument(DeviceResources, document.Width, document.Height, Folder.FullName);
            livedDocument.Name = document.Name;
            livedDocument.Description = document.Description;
            livedDocument.DefaultBlendMode = document.DefaultBlendMode;

            settingsStream.Dispose();
        }

        private void SaveDocInfo()
        {
            FileInfo SettingsFile = new FileInfo(Folder + "/Document.json");
            Stream settingsStream = SettingsFile.OpenWrite();

            WriteJsonStream(settingsStream, new _DCDocument()
            {
                Name = livedDocument.Name,
                Description = livedDocument.Description,
                DefaultBlendMode = livedDocument.DefaultBlendMode,
                Width = livedDocument.Width,
                Height = livedDocument.Height,
            });
            settingsStream.Dispose();
        }

        private void InitializeResource()
        {
            var brushes = new DirectoryInfo("DCResources\\Base\\Brushes");
            var blendModes = new DirectoryInfo("DCResources\\Base\\BlendModes");
            var nodes = new DirectoryInfo("DCResources\\Base\\Nodes");

            foreach (var file in brushes.GetFiles())
                file.CopyTo(brushesFolder.FullName + "/" + file.Name);
            foreach (var file in blendModes.GetFiles())
                file.CopyTo(blendModesFolder.FullName + "/" + file.Name);
            foreach (var file in nodes.GetFiles())
                file.CopyTo(nodeFolder.FullName + "/" + file.Name);
        }

        T ReadJsonStream<T>(Stream stream)
        {
            JsonSerializer jsonSerializer = new JsonSerializer();
            jsonSerializer.NullValueHandling = NullValueHandling.Ignore;
            foreach (var converter in jsonConverters)
                jsonSerializer.Converters.Add(converter);
            using (StreamReader reader1 = new StreamReader(stream))
            {
                return jsonSerializer.Deserialize<T>(new JsonTextReader(reader1));
            }
        }

        void WriteJsonStream(Stream stream, object serializingObject)
        {
            JsonSerializer jsonSerializer = new JsonSerializer();
            jsonSerializer.NullValueHandling = NullValueHandling.Ignore;
            jsonSerializer.DefaultValueHandling = DefaultValueHandling.Ignore;
            foreach (var converter in jsonConverters)
                jsonSerializer.Converters.Add(converter);

            using (StreamWriter writer1 = new StreamWriter(stream))
            using (JsonTextWriter jsonWriter = new JsonTextWriter(writer1))
            {
                jsonSerializer.Serialize(jsonWriter, serializingObject);
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
        [DefaultValue(LayoutDataSource.Default)]
        public LayoutDataSource DataSource;

        public Vector4 Color;

        public Graph graph;

        public List<Core.ParameterN> Parameters;
    }
}
