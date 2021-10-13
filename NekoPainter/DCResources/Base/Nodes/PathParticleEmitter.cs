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
        public NPParticles particles;
        public float t1;
    }
    static bool BoxCircleCollision(Vector2 circleMid, float radius, float x, float y, float width, float height)
    {
        float nX = Math.Clamp(circleMid.X, x, x + width);
        float nY = Math.Clamp(circleMid.Y, y, y + height);
        if (Vector2.DistanceSquared(circleMid, new Vector2(nX, nY)) < radius * radius) return true;
        return false;
    }
    static float Min(params float[] f1)
    {
        float a = f1[0];
        for (int i = 1; i < f1.Length; i++)
        {
            a = Math.Min(f1[i], a);
        }
        return a;
    }
    static float Max(params float[] f1)
    {
        float a = f1[0];
        for (int i = 1; i < f1.Length; i++)
        {
            a = Math.Max(f1[i], a);
        }
        return a;
    }
    static Vector2 GetRandomVector2(Random random)
    {
        return new Vector2((float)random.NextDouble(), (float)random.NextDouble());
    }
    static Vector3 GetRandomVector3(Random random)
    {
        return new Vector3((float)random.NextDouble(), (float)random.NextDouble(), (float)random.NextDouble());
    }
    static Vector2 GetProjectionMinMax(Vector2 p1, Vector2 p2, Vector2 p3, Vector2 p4, Vector2 ax1)
    {
        float rx1 = Min(Vector2.Dot(p1, ax1), Vector2.Dot(p2, ax1), Vector2.Dot(p3, ax1), Vector2.Dot(p4, ax1));
        float rx2 = Max(Vector2.Dot(p1, ax1), Vector2.Dot(p2, ax1), Vector2.Dot(p3, ax1), Vector2.Dot(p4, ax1));
        return new Vector2(rx1, rx2);
    }
    static float Lerp(float x, float y, float v)
    {
        return x * (1 - v) + y * v;
    }
    static Vector3 Lerp(Vector3 x, Vector3 y, float v)
    {
        return x * (1 - v) + y * v;
    }
    static bool BoxBox1Collision(Vector2 position1, Vector2 position2, float width0, float x, float y, float width, float height)
    {
        Vector2 dir1 = position2 - position1;
        Vector2 dir2 = Vector2.Normalize(new Vector2(-dir1.Y, dir1.X)) * width0 / 2;
        Vector2 p1 = position1 + dir2;
        Vector2 p2 = position1 - dir2;
        Vector2 p3 = position2 + dir2;
        Vector2 p4 = position2 - dir2;
        Vector2 ax1 = new Vector2(1, 0);
        Vector2 ax2 = new Vector2(0, 1);
        Vector2 ax3 = Vector2.Normalize(dir1);
        Vector2 ax4 = Vector2.Normalize(dir2);
        Vector2 rm1 = GetProjectionMinMax(p1, p2, p3, p4, ax1);
        Vector2 rm2 = GetProjectionMinMax(p1, p2, p3, p4, ax2);
        Vector2 rm3 = GetProjectionMinMax(new Vector2(x, y), new Vector2(x + width, y), new Vector2(x, y + height), new Vector2(x + width, y + height), ax1);
        Vector2 rm4 = GetProjectionMinMax(new Vector2(x, y), new Vector2(x + width, y), new Vector2(x, y + height), new Vector2(x + width, y + height), ax2);


        Vector2 rm5 = GetProjectionMinMax(p1, p2, p3, p4, ax3);
        Vector2 rm6 = GetProjectionMinMax(p1, p2, p3, p4, ax4);
        Vector2 rm7 = GetProjectionMinMax(new Vector2(x, y), new Vector2(x + width, y), new Vector2(x, y + height), new Vector2(x + width, y + height), ax3);
        Vector2 rm8 = GetProjectionMinMax(new Vector2(x, y), new Vector2(x + width, y), new Vector2(x, y + height), new Vector2(x + width, y + height), ax4);

        if (rm1.X > rm3.Y || rm1.Y < rm3.X || rm2.X > rm4.Y || rm2.Y < rm4.X || rm5.X > rm7.Y || rm5.Y < rm7.X || rm6.X > rm8.Y || rm6.Y < rm8.X)
            return false;
        return true;
    }
    public static void Invoke(Dictionary<string, object> parameters, NodeContext context)
    {
        Random random = new Random();
        parameters.TryGetValue("strokes", out object pstrokes);

        IList<Stroke> strokes = (IList<Stroke>)pstrokes;
        float strokeSize = Math.Max((float)parameters["strokeSize"], 0.001f);
        float randomSpeed = Math.Max((float)parameters["randomSpeed"], 0);
        float generateSpeed = Math.Max((float)parameters["generateSpeed"], 0);
        Vector3 originSpeed = (Vector3)parameters["originSpeed"];
        Vector4 color = (Vector4)parameters["color"];
        Vector4 color2 = (Vector4)parameters["color2"];
        Vector2 lifeMinMax = (Vector2)parameters["life"];
        int maxCount = (int)parameters["maxCount"];
        int width = context.width;
        int height = context.height;
        NPParticles particles;
        if (parameters.TryGetValue("particleCache", out object particles1) && particles1 is ParticleCaches caches)
        {
            particles = caches.particles;
        }
        else
        {
            caches = new ParticleCaches();
            particles = new NPParticles();
            particles.position = new List<Vector3>();
            caches.particles = particles;
        }
        caches.t1 += context.deltaTime;
        if (strokes != null)
        {
            List<Vector3> points = new List<Vector3>();
            List<float> findValue = new List<float>();
            float totalLength = 0;
            foreach (var stroke in strokes)
            {
                for (int i = 0; i < stroke.position.Count; i++)
                {
                    if (i > 0)
                    {
                        var point = stroke.position[i];
                        var point2 = stroke.position[i - 1];
                        if (Vector2.Distance(point, point2) < 0.5f) continue;
                        totalLength += Vector2.Distance(point, point2);
                    }
                }
                float tl1 = 0;
                for (int i = 0; i < stroke.position.Count; i++)
                {
                    var point = stroke.position[i];
                    if (i > 0)
                    {
                        var point2 = stroke.position[i - 1];
                        if (Vector2.Distance(point, point2) < 0.5f) continue;
                        tl1 += Vector2.Distance(point, point2);
                    }
                    points.Add(new Vector3(point, 0));
                    findValue.Add(tl1 / totalLength);
                }

                particles.speed ??= new List<Vector3>();
                particles.life ??= new List<float>();
                particles.lifeRemain ??= new List<float>();
                particles.color ??= new List<Vector4>();

                while (particles.position.Count < maxCount && caches.t1 > 0)
                {
                    var pos1 = stroke.position[0];
                    Vector3 pos3 = GetRandomVector3(random) * 16 + new Vector3(pos1.X, pos1.Y, 0);
                    Vector4 colorX = color;

                    float life = lifeMinMax.X + (float)random.NextDouble() * (lifeMinMax.Y - lifeMinMax.X);
                    particles.position.Add(pos3);
                    particles.speed.Add((GetRandomVector3(random) - new Vector3(0.5f, 0.5f, 0.5f)) * randomSpeed + originSpeed);
                    particles.life.Add(life);
                    particles.lifeRemain.Add(life);
                    particles.color.Add(colorX);
                    caches.t1 -= 1.0f / Math.Max(generateSpeed, 0.05f);
                }
            }
            for (int i = 0; i < particles.position.Count; i++)
            {
                //particles.position[i] += particles.speed[i] * context.deltaTime;
                float inter = particles.lifeRemain[i] / particles.life[i];
                int indexY = findValue.BinarySearch(1 - inter);
                if (indexY < 0) indexY = ~indexY;
                indexY = Math.Min(indexY, findValue.Count - 1);
                int indexX = Math.Clamp(indexY - 1, 0, findValue.Count - 1);
                particles.position[i] = Lerp(points[indexX], points[indexY], (1 - inter - findValue[indexX]) / Math.Max(findValue[indexY] - findValue[indexX], 0.0005f)) + particles.speed[i];
                //particles.position[i] = points[indexY];


                particles.lifeRemain[i] -= context.deltaTime;
                particles.color[i] = color * inter + color2 * (1 - inter);
            }
            for (int i = 0; i < particles.position.Count; i++)
            {
                if (particles.lifeRemain[i] <= 0)
                {
                    particles.RemoveParticleX(i);
                    i--;
                }
            }
            parameters["particles"] = particles;
            parameters["particleCache"] = caches;
        }
        else
        {
            parameters["particles"] = null;
            parameters["particleCache"] = null;
        }
    }
}
Modified.Invoke(parameters, context);

