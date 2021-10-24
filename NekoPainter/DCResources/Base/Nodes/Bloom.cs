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
    static float Gaussian(float x, float mu, float sigma)
    {
        float a = (x - mu) / sigma;
        return MathF.Exp(-0.5f * a * a);
    }
    public static void Invoke(Dictionary<string, object> parameters, NodeContext context)
    {
        parameters.TryGetValue("texture2D", out object ptexture2D);

        HashSet<Int2> covered = new HashSet<Int2>();

        int radius = Math.Clamp((int)parameters["radius"], 1, 500);
        float threshold = (float)parameters["threshold"];
        float intensity = (float)parameters["intensity"];
        ITexture2D tex = (ITexture2D)ptexture2D;
        int width = tex.width;
        int height = tex.height;

        float sigma = radius / 2.0f;
        int size1 = 2 * radius + 1;

        float[] weights = new float[size1];

        float sum = 0;
        for (int i = 0; i < size1; i++)
        {
            weights[i] = Gaussian(i, radius, sigma);
            sum += weights[i];
        }
        for (int i = 0; i < size1; i++)
        {
            weights[i] /= sum;
        }

        Random random = new Random();
        var gpuCompute = context.gpuCompute;
        var tex1 = gpuCompute.GetTemporaryTexture();
        gpuCompute.SetBuffer("weights", weights);
        gpuCompute.SetParameter("radius", radius);
        gpuCompute.SetParameter("threshold", threshold);
        gpuCompute.SetParameter("intensity", intensity);
        gpuCompute.SetTexture("tex0", tex1);
        gpuCompute.SetTexture("tex1", tex);
        gpuCompute.SetComputeShader("BloomH.json");
        gpuCompute.For(0, tex.width, 0, tex.height);
        gpuCompute.SetComputeShader("BloomV.json");
        gpuCompute.SetTexture("tex0", tex);
        gpuCompute.SetTexture("tex1", tex1);
        gpuCompute.For(0, tex.width, 0, tex.height);
    }
}
Modified.Invoke(parameters, context);

