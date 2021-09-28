using System;
using System.Diagnostics;
using NekoPainter.Data;
using System.Numerics;
using System.Collections.Generic;

static class Modified
{
    public static void Invoke(Texture2D tex, IList<Stroke> strokes)
    {
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
                for (int i = stroke.position.Count - 1; i >= 0; i--)
                {
                    var point = stroke.position[i];
                    if (i > 0)
                    {
                        pathLength += Vector2.Distance(stroke.position[i], stroke.position[i - 1]);
                    }
                    for (int k = 0; k < 5; k++)
                    {
                        positions.Add(new Vector2((float)(random.NextDouble() - 0.5), (float)(random.NextDouble() - 0.5)) * pathLength * 0.3f + point);
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
                            rawTex[i] = new Vector4(0.1f * p1, 1.0f * p1, 0.1f * p1, 1.0f);
                        }

            }
        }
        tex.EndModification();
    }
}
if (parameters.TryGetValue("strokes", out object strokes))
    Modified.Invoke((Texture2D)parameters["texture2D"], (IList<Stroke>)strokes);
else
    Modified.Invoke((Texture2D)parameters["texture2D"], null);

