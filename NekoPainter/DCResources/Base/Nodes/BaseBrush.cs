﻿using System;
using System.Diagnostics;
using NekoPainter.Data;
using System.Numerics;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Runtime.InteropServices;

static class Modified
{
    public static void Invoke(Dictionary<string, object> parameters)
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
            var rawTex1 = tex.GetRawTexture1();
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
            foreach (var point in positions)
            {
                int x1 = (int)(point.X - size / 2);
                int y1 = (int)(point.Y - size / 2);
                int x2 = (int)(point.X + size / 2) + 1;
                int y2 = (int)(point.Y + size / 2) + 1;
                if (size > 50)
                    Parallel.For(y1, y2, y =>
                     {
                         var rawTex = MemoryMarshal.Cast<byte, Vector4>(rawTex1);
                         for (int x = x1; x < x2; x++)
                             if (x >= 0 && x < width && y >= 0 && y < height)
                             {
                                 int i = x + y * width;
                                 if (Vector2.Distance(point, new Vector2(x, y)) < size / 2)
                                 {
                                     float pa = rawTex[i].W;
                                     rawTex[i] = rawTex[i] * (1 - color.W) + new Vector4(color.X, color.Y, color.Z, 0.0f) * color.W;
                                     rawTex[i] = new Vector4(rawTex[i].X, rawTex[i].Y, rawTex[i].Z, 1 - (1 - pa) * (1 - color.W));
                                 }
                             }
                     });
                else
                {
                    var rawTex = MemoryMarshal.Cast<byte, Vector4>(rawTex1);
                    for (int y = y1; y < y2; y++)
                        for (int x = x1; x < x2; x++)
                            if (x >= 0 && x < width && y >= 0 && y < height)
                            {
                                int i = x + y * width;
                                if (Vector2.Distance(point, new Vector2(x, y)) < size / 2)
                                {
                                    float pa = rawTex[i].W;
                                    rawTex[i] = rawTex[i] * (1 - color.W) + new Vector4(color.X, color.Y, color.Z, 0.0f) * color.W;
                                    rawTex[i] = new Vector4(rawTex[i].X, rawTex[i].Y, rawTex[i].Z, 1 - (1 - pa) * (1 - color.W));
                                }
                            }
                }
            }
            tex.EndModification();
        }
    }
}
Modified.Invoke(parameters);

