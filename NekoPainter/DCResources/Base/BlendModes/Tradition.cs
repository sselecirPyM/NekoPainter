using System;
using System.Diagnostics;
using NekoPainter.Data;
using System.Numerics;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Linq;

static class Modified
{
    public static void Invoke(Dictionary<string, object> parameters, NodeContext context)
    {
        if (!parameters.TryGetValue("tex0", out object _tex0)) return;
        if (!parameters.TryGetValue("tex1", out object _tex1)) return;
        string blendMode = (string)parameters["blendMode"];
        ITexture2D tex0 = (ITexture2D)_tex0;
        ITexture2D tex1 = (ITexture2D)_tex1;
        int width = tex0.width;
        int height = tex0.height;
        var gpuCompute = context.gpuCompute;
        switch (blendMode)
        {
            case "Add":
                gpuCompute.SetComputeShader("TextureBlendAdd.json");
                break;
            case "Alpha":
            default:
                gpuCompute.SetComputeShader("TextureBlendAlpha.json");
                break;
        }
        gpuCompute.SetTexture("tex0", tex0);
        gpuCompute.SetTexture("tex1", tex1);
        gpuCompute.For(0, width, 0, height);
    }
}
Modified.Invoke(parameters, context);

