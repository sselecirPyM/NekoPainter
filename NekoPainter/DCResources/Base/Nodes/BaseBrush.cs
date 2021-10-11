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
        parameters.TryGetValue("texture2D", out object ptexture2D);
        parameters.TryGetValue("strokes", out object pstrokes);

        IList<Stroke> strokes = (IList<Stroke>)pstrokes;
        Texture2D tex = (Texture2D)ptexture2D;
        Vector4 color = (Vector4)parameters["color"];
        float size = Math.Max((float)parameters["size"], 1);
        float spacing = Math.Max((float)parameters["spacing"], 0.01f);
        color.W *= 0.5f;
        int width = tex.width;
        int height = tex.height;
        List<Vector2> positions = new List<Vector2>();
        if (strokes != null)
        {
            var rawTex1 = new Vector4[width * height];
            foreach (var stroke in strokes)
            {
                float pathLength = 0;
                float distanceRemain = 0.0f;
                Vector2 prevPoint = stroke.position[0];
                positions.Add(prevPoint);
                for (int i = 0; i < stroke.position.Count; i++)
                {
                    var point = stroke.position[i];
                    if (i > 0)
                    {
                        while (Vector2.Distance(prevPoint, point) > size * spacing)
                        {
                            prevPoint += Vector2.Normalize(point - prevPoint) * size * spacing;
                            positions.Add(prevPoint);
                        }
                    }
                }
            }
            Dictionary<Int2, List<int>> divParticles = new Dictionary<Int2, List<int>>();
            for (int y = 0; y < height; y += 32)
                for (int x = 0; x < width; x += 32)
                {
                    divParticles[new Int2(x, y)] = new List<int>();
                }
            for (int i1 = 0; i1 < positions.Count; i1++)
            {
                var point = positions[i1];
                int x1 = Math.Max((int)(point.X - size / 2), 0);
                int y1 = Math.Max((int)(point.Y - size / 2), 0);
                int x2 = Math.Min((int)(point.X + size / 2) + 32, width);
                int y2 = Math.Min((int)(point.Y + size / 2) + 32, height);
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
                var rawTex = rawTex1;

                for (int i1 = 0; i1 < u.Value.Count; i1++)
                {
                    var point = positions[u.Value[i1]];

                    float size1 = size / 2;
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
                                Vector4 color1 = rawTex[i];
                                rawTex[i] = color1 * (1 - color.W) + new Vector4(color.X, color.Y, color.Z, 0.0f) * color.W;
                                rawTex[i] = new Vector4(rawTex[i].X, rawTex[i].Y, rawTex[i].Z, 1 - (1 - color1.W) * (1 - color.W));
                            }
                        }
                }
            });
            var gpuCompute = context.gpuCompute;
            var tex1 = gpuCompute.GetTemporaryTexture();
            tex1.UpdateTexture<Vector4>(rawTex1);
            gpuCompute.SetComputeShader("TextureBlendAlpha.json");
            gpuCompute.SetTexture("tex0", tex);
            gpuCompute.SetTexture("tex1", tex1);
            gpuCompute.For(0, tex.width, 0, tex.height);
        }
    }
}
Modified.Invoke(parameters, context);

