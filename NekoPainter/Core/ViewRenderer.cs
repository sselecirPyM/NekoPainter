using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;
using NekoPainter.Core;
using NekoPainter.Util;
using NekoPainter.Nodes;
using CanvasRendering;
using System.Numerics;
using System.Runtime.InteropServices;
using System.IO;
using System;
using Microsoft.CodeAnalysis.CSharp.Scripting;
using Microsoft.CodeAnalysis.Scripting;
using NekoPainter.Data;

namespace NekoPainter
{
    public class ViewRenderer
    {
        public ViewRenderer(LivedNekoPainterDocument livedDocument)
        {
            this.livedDocument = livedDocument;
        }

        public void RenderAll()
        {
            if (ManagedLayout.Count == 0) return;
            if (_paintingTexture == null)
            {
                _paintingTexture = new Texture2D { _texture = PaintingTexture, width = PaintingTexture.width, height = PaintingTexture.height };
            }

            PrepareRenderData();
            Output.Clear();
            int ofs = 0;
            var buffer = constantBuffer1.GetBuffer(DeviceResources);
            for (int i = ManagedLayout.Count - 1; i >= 0; i--)
            {
                PictureLayout selectedLayout = ManagedLayout[i];
                if (ManagedLayout[i].Hidden)
                {
                    ofs += 256;
                    continue;
                }

                livedDocument.LayoutTex.TryGetValue(selectedLayout.guid, out var tiledTexture);
                if (livedDocument.blendmodesMap.TryGetValue(selectedLayout.BlendMode, out var blendMode))
                {
                    if (selectedLayout.DataSource == LayoutDataSource.Color)
                    {
                        blendMode?.BlendPure(Output, buffer, ofs, 256);
                    }
                    else if (livedDocument.PaintAgent.CurrentLayout == selectedLayout || selectedLayout.generatePicture)
                    {
                        List<int> executeOrder;
                        PaintingTexture.Clear();
                        if (selectedLayout.graph != null)
                        {
                            var graph = selectedLayout.graph;
                            if (graph.Nodes.Count > 0)
                            {
                                SetAnimateNodeCacheInvalid(graph);
                                executeOrder = graph.GetUpdateList(graph.outputNode);
                                ExecuteNodes(graph, executeOrder);
                            }
                            if (graph.NodeParamCaches != null && graph.NodeParamCaches.TryGetValue(graph.outputNode, out var cache))
                            {
                                foreach (var cache1 in cache.outputCache)
                                    if (cache1.Value is TiledTexture t1)
                                        t1.UnzipToTexture(PaintingTexture);
                            }
                        }

                        if (selectedLayout.generatePicture.SetFalse())
                        {
                            if (livedDocument.LayoutTex.TryGetValue(selectedLayout.guid, out var tiledTexture1)) tiledTexture1.Dispose();
                            var tiledTexture2 = new TiledTexture(PaintingTexture);
                            livedDocument.LayoutTex[selectedLayout.guid] = tiledTexture2;
                        }

                        blendMode?.Blend(PaintingTexture, Output, buffer, ofs, 256);
                    }
                    else if (tiledTexture != null && tiledTexture.tilesCount != 0)
                    {
                        blendMode?.Blend(tiledTexture, Output, buffer, ofs, 256);
                    }
                }
                ofs += 256;
            }
        }

        void PrepareRenderData()
        {
            var writer = constantBuffer1.Begin();
            int ofs = 0;
            for (int i = ManagedLayout.Count - 1; i >= 0; i--)
            {
                writer.Seek(ofs, SeekOrigin.Begin);
                Vector4 color = ManagedLayout[i].Color;
                writer.Write(color);

                GetData(ManagedLayout[i], writer);
                ofs += 256;
            }
        }

        public void GetData(PictureLayout layout, BinaryWriterPlus writer)
        {
            if (livedDocument.blendmodesMap.TryGetValue(layout.BlendMode, out var blendMode) && blendMode.Paramerters != null)
            {
                for (int i = 0; i < blendMode.Paramerters.Length; i++)
                {
                    if (layout.parameters.TryGetValue(blendMode.Paramerters[i].Name, out var value))
                    {
                        writer.Write((float)value.X);
                    }
                    else
                    {
                        writer.Write(0.0f);
                    }
                }
            }
        }

        RenderTexture Output { get { return livedDocument.Output; } }
        RenderTexture PaintingTexture { get { return livedDocument.PaintingTexture; } }
        Texture2D _paintingTexture;
        DeviceResources DeviceResources { get { return livedDocument.DeviceResources; } }
        IReadOnlyList<PictureLayout> ManagedLayout { get { return livedDocument.Layouts; } }

        public readonly LivedNekoPainterDocument livedDocument;

        StreamedBuffer constantBuffer1 = new StreamedBuffer();

