using System;
using System.Diagnostics;
using NekoPainter.Data;
using System.Numerics;
using System.Collections.Generic;

static class Modified
{
    public static void Invoke(Dictionary<string, object> parameters)
    {
        parameters.TryGetValue("texture2D", out object ptexture2D);
        parameters.TryGetValue("strokes", out object pstrokes);

        IList<Stroke> strokes = (IList<Stroke>)pstrokes;
        Texture2D tex = (Texture2D)ptexture2D;
        Vector4 color = (Vector4)parameters["color"];
        float countByDistance = (float)parameters["countByDistance"];
        float countByDistanceSquare = (float)parameters["countByDistanceSquare"];
        float rangeByDistance = (float)parameters["rangeByDistance"];
        var rawTex = tex.GetRawTexture();
        int f1 = rawTex.Length / 4;
        int width = tex.width;
        int height = tex.height;
        Random random = new Random(2);
        Random random1 = new Random(5);
        List<Vector2> positions = new List<Vector2>();
        DateTime dateTime = DateTime.Now;
        if (strokes != null)
        {
            foreach (var stroke in strokes)
            {
                float pathLength = 0;
                float distanceRemain = 0.0f;
                for (int i = stroke.position.Count - 1; i >= 0; i--)
                {
                    var point = stroke.position[i];
                    if (i > 0)
                    {
                        float distance = Vector2.Distance(stroke.position[i], stroke.position[i - 1]);
                        pathLength += distance;
                        distanceRemain += distance * countByDistance + (pathLength * distance * 2 - distance * distance) * countByDistanceSquare;
                    }
                    while (distanceRemain > 1.0f)
                    {
                        positions.Add(new Vector2((float)(random.NextDouble() - 0.5), (float)(random.NextDouble() - 0.5)) * pathLength * rangeByDistance + point);
                        distanceRemain -= 1;
                    }
                }
            }
            foreach (var point in positions)
            {
                float p1 = (float)random1.NextDouble();
                p1 = (1 - dateTime.Millisecond / 1000.0f + p1) % 1.0f;
                int x1 = (int)point.X;
                int y1 = (int)point.Y;
                for (int x = x1 - 2; x < x1 + 3; x++)
                    for (int y = y1 - 2; y < y1 + 3; y++)
                        if (x >= 0 && x < width && y >= 0 && y < height)
                        {
                            int i = x + y * width;
                            rawTex[i] += color * new Vector4(p1, p1, p1, 1.0f);
                        }

            }
        }
        tex.EndModification();
    }
}
Modified.Invoke(parameters);

