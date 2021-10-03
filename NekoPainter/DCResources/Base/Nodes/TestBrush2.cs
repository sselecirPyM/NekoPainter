using System;
using System.Diagnostics;
using NekoPainter.Data;
using System.Numerics;
using System.Collections.Generic;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Drawing;
using SixLabors.ImageSharp.Drawing.Processing;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;

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
        float range = (float)parameters["range"];
        int width = tex.width;
        int height = tex.height;
        Random random = new Random(2);
        Random random1 = new Random(5);
        List<Vector2> positions = new List<Vector2>();
        DateTime dateTime = DateTime.Now;
        if (strokes != null)
        {
            var rawTex1 = tex.GetRawTexture1();
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
                        positions.Add(new Vector2((float)(random.NextDouble() - 0.5), (float)(random.NextDouble() - 0.5)) * (pathLength * rangeByDistance + range) + point);
                        distanceRemain -= 1;
                    }
                }
            }
            if (positions.Count > 1)
            {
                PointF[] points = new PointF[positions.Count];
                for (int i = 0; i < positions.Count; i++)
                {
                    points[i] = new PointF(positions[i].X, positions[i].Y);
                }
                var image = Image.WrapMemory<RgbaVector>(rawTex1, width, height);
                LinearLineSegment a = new LinearLineSegment(points);
                SixLabors.ImageSharp.Drawing.Path path1 = new SixLabors.ImageSharp.Drawing.Path(a);
                image.Mutate(x => x.Draw(new Color(color), 2, path1));
            }
        }
        tex.EndModification();
    }
}
Modified.Invoke(parameters);

