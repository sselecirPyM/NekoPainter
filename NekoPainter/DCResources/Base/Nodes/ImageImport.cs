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
    public static void Invoke(Dictionary<string, object> parameters, NodeContext context)
    {
        parameters.TryGetValue("texture2D", out object ptexture2D);
        parameters.TryGetValue("file", out object pbytes);

        byte[] bytes = (byte[])pbytes;
        ITexture2D tex = (ITexture2D)ptexture2D;
        float scale = Math.Max((float)parameters["scale"], 0.001f);
        Vector2 offset = (Vector2)parameters["offset"];

        int width = tex.width;
        int height = tex.height;
        List<Vector2> positions = new List<Vector2>();
        if (bytes != null)
        {
            Image<RgbaVector> image = Image.Load<RgbaVector>(bytes);
            int offsetX = (int)offset.X;
            int offsetY = (int)offset.Y;

            int leftA = (int)Math.Floor(-offsetX / scale);
            int topA = (int)Math.Floor(-offsetY / scale);

            int width1 = (int)(scale * image.Width);
            int height1 = (int)(scale * image.Height);
            if (width1 < 1 || height1 < 1) return;

            int left = Math.Max(leftA, 0);
            int top = Math.Max(topA, 0);
            int sizeX = Math.Min((int)Math.Ceiling(width  / scale), image.Width - left);
            int sizeY = Math.Min((int)Math.Ceiling(height / scale), image.Height - top);

            int widthA = Math.Max((int)(scale * sizeX), 1);
            int heightA = Math.Max((int)(scale * sizeY), 1);
            bool resized = false;
            var rawTex = new Vector4[width * height];
            var gpuCompute = context.gpuCompute;

            gpuCompute.SetParameter("sampler1", new SamplerStateDef()
            {
                AddressU = AddressModeDef.Clamp,
                AddressV = AddressModeDef.Clamp,
                AddressW = AddressModeDef.Clamp,
                filter = FilterDef.MinMagMipLinear
            });
            if (width1 < image.Width || height1 < image.Height)
            {
                resized = true;
                if (sizeX < 1 || sizeY < 1) return;
                image.Mutate(x => x.Crop(new Rectangle(left, top, sizeX, sizeY)));
                image.Mutate(x => x.Resize(widthA, heightA, KnownResamplers.Box));
                int x1 = (offsetX + (int)(left * scale));
                int y1 = (offsetY + (int)(top * scale));
                int x2 = Math.Min(x1 + image.Width, width);
                int y2 = Math.Min(y1 + image.Height, height);
                int x3 = Math.Max(x1, 0);
                int y3 = Math.Max(y1, 0);

                for (int y = y3; y < y2; y++)
                    for (int x = x3; x < x2; x++)
                    {
                        if (x - x1 < 0 || y - y1 < 0) continue;
                        int i = x + y * width;
                        var pixel = image[x - x1, y - y1];
                        rawTex[i] = new Vector4(pixel.R, pixel.G, pixel.B, pixel.A);
                    }
                gpuCompute.SetParameter("sourceRect", new Vector4(x1, y1, x2 - x1, y2 - y1));
                gpuCompute.SetParameter("targetRect", new Vector4(x1, y1, x2 - x1, y2 - y1));
                var tex1 = gpuCompute.GetTemporaryTexture();
                tex1.UpdateTexture<Vector4>(rawTex);
                gpuCompute.SetTexture("tex0", tex);
                gpuCompute.SetTexture("tex1", tex1);
                gpuCompute.SetComputeShader("TextureResize.json");
                gpuCompute.For(x3, x2, y3, y2);
            }
            else
            {
                int x1 = -leftA;
                int y1 = -topA;
                int x2 = Math.Min(x1 + image.Width, width);
                int y2 = Math.Min(y1 + image.Height, height);
                int x3 = Math.Max(x1, 0);
                int y3 = Math.Max(y1, 0);
                for (int y = y3; y < y2; y++)
                    for (int x = x3; x < x2; x++)
                    {
                        if (x - x1 < 0 || y - y1 < 0) continue;
                        int i = x + y * width;
                        var pixel = image[x - x1, y - y1];
                        rawTex[i] = new Vector4(pixel.R, pixel.G, pixel.B, pixel.A);
                    }
                int x4 = offsetX;
                int y4 = offsetY;
                gpuCompute.SetParameter("sourceRect", new Vector4(x1, y1, image.Width, image.Height));
                gpuCompute.SetParameter("targetRect", new Vector4(x4, y4, width1, height1));
                var tex1 = gpuCompute.GetTemporaryTexture();
                tex1.UpdateTexture<Vector4>(rawTex);
                gpuCompute.SetTexture("tex0", tex);
                gpuCompute.SetTexture("tex1", tex1);
                gpuCompute.SetComputeShader("TextureResize.json");
                gpuCompute.For(Math.Max(x4, 0), Math.Min(x4 + width1, width), Math.Max(y4, 0), Math.Min(y4 + height1, height));
            }
        }
    }
}
ImageImport.Invoke(parameters, context);
