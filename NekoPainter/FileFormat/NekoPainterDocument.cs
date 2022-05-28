using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NekoPainter.Core;
using System.IO;
using CanvasRendering;
using System.Numerics;
using Newtonsoft.Json;
using NekoPainter.Core.UndoCommand;
using NekoPainter.Core.Nodes;
using NekoPainter.Data;
using NekoPainter.Core.Util;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace NekoPainter.FileFormat
{
    public class NekoPainterDocument : IDisposable
    {
        public NekoPainterDocument(DirectoryInfo folder)
        {
            this.folder = folder;
        }

        public LivedNekoPainterDocument livedDocument;
        public DirectoryInfo folder;
        public DirectoryInfo blendModesFolder;
        public DirectoryInfo brushesFolder;
        public DirectoryInfo cachesFolder;
        public DirectoryInfo nodeFolder;
        public DirectoryInfo shadersFolder;

        Dictionary<Guid, FileInfo> cacheFileMap = new Dictionary<Guid, FileInfo>();

        public DeviceResources DeviceResources;
        public RenderTexture Output;

        public PaintAgent PaintAgent { get; private set; } = new PaintAgent();
        public UndoManager UndoManager = new UndoManager();

        public static JsonConverter[] jsonConverters = new JsonConverter[]
        {
            new Newtonsoft.Json.Converters.StringEnumConverter(),
            new VectorConverter(),
        };

        public void Create(DeviceResources deviceResources, int width, int height, string name)
        {
            DeviceResources = deviceResources;
            FolderDefs();
            InitializeResource();

            livedDocument = new LivedNekoPainterDocument(width, height);
            livedDocument.DefaultBlendMode = Guid.Parse("9c9f90ac-752c-4db5-bcb5-0880c35c50bf");
            livedDocument.Name = name;
            Output = new RenderTexture(DeviceResources, width, height, Vortice.DXGI.Format.R32G32B32A32_Float, false);
            PaintAgent.CurrentLayout = NewLayout(0);
            PaintAgent.document = livedDocument;
            PaintAgent.UndoManager = UndoManager;
            Save();
            LoadDocRes();
        }

        public void Load(DeviceResources deviceResources)
        {
            FolderDefs();
            LoadDocInfo(deviceResources);
            LoadCaches();
            LoadDocRes();
        }

        private void LoadDocInfo(DeviceResources deviceResources)
        {
            DeviceResources = deviceResources;
            FileInfo layoutSettingsFile = new FileInfo(folder + "/Document.json");
            Stream settingsStream = layoutSettingsFile.OpenRead();

            _Document document = ReadJsonStream<_Document>(settingsStream);
            livedDocument = new LivedNekoPainterDocument(document.Width, document.Height);
            livedDocument.Name = document.Name;
            livedDocument.Description = document.Description;
            livedDocument.DefaultBlendMode = document.DefaultBlendMode;

            Output = new RenderTexture(DeviceResources, livedDocument.Width, livedDocument.Height, Vortice.DXGI.Format.R32G32B32A32_Float, false);
            PaintAgent.document = livedDocument;
            PaintAgent.UndoManager = UndoManager;

            settingsStream.Dispose();
        }

        public void SetBlendMode(PictureLayout layout, BlendMode blendMode)
        {
            UndoManager.AddUndoData(new CMD_BlendModeChange(layout, layout.BlendMode));
            layout.BlendMode = blendMode.guid;
        }

        public void SetActivatedLayout(int layoutIndex)
        {
            if (layoutIndex == -1)
            {
                livedDocument.ActivatedLayout = null;
                PaintAgent.CurrentLayout = null;
                return;
            }
            SetActivatedLayout(livedDocument.Layouts[layoutIndex]);
        }

        public void SetActivatedLayout(PictureLayout layout)
        {
            livedDocument.ActivatedLayout = layout;

            PaintAgent.CurrentLayout = livedDocument.ActivatedLayout;
        }

        public void DeleteLayout(int index)
        {
            PictureLayout pictureLayout = livedDocument.Layouts[index];
            if (PaintAgent.CurrentLayout == pictureLayout)
            {
                PaintAgent.CurrentLayout = null;
            }
            livedDocument.Layouts.RemoveAt(index);
            UndoManager.AddUndoData(new CMD_RecoverLayout(pictureLayout, livedDocument, this, index));
        }

        public PictureLayout CopyLayout(int index)
        {
            PictureLayout pictureLayout = livedDocument.Layouts[index];
            livedDocument.LayoutTex.TryGetValue(pictureLayout.guid, out var tiledTexture);

            TiledTexture newTiledTexture = new TiledTexture(tiledTexture);

            PictureLayout newPictureLayout = new PictureLayout(pictureLayout)
            {
                Name = string.Format("{0} 复制", pictureLayout.Name),
            };

            livedDocument.LayoutTex[newPictureLayout.guid] = newTiledTexture;
            livedDocument.Layouts.Insert(index, newPictureLayout);
            UndoManager.AddUndoData(new CMD_DeleteLayout(newPictureLayout, livedDocument, this, index));
            return newPictureLayout;
        }

        public PictureLayout NewLayout(int insertIndex)
        {
            PictureLayout standardLayout = new PictureLayout()
            {
                BlendMode = livedDocument.DefaultBlendMode,
                guid = System.Guid.NewGuid(),
                Name = string.Format("图层 {0}", livedDocument.Layouts.Count + 1)
            };
            livedDocument.Layouts.Insert(insertIndex, standardLayout);
            UndoManager.AddUndoData(new CMD_DeleteLayout(standardLayout, livedDocument, this, insertIndex));

            return standardLayout;
        }
        private void FolderDefs()
        {
            blendModesFolder = folder.CreateSubdirectory("BlendModes");
            brushesFolder = folder.CreateSubdirectory("Brushes");
            cachesFolder = folder.CreateSubdirectory("Caches");
            nodeFolder = folder.CreateSubdirectory("Nodes");
            shadersFolder = folder.CreateSubdirectory("Shaders");
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

            Stream layoutsInfoStream = new FileStream(folder + "/Layouts.json", FileMode.Create, FileAccess.ReadWrite, FileShare.None);

            WriteJsonStream(layoutsInfoStream, livedDocument.Layouts);
            layoutsInfoStream.Dispose();

            foreach (var layout in livedDocument.Layouts)
            {
                if (!layout.saved)
                {
                    if (!cacheFileMap.TryGetValue(layout.guid, out var storageFile))
                    {
                        storageFile = new FileInfo(Path.Combine(cachesFolder.FullName, string.Format("{0}.dclf", layout.guid.ToString())));
                        cacheFileMap[layout.guid] = storageFile;
                    }
                    layout.SaveToFile(livedDocument, storageFile);
                }
            }
            HashSet<Guid> existLayoutGuids = new HashSet<Guid>();
            foreach (var layout in livedDocument.Layouts)
            {
                existLayoutGuids.Add(layout.guid);
            }
            foreach (var cmd in UndoManager.undoStack)
            {
                if (cmd is CMD_DeleteLayout delLCmd)
                    existLayoutGuids.Add(delLCmd.layout.guid);
                else if (cmd is CMD_RecoverLayout recLCmd)
                    existLayoutGuids.Add(recLCmd.layout.guid);
            }
            foreach (var cmd in UndoManager.redoStack)
            {
                if (cmd is CMD_DeleteLayout delLCmd)
                    existLayoutGuids.Add(delLCmd.layout.guid);
                else if (cmd is CMD_RecoverLayout recLCmd)
                    existLayoutGuids.Add(recLCmd.layout.guid);
            }
            List<Guid> delFileGuids = new List<Guid>();
            foreach (var pair in cacheFileMap)
            {
                if (!existLayoutGuids.Contains(pair.Key))
                {
                    delFileGuids.Add(pair.Key);
                    pair.Value.Delete();
                }
            }
            foreach (Guid guid in delFileGuids)
            {
                cacheFileMap.Remove(guid);
            }
        }

        private void LoadCaches()
        {
            var cacheFiles = cachesFolder.GetFiles();

            foreach (var cacheFile in cacheFiles)
            {
                if (!".dclf".Equals(cacheFile.Extension, StringComparison.CurrentCultureIgnoreCase)) continue;
                Guid guid = CompressedTexFormat.LoadFromFile(livedDocument, cacheFile);
                cacheFileMap[guid] = cacheFile;
            }

            FileInfo layoutSettingsFile = new FileInfo(folder.FullName + "/Layouts.json");
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
            PaintAgent.brushes = new List<Brush>(livedDocument.brushes.Values);
            foreach (var nodeDef in PaintAgent.brushes)
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

        private void SaveDocInfo()
        {
            FileInfo SettingsFile = new FileInfo(folder + "/Document.json");
            Stream settingsStream = SettingsFile.OpenWrite();

            WriteJsonStream(settingsStream, new _Document()
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
            CopyDir(path, folder.FullName);//Be careful,it may cause security problem.
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


        public void ImportImage( string path)
        {
            var document = this;
            var livedNekoPainterDocument = document.livedDocument;
            if (livedNekoPainterDocument?.ActivatedLayout == null) return;
            var layout = livedNekoPainterDocument.ActivatedLayout;
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

            layout.graph.AddNodeToEnd(fileNode, new Vector2(10, -20));
            layout.graph.AddNodeToEnd(scriptNode, new Vector2(70, 0));
            layout.graph.Link(fileNode.Luid, "bytes", scriptNode.Luid, "file");
            CMD_Remove_RecoverNodes cmd = new CMD_Remove_RecoverNodes();
            cmd.BuildRemoveNodes(livedNekoPainterDocument, layout.graph, new List<int>() { fileNode.Luid, scriptNode.Luid }, layout.guid);
            document.UndoManager.AddUndoData(cmd);
        }

        public void ExportImage( string path)
        {
            var document = this;
            var output = document.Output;
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

        public void Dispose()
        {
            Output.Dispose();
            livedDocument.Dispose();
            UndoManager.Dispose();
        }
    }
    public class _Document
    {
        public string Name;
        public string Description;
        public int Width;
        public int Height;
        public Guid DefaultBlendMode;
    }
}
