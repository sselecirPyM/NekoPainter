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
    public class ParticleCaches
    {
        public List<Particle> particles = new List<Particle>();
        public Vector4[] cacheArray;
        public Dictionary<Int2, List<int>> cacheD1;
        public float t1;
    }
    public struct Particle
    {
        public Vector2 position;
        public Vector2 speed;
        public Vector4 color;
        public float life;
        public float lifeRemain;
    }
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
        parameters.TryGetValue("strokes", out object pstrokes);

        IList<Stroke> strokes = (IList<Stroke>)pstrokes;
        HashSet<Int2> covered = new HashSet<Int2>();

        Texture2D tex = (Texture2D)ptexture2D;
        //Texture2D tex1 = (Texture2D)psampleSource;
        int width = tex.width;
        int height = tex.height;

        Random random = new Random();
        DateTime dateTime = DateTime.Now;

        Vector4 color = (Vector4)parameters["color"];
        Vector4 color2 = (Vector4)parameters["color2"];
        float size = Math.Max((float)parameters["size"], 1);
        float particleSize = Math.Max((float)parameters["particleSize"], 1);
        float speed = Math.Max((float)parameters["speed"], 0);
        float spacing = Math.Max((float)parameters["spacing"], 0.01f);
        float threshold = Math.Max((float)parameters["threshold"], 0.01f);

        ParticleCaches caches;
        Dictionary<Int2, List<int>> divParticles;
        if (parameters.TryGetValue("particleCache", out object cache1) && cache1 != null)
        {
            caches = (ParticleCaches)cache1;
            divParticles = caches.cacheD1;
        }
        else
        {
            caches = new ParticleCaches();
            caches.cacheArray = new Vector4[width * height];
            parameters["particleCache"] = caches;
            caches.t1 = 1;
            divParticles = new Dictionary<Int2, List<int>>();
            for (int y = 0; y < height; y += 32)
                for (int x = 0; x < width; x += 32)
                {
                    divParticles[new Int2(x, y)] = new List<int>();
                }
            caches.cacheD1 = divParticles;
        }
        Vector4[] powerField = caches.cacheArray;
        Array.Clear(powerField, 0, powerField.Length);
        var deltaTime = context.deltaTime;
        caches.t1 += deltaTime;
        caches.t1 = Math.Min(caches.t1, 4);

        List<Vector2> positions = new List<Vector2>();
        int sizeX1 = ((int)size + 15) & ~15;
        if (strokes != null)
        {
            foreach (var stroke in strokes)
            {
                float pathLength = 0;
                float distanceRemain = 0.0f;
                Vector2 prevPoint = stroke.position[0];
                for (int i = 0; i < stroke.position.Count; i++)
                {
                    var point = stroke.position[i];
                    if (i > 0)
                    {
                        while (Vector2.Distance(prevPoint, point) > size * spacing)
                        {
                            prevPoint += Vector2.Normalize(point - prevPoint) * size * spacing;
                            int x1 = Math.Max(((int)prevPoint.X - (int)(size / 2)) & ~15, 0);
                            int y1 = Math.Max(((int)prevPoint.Y - (int)(size / 2)) & ~15, 0);
                            int x2 = Math.Min(x1 + sizeX1 + 1, width);
                            int y2 = Math.Min(y1 + sizeX1 + 1, height);
                            for (int y = y1; y < y2; y += 16)
                                for (int x = x1; x < x2; x += 16)
                                {
                                    covered.Add(new Int2(x, y));
                                }
                        }
                    }
                }
            }
            var covered2 = covered.ToArray();
            var particles = caches.particles;
            while (particles.Count < covered2.Length * 2 && caches.t1 > 0)
            {
                int n1 = random.Next(0, covered2.Length);
                var pos1 = covered2[n1];
                Vector2 pos2 = new Vector2(pos1.X, pos1.Y);
                Vector2 pos3 = GetRandomVector2(random) * 16 + pos2;
                Vector4 colorX = ((dateTime.Second & 1) > 0) ? color : color2;
                //int x = (int)pos3.X;
                //int y = (int)pos3.Y;
                //int i = x + y * width;
                //if (i >= 0 && i < rawTex.Length)
                //{
                //    colorX = rawTex[i];
                //}

                float life = 4.0f + (float)random.NextDouble() * 2;
                particles.Add(new Particle { speed = (GetRandomVector2(random) - new Vector2(0.5f, 0.5f)) * speed, position = pos3, life = life, lifeRemain = life, color = colorX });
                caches.t1 -= 5.0f / covered2.Length;
            }
            for (int i = 0; i < particles.Count; i++)
            {
                var particle = particles[i];
                particle.lifeRemain -= deltaTime;
                particle.position += particle.speed * deltaTime;
                particles[i] = particle;
            }
            for (int i = 0; i < particles.Count; i++)
            {
                if (particles[i].lifeRemain <= 0)
                {
                    particles[i] = particles[particles.Count - 1];
                    particles.RemoveAt(particles.Count - 1);
                }
            }
            for (int i1 = 0; i1 < particles.Count; i1++)
            {
                Particle point1 = particles[i1];
                var point = point1.position;
                float c1 = point1.lifeRemain / Math.Max(point1.life, 0.001f);
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
            var p3 = divParticles.Where(u => u.Value.Count > 0);
            Parallel.ForEach(p3, u =>
            {
                int x3 = u.Key.X;
                int y3 = u.Key.Y;
                int x4 = Math.Min(x3 + 32, width);
                int y4 = Math.Min(y3 + 32, height);

                for (int i1 = 0; i1 < u.Value.Count; i1++)
                {
                    Particle point1 = particles[u.Value[i1]];
                    var point = point1.position;

                    float c1 = point1.lifeRemain / Math.Max(point1.life, 0.001f);
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
                                    powerField[i] = WithW(point1.color * power1 + powerField[i], powerSum);
                                }
                                else
                                {
                                    powerField[i] = WithW(point1.color * 65536, 65536);
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
            gpuCompute.SetParameter("color", color);
            gpuCompute.SetParameter("threshold", threshold);
            gpuCompute.SetTexture("tex0", tex);
            gpuCompute.SetTexture("tex1", tex1);
            gpuCompute.SetComputeShader("metaballRender.json");
            gpuCompute.For(0, tex.width, 0, tex.height);
        }
    }
}
Modified.Invoke(parameters, context);

