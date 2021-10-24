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
    static Vector2 GetRandomVector2(Random random)
    {
        return new Vector2((float)random.NextDouble(), (float)random.NextDouble());
    }
    static Vector4 GetRandomVector4(Random random)
    {
        return new Vector4((float)random.NextDouble(), (float)random.NextDouble(), (float)random.NextDouble(), (float)random.NextDouble());
    }
    static Vector4 WithW(Vector4 ori, float w)
    {
        return new Vector4(ori.X, ori.Y, ori.Z, w);
    }
    public static void Invoke(Dictionary<string, object> parameters, NodeContext context)
    {
        //parameters.TryGetValue("sampleSource", out object psampleSource);
        parameters.TryGetValue("texture2D", out object ptexture2D);
        parameters.TryGetValue("particles", out object pparticles);

        NPParticles particles = (NPParticles)pparticles;
        HashSet<Int2> covered = new HashSet<Int2>();

        ITexture2D tex = (ITexture2D)ptexture2D;
        //Texture2D tex1 = (Texture2D)psampleSource;
        int width = tex.width;
        int height = tex.height;

        Random random = new Random();
        DateTime dateTime = DateTime.Now;

        float particleSize = Math.Max((float)parameters["particleSize"], 1);
        float threshold = Math.Max((float)parameters["threshold"], 0.01f);

        Dictionary<Int2, List<int>> divParticles;
        divParticles = new Dictionary<Int2, List<int>>();
        for (int y = 0; y < height; y += 32)
            for (int x = 0; x < width; x += 32)
            {
                divParticles[new Int2(x, y)] = new List<int>();
            }

        Vector4[] powerField = new Vector4[width * height];
        Array.Clear(powerField, 0, powerField.Length);

        if (particles != null)
        {
            for (int i1 = 0; i1 < particles.position.Count; i1++)
            {
                var point = particles.position[i1];
                float c1 = particles.lifeRemain[i1] / Math.Max(particles.life[i1], 0.001f);
                float size1 = particleSize * c1;
                int x1 = Math.Max((int)(point.X - size1), 0);
                int y1 = Math.Max((int)(point.Y - size1), 0);
                int x2 = Math.Min((int)(point.X + size1) + 1, width);
                int y2 = Math.Min((int)(point.Y + size1) + 1, height);
                for (int y = y1 & ~31; y < y2; y += 32)
                    for (int x = x1 & ~31; x < x2; x += 32)
                    {
                        divParticles[new Int2(x, y)].Add(i1);
                    }
            }
            var p3 = divParticles.Where(u => u.Value.Count > 0).ToArray();
            Parallel.ForEach(p3, u =>
            {
                int x3 = u.Key.X;
                int y3 = u.Key.Y;
                int x4 = Math.Min(x3 + 32, width);
                int y4 = Math.Min(y3 + 32, height);

                for (int i1 = 0; i1 < u.Value.Count; i1++)
                {
                    int index1 = u.Value[i1];
                    var px1 = particles.position[index1];
                    var point = new Vector2(px1.X, px1.Y);

                    float c1 = particles.lifeRemain[index1] / Math.Max(particles.life[index1], 0.001f);
                    float size1 = particleSize * c1;
                    int x1 = Math.Max((int)(point.X - size1), x3);
                    int y1 = Math.Max((int)(point.Y - size1), y3);
                    int x2 = Math.Min((int)(point.X + size1) + 1, x4);
                    int y2 = Math.Min((int)(point.Y + size1) + 1, y4);
                    for (int y = y1; y < y2; y++)
                        for (int x = x1; x < x2; x++)
                        {
                            int i = x + y * width;
                            if (Vector2.Distance(point, new Vector2(x, y)) < size1)
                            {
                                float ds = Vector2.DistanceSquared(point, new Vector2(x, y));
                                if (ds > 1)
                                {
                                    float power0 = powerField[i].W;
                                    float power1 = Math.Max((65536 / ds * c1), 0);
                                    float powerSum = power0 + power1;
                                    powerField[i] = WithW(particles.color[index1] * power1 + powerField[i], powerSum);
                                }
                                else
                                {
                                    powerField[i] = WithW(particles.color[index1] * 65536 + powerField[i], 65536 + powerField[i].W);
                                }
                            }
                        }
                }
            });
            foreach (var pair in divParticles)
            {
                pair.Value.Clear();
            }
            var gpuCompute = context.gpuCompute;
            var tex1 = gpuCompute.GetTemporaryTexture();
            tex1.UpdateTexture<Vector4>(powerField);
            //gpuCompute.SetParameter("color", color);
            gpuCompute.SetParameter("threshold", threshold);
            gpuCompute.SetTexture("tex0", tex);
            gpuCompute.SetTexture("tex1", tex1);
            gpuCompute.SetComputeShader("metaballRender.json");
            gpuCompute.For(0, tex.width, 0, tex.height);
        }
    }
}
Modified.Invoke(parameters, context);

