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
        public DateTime previous;
        public Vector4[] cacheArray;
        public float f1;
    }
    public struct Particle
    {
        public Vector2 position;
        public Vector2 speed;
        public Vector4 color;
        public float life;
        public float lifeRemain;
    }
    static Vector2 GetVector2(Random random)
    {
        return new Vector2((float)random.NextDouble(), (float)random.NextDouble());
    }
    static Vector4 GetVector4(Random random)
    {
        return new Vector4((float)random.NextDouble(), (float)random.NextDouble(), (float)random.NextDouble(), (float)random.NextDouble());
    }
    static Vector4 WithW(Vector4 ori, float w)
    {
        return new Vector4(ori.X, ori.Y, ori.Z, w);
    }
    public static void Invoke(Dictionary<string, object> parameters, NodeContext context)
    {
        parameters.TryGetValue("sampleSource", out object psampleSource);
        parameters.TryGetValue("texture2D", out object ptexture2D);
        parameters.TryGetValue("strokes", out object pstrokes);

        IList<Stroke> strokes = (IList<Stroke>)pstrokes;
        HashSet<Int2> covered = new HashSet<Int2>();

        Texture2D tex = (Texture2D)ptexture2D;
        Texture2D tex1 = (Texture2D)psampleSource;
        int width = tex.width;
        int height = tex.height;

        Random random = new Random();
        DateTime dateTime = DateTime.Now;

        Vector4 color = (Vector4)parameters["color"];
        float size = Math.Max((float)parameters["size"], 1);
        float particleSize = Math.Max((float)parameters["particleSize"], 1);
        float speed = Math.Max((float)parameters["speed"], 1);
        float spacing = Math.Max((float)parameters["spacing"], 0.01f);
        float threshold = Math.Max((float)parameters["threshold"], 0.01f);

        ParticleCaches caches;
        if (parameters.TryGetValue("particleCache", out object cache1) && cache1 != null)
        {
            caches = (ParticleCaches)cache1;
        }
        else
        {
            caches = new ParticleCaches();
            caches.previous = dateTime;
            caches.cacheArray = new Vector4[width * height];
            parameters["particleCache"] = caches;
            caches.f1 = 1;
        }
        Vector4[] powerField = caches.cacheArray;
        Array.Clear(powerField, 0, powerField.Length);
        var deltaTime = (float)((dateTime - caches.previous).TotalSeconds);
        caches.previous = dateTime;
        caches.f1 += deltaTime;
        caches.f1 = Math.Min(caches.f1, 4);

        List<Vector2> positions = new List<Vector2>();
        int sizeX1 = ((int)size + 15) & ~15;
        if (strokes != null)
        {
            //foreach (var stroke in strokes)
            //{
            //    float pathLength = 0;
            //    float distanceRemain = 0.0f;
            //    Vector2 prevPoint = stroke.position[0];
            //    positions.Add(prevPoint);
            //    for (int i = 0; i < stroke.position.Count; i++)
            //    {
            //        var point = stroke.position[i];
            //        if (i > 0)
            //        {
            //            while (Vector2.Distance(prevPoint, point) > size * spacing)
            //            {
            //                prevPoint += Vector2.Normalize(point - prevPoint) * size * spacing;
            //                positions.Add(prevPoint);
            //            }
            //        }
            //    }
            //}
            //foreach (var point in positions)
            //{
            //    int x1 = (int)(point.X - size / 2);
            //    int y1 = (int)(point.Y - size / 2);
            //    int x2 = (int)(point.X + size / 2);
            //    int y2 = (int)(point.Y + size / 2);
            //    var rawTex = MemoryMarshal.Cast<byte, Vector4>(rawTex1);
            //    for (int y = y1; y < y2; y++)
            //        for (int x = x1; x < x2; x++)
            //            if (x >= 0 && x < width && y >= 0 && y < height)
            //            {
            //                int i = x + y * width;
            //                if (Vector2.Distance(point, new Vector2(x, y)) < size / 2)
            //                {
            //                    float pa = rawTex[i].W;
            //                    rawTex[i] = rawTex[i] * (1 - color.W) + new Vector4(color.X, color.Y, color.Z, 0.0f) * color.W;
            //                    rawTex[i] = new Vector4(rawTex[i].X, rawTex[i].Y, rawTex[i].Z, 1 - (1 - pa) * (1 - color.W));
            //                }
            //            }
            //}
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
                            int x2 = Math.Min(x1 + sizeX1 + 1, width + 1);
                            int y2 = Math.Min(y1 + sizeX1 + 1, height + 1);
                            for (int y = y1; y < y2; y += 16)
                                for (int x = x1; x < x2; x += 16)
                                {
                                    if (x >= 0 && y >= 0)
                                        covered.Add(new Int2(x, y));
                                }
                        }
                    }
                }
            }
            var covered2 = covered.ToArray();
            var particles = caches.particles;
            while (particles.Count < covered2.Length * 2 && caches.f1 > 0)
            {
                int n1 = random.Next(0, covered2.Length);
                var pos1 = covered2[n1];
                Vector2 pos2 = new Vector2(pos1.X, pos1.Y);
                Vector2 pos3 = GetVector2(random) * 16 + pos2;
                Vector4 colorX = color;
                int x = (int)pos3.X;
                int y = (int)pos3.Y;
                int i = x + y * width;
                //if (i >= 0 && i < rawTex.Length)
                //{
                //    colorX = rawTex[i];
                //}

                float life = 4.0f + (float)random.NextDouble() * 2;
                particles.Add(new Particle { speed = (GetVector2(random) - new Vector2(0.5f, 0.5f)) * speed, position = pos3, life = life, lifeRemain = life, color = colorX });
                caches.f1 -= 5.0f / covered2.Length;
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

            foreach (var point1 in particles)
            {
                var point = point1.position;
                int x1 = (int)(point.X - particleSize);
                int y1 = (int)(point.Y - particleSize);
                int x2 = (int)(point.X + particleSize);
                int y2 = (int)(point.Y + particleSize);
                float c1 = point1.lifeRemain / Math.Max(point1.life, 0.001f);
                for (int y = y1; y <= y2; y++)
                    for (int x = x1; x <= x2; x++)
                        if (x >= 0 && x < width && y >= 0 && y < height)
                        {
                            int i = x + y * width;
                            if (Vector2.Distance(point, new Vector2(x, y)) < particleSize)
                            {
                                float ds = Vector2.DistanceSquared(point, new Vector2(x, y));
                                if (ds > 1)
                                {
                                    float power0 = powerField[i].W;
                                    float power1 = Math.Max((65536 / ds * c1), 0);
                                    float powerSum = power0 + power1;
                                    powerField[i] = WithW(point1.color * (power1 / powerSum) + powerField[i] * (power0 / powerSum), powerSum);
                                }
                                else
                                {
                                    powerField[i] = WithW(point1.color, 65535);
                                }
                            }
                        }
            }
            tex1.UpdateTexture<Vector4>(powerField);
        }
        var gpuCompute = context.gpuCompute;
        gpuCompute.SetParameter("color", color);
        gpuCompute.SetParameter("threshold", threshold);
        gpuCompute.SetTexture("tex0", tex);
        gpuCompute.SetTexture("tex1", tex1);
        gpuCompute.SetComputeShader("metaballRender.json");
        gpuCompute.For(0, tex.width, 0, tex.height);
    }
}
Modified.Invoke(parameters, context);

