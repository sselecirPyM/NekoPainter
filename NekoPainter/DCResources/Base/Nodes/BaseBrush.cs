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
        int width = tex._texture.width;
        int height = tex._texture.height;
        if (strokes != null)
        {
            foreach (var stroke in strokes)
            {
                foreach (var point in stroke.position)
                {
                    int x1 = (int)point.X;
                    int y1 = (int)point.Y;
                    for (int x = x1 - 10; x < x1; x++)
                        for (int y = y1 - 10; y < y1; y++)
                            if (x >= 0 && x < width && y >= 0 && y < height)
                            {
                                int i = x + y * width;
                                rawTex[i] = new Vector4(0.1f,1.0f,0.1f,1.0f);
                            }
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

