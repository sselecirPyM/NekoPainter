using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;
using NekoPainter.Core;
using NekoPainter.Util;
using NekoPainter.Nodes;
using CanvasRendering;
using System.Numerics;
using System.IO;
using System;
using Microsoft.CodeAnalysis.CSharp.Scripting;
using Microsoft.CodeAnalysis.Scripting;
using NekoPainter.Data;

namespace NekoPainter
{
    public class ViewRenderer : IDisposable
    {
        public ViewRenderer(LivedNekoPainterDocument doc)
        {
            this.livedDocument = doc;
            gpuCompute.document = doc;
            nodeContext.gpuCompute = gpuCompute;
        }
        System.Diagnostics.Stopwatch stopwatch = System.Diagnostics.Stopwatch.StartNew();
        long prevTick = 0;
        float deltaTime = 0;

        public void RenderAll()
        {
            if (ManagedLayout.Count == 0) return;

            deltaTime = Math.Clamp((stopwatch.ElapsedTicks - prevTick) / 1e7f, 0, 1);
            prevTick = stopwatch.ElapsedTicks;

            Output.Clear();
            int ofs = 0;
            for (int i = ManagedLayout.Count - 1; i >= 0; i--)
            {
                PictureLayout selectedLayout = ManagedLayout[i];
                if (ManagedLayout[i].Hidden)
                {
                    ofs += 256;
                    continue;
                }

                livedDocument.LayoutTex.TryGetValue(selectedLayout.guid, out var tiledTexture);
                if (livedDocument.blendModesMap.TryGetValue(selectedLayout.BlendMode, out var blendMode1))
                {
                    //if (selectedLayout.DataSource == LayoutDataSource.Color)
                    //{
                    //    blendMode?.BlendPure(Output, buffer, ofs, 256);
                    //}
                    //else
                    if (livedDocument.PaintAgent.CurrentLayout == selectedLayout || selectedLayout.generateCache)
                    {
                        List<int> executeOrder;
                        TiledTexture finalOutput = null;
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
                                        //t1.UnzipToTexture(paintingTexture1);
                                        finalOutput = t1;
                            }
                        }
                        selectedLayout.generateCache = false;
                        //if (selectedLayout.generateCache.SetFalse())
                        //{
                        if (livedDocument.LayoutTex.TryGetValue(selectedLayout.guid, out var tiledTexture1)) tiledTexture1.Dispose();
                        var tiledTexture2 = finalOutput != null ? new TiledTexture(finalOutput) : null;
                        if (tiledTexture2 != null)
                            livedDocument.LayoutTex[selectedLayout.guid] = tiledTexture2;
                        else
                            livedDocument.LayoutTex.Remove(selectedLayout.guid);
                        //}

                        if (tiledTexture2 != null)
                        {
                            var texture1 = gpuCompute.GetTemporaryTexture();
                            tiledTexture2.UnzipToTexture(((Texture2D)texture1)._texture);
                            BlendMode(blendMode1, selectedLayout, texture1);
                            gpuCompute.RecycleTemplateTextures();
                        }
                    }
                    else if (tiledTexture != null && tiledTexture.tilesCount != 0)
                    {
                        var texture1 = gpuCompute.GetTemporaryTexture();
                        tiledTexture.UnzipToTexture(((Texture2D)texture1)._texture);
                        BlendMode(blendMode1, selectedLayout, texture1);
                        gpuCompute.RecycleTemplateTextures();
                        //blendMode?.Blend(tiledTexture, Output, buffer, ofs, 256);
                    }
                }
                ofs += 256;
            }
        }


        RenderTexture Output { get { return livedDocument.Output; } }

        DeviceResources DeviceResources { get { return livedDocument.DeviceResources; } }
        IReadOnlyList<PictureLayout> ManagedLayout { get { return livedDocument.Layouts; } }

        NodeContext nodeContext = new NodeContext();

        public readonly LivedNekoPainterDocument livedDocument;

        public void ExecuteNodes(Graph graph, List<int> executeOrder)
        {
            GC(graph);
            foreach (int nodeId in executeOrder)
            {
                var node = graph.Nodes[nodeId];
                if (node.strokeNode != null)
                {
                    var cache = graph.NodeParamCaches.GetOrCreate(node.Luid);
                    cache.outputCache["strokes"] = node.strokeNode.strokes;
                    cache.valid = true;
                }
                else if (node.fileNode != null)
                {
                    var cache = graph.NodeParamCaches.GetOrCreate(node.Luid);
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

                    ScriptGlobal global = new ScriptGlobal { parameters = new Dictionary<string, object>(), context = nodeContext };
                    nodeContext.deltaTime = deltaTime;
                    nodeContext.width = livedDocument.Width;
                    nodeContext.height = livedDocument.Height;
                    var cache = graph.NodeParamCaches.GetOrCreate(nodeId);
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
                                    var texx = (Texture2D)gpuCompute.GetTemporaryTexture();
                                    tex1.UnzipToTexture(texx._texture);
                                    global.parameters[input.Key] = texx;
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
                        var param = nodeDef.parameters.Find(u => u.name == ioDef.name);
                        if (param.type == "texture2D" && ioDef.ioType == "input")
                        {
                            if (!global.parameters.ContainsKey(ioDef.name))
                            {
                                global.parameters[ioDef.name] = gpuCompute.GetTemporaryTexture();
                            }
                            else
                            {

                            }
                        }
                        else if (ioDef.ioType == "cache")
                        {
                            global.parameters[ioDef.name] = cache.outputCache.GetOrDefault(ioDef.name, null);
                        }
                    }
                    //检查参数
                    if (nodeDef.parameters != null)
                    {
                        foreach (var param in nodeDef.parameters)
                        {
                            if (param.type == "float")
                            {
                                global.parameters[param.name] = node.fParams.GetOrDefault(param.name, (float)(param.defaultValue1));
                            }
                            if (param.type == "float2")
                            {
                                global.parameters[param.name] = node.f2Params.GetOrDefault(param.name, (Vector2)(param.defaultValue1));
                            }
                            if (param.type == "float3" || param.type == "color3")
                            {
                                global.parameters[param.name] = node.f3Params.GetOrDefault(param.name, (Vector3)(param.defaultValue1));
                            }
                            if (param.type == "float4" || param.type == "color4")
                            {
                                global.parameters[param.name] = node.f4Params.GetOrDefault(param.name, (Vector4)(param.defaultValue1));
                            }
                            if (param.type == "int")
                            {
                                global.parameters[param.name] = node.iParams.GetOrDefault(param.name, (int)(param.defaultValue1));
                            }
                            if (param.type == "bool")
                            {
                                global.parameters[param.name] = node.bParams.GetOrDefault(param.name, (bool)(param.defaultValue1));
                            }
                            if (param.type == "string")
                            {
                                global.parameters[param.name] = node.sParams.GetOrDefault(param.name, (string)(param.defaultValue1));
                            }
                        }
                    }
                    //编译、运行脚本
                    RunScript(nodeDef.path, global);

                    //缓存输出
                    foreach (var ioDef in nodeDef.ioDefs)
                    {
                        var param = nodeDef.parameters.Find(u => u.name == ioDef.name);
                        if (ioDef.ioType == "output")
                        {
                            if (param.type == "texture2D" && global.parameters.ContainsKey(ioDef.name))
                            {
                                Texture2D tex = (Texture2D)global.parameters[ioDef.name];
                                if (cache.outputCache.TryGetValue(ioDef.name, out var _tex1))
                                {
                                    ((TiledTexture)_tex1).Dispose();
                                }
                                cache.outputCache[ioDef.name] = new TiledTexture(tex._texture);
                            }
                            else
                            {
                                cache.outputCache[ioDef.name] = global.parameters[ioDef.name];
                            }
                        }
                        else if (ioDef.ioType == "cache")
                        {
                            cache.outputCache[ioDef.name] = global.parameters[ioDef.name];
                        }
                    }
                    gpuCompute.RecycleTemplateTextures();
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
                    graph.SetNodeCacheInvalid(nodePair.Key);
                }
            }
        }

        public void RunScript(string path, ScriptGlobal global)
        {
            var script = livedDocument.scriptCache.GetOrCreate(path, () =>
            {
                ScriptOptions options = ScriptOptions.Default
                .WithReferences(typeof(Texture2D).Assembly, typeof(SixLabors.ImageSharp.Image).Assembly, typeof(SixLabors.ImageSharp.Drawing.Path).Assembly)
                .WithImports("NekoPainter.Data").WithOptimizationLevel(Microsoft.CodeAnalysis.OptimizationLevel.Release);

                return CSharpScript.Create(livedDocument.scripts[path], options, typeof(ScriptGlobal));
            });
            var state = script.RunAsync(global).Result;
        }

        GPUCompute gpuCompute = new GPUCompute();

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

        public void BlendMode(BlendMode blendMode, PictureLayout layout, ITexture2D tex1)
        {
            ScriptGlobal global = new ScriptGlobal { parameters = new Dictionary<string, object>(), context = nodeContext };
            Texture2D tex0 = new Texture2D() { _texture = Output };
            global.parameters["tex0"] = tex0;
            global.parameters["tex1"] = tex1;

            var node = layout;
            var parameters = blendMode.parameters;
            if (parameters != null)
            {
                foreach (var param in parameters)
                {
                    if (param.type == "float")
                    {
                        global.parameters[param.name] = node.fParams.GetOrDefault(param.name, (float)(param.defaultValue1));
                    }
                    if (param.type == "float2")
                    {
                        global.parameters[param.name] = node.f2Params.GetOrDefault(param.name, (Vector2)(param.defaultValue1));
                    }
                    if (param.type == "float3" || param.type == "color3")
                    {
                        global.parameters[param.name] = node.f3Params.GetOrDefault(param.name, (Vector3)(param.defaultValue1));
                    }
                    if (param.type == "float4" || param.type == "color4")
                    {
                        global.parameters[param.name] = node.f4Params.GetOrDefault(param.name, (Vector4)(param.defaultValue1));
                    }
                    if (param.type == "int")
                    {
                        global.parameters[param.name] = node.iParams.GetOrDefault(param.name, (int)(param.defaultValue1));
                    }
                    if (param.type == "bool")
                    {
                        global.parameters[param.name] = node.bParams.GetOrDefault(param.name, (bool)(param.defaultValue1));
                    }
                    if (param.type == "string")
                    {
                        global.parameters[param.name] = node.sParams.GetOrDefault(param.name, (string)(param.defaultValue1));
                    }
                }
            }

            RunScript(blendMode.script, global);
        }

        public void Dispose()
        {
            gpuCompute.Dispose();
        }
    }

    public class ScriptGlobal
    {
        public Dictionary<string, object> parameters;
        public NodeContext context;
    }

    public class LayoutRenderConfig
    {
        public int frame;
        public float frameInterval;
    }
}