        public void ExecuteNodes(Graph graph, List<int> executeOrder)
        {
            GC(graph);
            foreach (int nodeId in executeOrder)
            {
                var node = graph.Nodes[nodeId];
                if (node.strokeNode != null)
                {
                    var cache = (graph.NodeParamCaches ??= new Dictionary<int, NodeParamCache>()).GetOrCreate(node.Luid);
                    cache.outputCache["strokes"] = new Stroke[] { node.strokeNode.stroke };
                    cache.valid = true;
                }
                else if (node.fileNode != null)
                {
                    var cache = (graph.NodeParamCaches ??= new Dictionary<int, NodeParamCache>()).GetOrCreate(node.Luid);
                    if (!cache.outputCache.TryGetValue(node.fileNode.path, out object path1))
                    {
                        cache.outputCache["filePath"] = node.fileNode.path;
                        cache.outputCache["bytes"] = File.ReadAllBytes(node.fileNode.path);
                    }
                    cache.valid = true;
                }
                else if (node.scriptNode != null)
                {

                    var nodeDef = livedDocument.scriptNodeDefs[node.GetNodeTypeName()];

                    ScriptGlobal global = new ScriptGlobal { parameters = new Dictionary<string, object>() };

                    var cache = (graph.NodeParamCaches ??= new Dictionary<int, NodeParamCache>()).GetOrCreate(nodeId);
                    cache.valid = true;

                    //获取输入
                    if (node.Inputs != null)
                        foreach (var input in node.Inputs)
                        {
                            var inputNode = graph.Nodes[input.Value.targetUid];
                            var inputNodeCache = graph.NodeParamCaches.GetOrCreate(inputNode.Luid);
                            if (inputNodeCache.outputCache.TryGetValue(input.Value.targetSocket, out object obj1))
                            {
                                if (obj1 is TiledTexture tex1)
                                {
                                    PaintingTexture.Clear();
                                    tex1.UnzipToTexture(PaintingTexture);
                                    global.parameters[input.Key] = _paintingTexture;
                                }
                                else
                                {
                                    global.parameters[input.Key] = obj1;
                                }
                            }
                        }
                    //检查null输入
                    foreach (var ioDef in nodeDef.ioDefs)
                    {
                        if (ioDef.type == "texture2D" && ioDef.ioType == "input")
                        {
                            if (!global.parameters.ContainsKey(ioDef.name))
                            {
                                PaintingTexture.Clear();
                                global.parameters[ioDef.name] = _paintingTexture;

                            }
                            else
                            {

                            }
                        }
                    }
                    //检查参数
                    if (nodeDef.parameters != null)
                    {
                        foreach (var param in nodeDef.parameters)
                        {
                            if (param.type == "float")
                            {
                                global.parameters[param.name] = (node.fParams ??= new Dictionary<string, float>()).GetOrDefault(param.name, (float)(param.defaultValue1));
                            }
                            if (param.type == "float2")
                            {
                                global.parameters[param.name] = (node.f2Params ??= new Dictionary<string, Vector2>()).GetOrDefault(param.name, (Vector2)(param.defaultValue1));
                            }
                            if (param.type == "float3" || param.type == "color3")
                            {
                                global.parameters[param.name] = (node.f3Params ??= new Dictionary<string, Vector3>()).GetOrDefault(param.name, (Vector3)(param.defaultValue1));
                            }
                            if (param.type == "float4" || param.type == "color4")
                            {
                                global.parameters[param.name] = (node.f4Params ??= new Dictionary<string, Vector4>()).GetOrDefault(param.name, (Vector4)(param.defaultValue1));
                            }
                        }
                    }
                    //编译、运行脚本
                    {
                        string path = nodeDef.path;
                        var script = livedDocument.scriptCache.GetOrCreate(path, () =>
                        {
                            ScriptOptions options = ScriptOptions.Default
                            .WithReferences(typeof(Texture2D).Assembly, typeof(SixLabors.ImageSharp.Image).Assembly, typeof(SixLabors.ImageSharp.Drawing.Path).Assembly)
                            .WithImports("NekoPainter.Data");

                            return CSharpScript.Create(livedDocument.scripts[path], options, typeof(ScriptGlobal));
                        });
                        var state = script.RunAsync(global).Result;
                    }
                    //缓存输出
                    foreach (var ioDef in nodeDef.ioDefs)
                    {
                        if (ioDef.type == "texture2D" && ioDef.ioType == "output" && global.parameters.ContainsKey(ioDef.name))
                        {
                            Texture2D tex = (Texture2D)global.parameters[ioDef.name];
                            if (cache.outputCache.TryGetValue(ioDef.name, out var _tex1))
                            {
                                ((TiledTexture)_tex1).Dispose();
                            }
                            cache.outputCache[ioDef.name] = new TiledTexture(tex._texture);
                        }
                    }
                }
            }
        }

        public void SetAnimateNodeCacheInvalid(Graph graph)
        {
            foreach (var nodePair in graph.Nodes)
            {
                var node = nodePair.Value;
                var nodeDef = livedDocument.scriptNodeDefs[node.GetNodeTypeName()];
                if (nodeDef.animated)
                {
                    graph.SetNodeInvalid(nodePair.Key);
                }
            }
        }

        HashSet<int> gcRemoveNode = new HashSet<int>();
        public void GC(Graph graph)
        {
            if (graph.NodeParamCaches == null) return;
            gcRemoveNode.Clear();
            foreach (var cache in graph.NodeParamCaches)
            {
                if (!graph.Nodes.ContainsKey(cache.Key))
                {
                    gcRemoveNode.Add(cache.Key);
                    foreach (var cache1 in cache.Value.outputCache)
                    {
                        if (cache1.Value is TiledTexture t1)
                        {
                            t1.Dispose();
                        }
                    }
                }
            }
            foreach (var key in gcRemoveNode)
                graph.NodeParamCaches.Remove(key);
        }
    }

    public class ScriptGlobal
    {
        public Dictionary<string, object> parameters;
        public NodeContext context;
    }
}

