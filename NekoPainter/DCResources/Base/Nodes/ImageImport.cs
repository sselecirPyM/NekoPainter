using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Numerics;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;

static class ImageImport
{
    public static void Invoke(Dictionary<string, object> parameters)
    {
        parameters.TryGetValue("texture2D", out object ptexture2D);
        parameters.TryGetValue("file", out object pbytes);

        byte[] bytes = (byte[])pbytes;
        Texture2D tex = (Texture2D)ptexture2D;
        float scale = Math.Max((float)parameters["scale"], 0.001f);
        Vector2 offset = (Vector2)parameters["offset"];

        int width = tex.width;
        int height = tex.height;
        List<Vector2> positions = new List<Vector2>();
        if (bytes != null)
        {
            Image<RgbaVector> image = Image.Load<RgbaVector>(bytes);
            int width1 = Math.Max((int)(scale * image.Width), 1);
            int height1 = Math.Max((int)(scale * image.Height), 1);
            if (width1 != image.Width || height1 != image.Height)
                image.Mutate(x => x.Resize(width1, height1, KnownResamplers.Box));
            int x1 = (int)offset.X;
            int y1 = (int)offset.Y;
            int x2 = Math.Min(x1 + image.Width, width);
            int y2 = Math.Min(y1 + image.Height, height);
            var rawTex1 = tex.GetRawTexture1();
            var rawTex = MemoryMarshal.Cast<byte, Vector4>(rawTex1);
            for (int y = y1; y < y2; y++)
                for (int x = x1; x < x2; x++)
                {
                    if (x < 0 || y < 0 || x - x1 < 0 || y - y1 < 0) continue;
                    int i = x + y * width;
                    var pixel = image[x - x1, y - y1];
                    rawTex[i] = new Vector4(pixel.R, pixel.G, pixel.B, pixel.A);
                }

            tex.EndModification();
        }
    }
}
ImageImport.Invoke(parameters);
