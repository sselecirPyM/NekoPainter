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
        public NekoPainterDocument(DirectoryInfo folder)
        {
            Folder = folder;
        }

        public LivedNekoPainterDocument livedDocument;
        public DirectoryInfo Folder;
        public DirectoryInfo blendModesFolder;
        public DirectoryInfo brushesFolder;
        public DirectoryInfo cachesFolder;
        public DirectoryInfo nodeFolder;
        public DirectoryInfo shadersFolder;

        Dictionary<Guid, FileInfo> layoutFileMap = new Dictionary<Guid, FileInfo>();

        public static JsonConverter[] jsonConverters = new JsonConverter[]
        {
            new Newtonsoft.Json.Converters.StringEnumConverter(),
            new VectorConverter(),
        };

        public void Create(DeviceResources deviceResources, int width, int height, string name)
        {
            FolderDefs();
            InitializeResource();

            livedDocument = new LivedNekoPainterDocument(deviceResources, width, height, Folder.FullName);
            livedDocument.DefaultBlendMode = Guid.Parse("9c9f90ac-752c-4db5-bcb5-0880c35c50bf");
            livedDocument.PaintAgent.CurrentLayout = livedDocument.NewStandardLayout(0);
            livedDocument.Name = name;
            Save();
            LoadDocRes();
        }

        public void Load(DeviceResources deviceResources)
        {
            FolderDefs();
            LoadDocInfo(deviceResources);
            LoadLayouts();
            LoadDocRes();
        }
        private void FolderDefs()
        {
            blendModesFolder = Folder.CreateSubdirectory("BlendModes");
            brushesFolder = Folder.CreateSubdirectory("Brushes");
            cachesFolder = Folder.CreateSubdirectory("Caches");
            nodeFolder = Folder.CreateSubdirectory("Nodes");
            shadersFolder = Folder.CreateSubdirectory("Shaders");
        }

        private void LoadDocRes()
        {
            LoadBlendmodes();
            LoadBrushes();
            LoadNodeDefs();
            LoadShaderDefs();
            foreach (var pair in ReadJsonStream<Dictionary<string, ScriptNodeDef>>(new FileStream("Nodes/NodeDef.json", FileMode.Open, FileAccess.Read)))
            {
                livedDocument.scriptNodeDefs.Add(pair.Key, pair.Value);
            }
        }

        public void Save()
        {
            SaveDocInfo();

            Stream layoutsInfoStream = new FileStream(Folder + "/Layouts.json", FileMode.Create, FileAccess.ReadWrite, FileShare.None);

            WriteJsonStream(layoutsInfoStream, livedDocument.Layouts);
            layoutsInfoStream.Dispose();

            foreach (var layout in livedDocument.Layouts)
            {
                if (!layout.saved)
                {
                    if (!layoutFileMap.TryGetValue(layout.guid, out var storageFile))
                    {
                        storageFile = new FileInfo(Path.Combine(cachesFolder.FullName, string.Format("{0}.dclf", layout.guid.ToString())));
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
            var layoutFiles = cachesFolder.GetFiles();

            foreach (var layoutFile in layoutFiles)
            {
                if (!".dclf".Equals(layoutFile.Extension, StringComparison.CurrentCultureIgnoreCase)) continue;
                Guid guid = CompressedTexFormat.LoadFromFile(livedDocument, layoutFile);
                layoutFileMap[guid] = layoutFile;
            }

            FileInfo layoutSettingsFile = new FileInfo(Folder.FullName + "/Layouts.json");
            Stream settingsStream = layoutSettingsFile.OpenRead();

            List<PictureLayout> layouts = ReadJsonStream<List<PictureLayout>>(settingsStream);
            livedDocument.Layouts = layouts;
            settingsStream.Dispose();

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
            var blendModesMap = livedDocument.blendModesMap;
            foreach (FileInfo file in BlendmodeFiles)
            {
                string relatePath = Path.GetRelativePath(blendModesFolder.FullName, file.FullName);
                if (".cs".Equals(file.Extension, StringComparison.CurrentCultureIgnoreCase))
                {
                    livedDocument.scripts[relatePath] = File.ReadAllText(file.FullName);
                }
                if (".json".Equals(file.Extension, StringComparison.CurrentCultureIgnoreCase))
                {
                    var blendMode = ReadJsonStream<BlendMode>(file.OpenRead());
                    blendModesMap.Add(blendMode.guid, blendMode);
                    livedDocument.blendModes.Add(blendMode);
                }
            }

            foreach (var blendModeDef in livedDocument.blendModes)
            {
                GenerateDefaultVaue(blendModeDef.parameters);
            }
        }

        private void LoadBrushes()
        {
            var brushFiles = brushesFolder.GetFiles();

            foreach (var file in brushFiles)
            {
                if (".json".Equals(file.Extension, StringComparison.CurrentCultureIgnoreCase))
                {
                    var brush1 = ReadJsonStream<Brush>(file.OpenRead());
                    livedDocument.brushes[file.FullName] = brush1;
                }
            }
            livedDocument.PaintAgent.brushes = new List<Brush>(livedDocument.brushes.Values);
            foreach (var nodeDef in livedDocument.PaintAgent.brushes)
            {
                GenerateDefaultVaue(nodeDef.parameters);
            }

        }

        private void LoadNodeDefs()
        {
            var nodeFiles = nodeFolder.GetFiles();

            foreach (var file in nodeFiles)
            {
                string relatePath = Path.GetRelativePath(nodeFolder.FullName, file.FullName);
                if (".cs".Equals(file.Extension, StringComparison.CurrentCultureIgnoreCase))
                {
                    livedDocument.scripts[relatePath] = File.ReadAllText(file.FullName);
                }
                if (".json".Equals(file.Extension, StringComparison.CurrentCultureIgnoreCase))
                {
                    using (var filestream = file.OpenRead())
                    {
                        livedDocument.scriptNodeDefs[relatePath] = ReadJsonStream<ScriptNodeDef>(filestream);
                    }
                }
            }

            foreach (var nodeDef in livedDocument.scriptNodeDefs)
            {
                GenerateDefaultVaue(nodeDef.Value.parameters);
            }
        }

        private void LoadShaderDefs()
        {
            var shaderFiles = shadersFolder.GetFiles();
            foreach (var file in shaderFiles)
            {
                string relatePath = Path.GetRelativePath(shadersFolder.FullName, file.FullName);
                if (".hlsl".Equals(file.Extension, StringComparison.CurrentCultureIgnoreCase))
                {
                    livedDocument.shaders[relatePath] = File.ReadAllText(file.FullName);
                }
                if (".json".Equals(file.Extension, StringComparison.CurrentCultureIgnoreCase))
                {
                    using (var filestream = file.OpenRead())
                    {
                        livedDocument.shaderDefs[relatePath] = ReadJsonStream<ComputeShaderDef>(filestream);
                    }
                }
            }
            foreach (var shaderDef in livedDocument.shaderDefs)
            {
                if (shaderDef.Value.parameters != null)
                    foreach (var param in shaderDef.Value.parameters)
                    {
                        GenerateDefaultVaue(param);
                    }
            }
        }

        public static void GenerateDefaultVaue(List<ScriptNodeParamDef> paramDefs)
        {
            if (paramDefs != null)
                foreach (var param in paramDefs)
                {
                    GenerateDefaultVaue(param);
                }
        }

        public static void GenerateDefaultVaue(ScriptNodeParamDef paramDef)
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
            if (paramDef.type == "int")
            {
                paramDef.defaultValue1 ??= StringConvert.GetInt(paramDef.defaultValue);
            }
            if (paramDef.type == "bool")
            {
                paramDef.defaultValue1 ??= bool.Parse(paramDef.defaultValue);
            }
            if (paramDef.type == "string")
            {
                paramDef.defaultValue1 ??= paramDef.defaultValue;
            }
        }

        private void LoadDocInfo(DeviceResources deviceResources)
        {
            FileInfo layoutSettingsFile = new FileInfo(Folder + "/Document.json");
            Stream settingsStream = layoutSettingsFile.OpenRead();

            _DCDocument document = ReadJsonStream<_DCDocument>(settingsStream);
            livedDocument = new LivedNekoPainterDocument(deviceResources, document.Width, document.Height, Folder.FullName);
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
            var shaders = new DirectoryInfo("DCResources\\Base\\Shaders");
            foreach (var file in brushes.GetFiles())
                file.CopyTo(brushesFolder.FullName + "/" + file.Name);
            foreach (var file in blendModes.GetFiles())
                file.CopyTo(blendModesFolder.FullName + "/" + file.Name);
            foreach (var file in nodes.GetFiles())
                file.CopyTo(nodeFolder.FullName + "/" + file.Name);
            foreach (var file in shaders.GetFiles())
                file.CopyTo(shadersFolder.FullName + "/" + file.Name);

            string path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "NekoPainter/CustomData");
            CopyDir(path, Folder.FullName);//Be careful,it may cause security problem.
        }

        void CopyDir(string source, string targetDir)
        {
            var info = new DirectoryInfo(source);
            var targetInfo = new DirectoryInfo(targetDir);
            if (!targetInfo.Exists)
            {
                targetInfo.Create();
            }
            if (info.Exists)
            {
                foreach (var file in info.GetFiles())
                {
                    var relatePath = Path.GetRelativePath(source, file.FullName);
                    var target = Path.Combine(targetDir, relatePath);
                    System.IO.File.Copy(file.FullName, target);
                }
                foreach (var folder in info.GetDirectories())
                {
                    var targetPath = Path.Combine(targetDir, folder.Name);
                    CopyDir(folder.FullName, targetPath);
                }
            }
        }

        public static T ReadJsonStream<T>(Stream stream)
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

        public static void WriteJsonStream(Stream stream, object serializingObject)
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
}
