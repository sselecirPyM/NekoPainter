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
        //Vector4 color = (Vector4)parameters["color"];
        //float spacing = Math.Max((float)parameters["spacing"], 0.01f);

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
            int x2 = Math.Min(image.Width, width);
            int y2 = Math.Min(image.Height, height);
            var rawTex1 = tex.GetRawTexture1();
            var rawTex = MemoryMarshal.Cast<byte, Vector4>(rawTex1);
            for (int y = 0; y < y2; y++)
                for (int x = 0; x < x2; x++)
                {
                    int i = x + y * width;
                    var pixel = image[x, y];
                    rawTex[i] = new Vector4(pixel.R, pixel.G, pixel.B, pixel.A);
                }

            tex.EndModification();
        }
    }
}
ImageImport.Invoke(parameters);
