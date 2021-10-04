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
    }
    public struct Particle
    {
        public Vector2 position;
        public Vector2 speed;
        public float life;
        public float lifeRemain;
    }
    public static void Invoke(Dictionary<string, object> parameters)
    {
        parameters.TryGetValue("texture2D", out object ptexture2D);
        parameters.TryGetValue("strokes", out object pstrokes);

        IList<Stroke> strokes = (IList<Stroke>)pstrokes;
        HashSet<Int2> covered = new HashSet<Int2>();

        Random random = new Random();
        DateTime dateTime = DateTime.Now;

        Texture2D tex = (Texture2D)ptexture2D;
        Vector4 color = (Vector4)parameters["color"];
        float size = Math.Max((float)parameters["size"], 1);
        float particleSize = Math.Max((float)parameters["particleSize"], 1);
        float speed = Math.Max((float)parameters["speed"], 1);
        float spacing = Math.Max((float)parameters["spacing"], 0.01f);

        ParticleCaches caches;
        if (parameters.TryGetValue("particleCache", out object cache1) && cache1 != null)
        {
            caches = (ParticleCaches)cache1;
        }
        else
        {
            caches = new ParticleCaches();
            caches.previous = dateTime;
            parameters["particleCache"] = caches;
        }
        var deltaTime = (float)((dateTime - caches.previous).TotalSeconds);
        caches.previous = dateTime;

        int width = tex.width;
        int height = tex.height;
        List<Vector2> positions = new List<Vector2>();
        int sizeX1 = ((int)size + 15) & ~15;
        if (strokes != null)
        {
            var rawTex1 = tex.GetRawTexture1();
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
                            int x1 = ((int)prevPoint.X - (int)(size / 2)) & ~15;
                            int y1 = ((int)prevPoint.Y - (int)(size / 2)) & ~15;
                            int x2 = x1 + sizeX1 + 1;
                            int y2 = y1 + sizeX1 + 1;
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
            while (particles.Count < covered2.Length * 2 && covered2.Length > 0)
            {
                int n1 = random.Next(0, covered2.Length);
                var pos1 = covered2[n1];
                Vector2 pos2 = new Vector2(pos1.X, pos1.Y);
                particles.Add(new Particle { speed = new Vector2((float)random.NextDouble() - 0.5f, (float)random.NextDouble() - 0.5f) * speed, position = new Vector2((float)random.NextDouble(), (float)random.NextDouble()) * 16 + pos2, lifeRemain = 4.0f + (float)random.NextDouble() * 2 });

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
                if (particles[i].lifeRemain < 0)
                {
                    particles[i] = particles[particles.Count - 1];
                    particles.RemoveAt(particles.Count - 1);
                }
            }


            foreach (var point1 in particles)
            {
                var point = point1.position;
                int x1 = (int)(point.X - particleSize / 2);
                int y1 = (int)(point.Y - particleSize / 2);
                int x2 = (int)(point.X + particleSize / 2) + 1;
                int y2 = (int)(point.Y + particleSize / 2) + 1;
                var rawTex = MemoryMarshal.Cast<byte, Vector4>(rawTex1);
                for (int y = y1; y <= y2; y++)
                    for (int x = x1; x <= x2; x++)
                        if (x >= 0 && x < width && y >= 0 && y < height)
                        {
                            int i = x + y * width;
                            if (Vector2.Distance(point, new Vector2(x, y)) < particleSize / 2)
                            {
                                float pa = rawTex[i].W;
                                rawTex[i] = rawTex[i] * (1 - color.W) + new Vector4(color.X, color.Y, color.Z, 0.0f) * color.W;
                                rawTex[i] = new Vector4(rawTex[i].X, rawTex[i].Y, rawTex[i].Z, 1 - (1 - pa) * (1 - color.W));
                            }
                        }
            }

            tex.EndModification();
        }
    }
}
Modified.Invoke(parameters);

