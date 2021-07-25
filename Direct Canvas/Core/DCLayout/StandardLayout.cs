using System.Collections;
using System.Collections.Generic;
using CanvasRendering;
using DirectCanvas.Core;
using System;
using System.Numerics;

namespace DirectCanvas.Layout
{
    public sealed class StandardLayout : PictureLayout
    {
        public StandardLayout() : base()
        {
        }
        public StandardLayout(StandardLayout standardLayout)
        {
            BlendMode = standardLayout.BlendMode;
            Alpha = standardLayout.Alpha;
            Color = standardLayout.Color;
            IsPureLayout = standardLayout.IsPureLayout;

            guid = Guid.NewGuid();
        }

        public static void Activate(RenderTexture PaintingTexture, TiledTexture tiledTexture)
        {
            PaintingTexture.Clear();
            tiledTexture?.UnzipToTexture(PaintingTexture);
        }

        public static void ReplaceTiles1(TiledTexture tt, ref TiledTexture layoutTexture, RenderTexture PaintingTextureTemp, RenderTexture PaintingTexture, out TiledTexture before, bool painting)
        {
            if (painting)
            {
                //before = new TiledTexture(PaintingTexture, tt.TilePositionList);
                tt.UnzipToTexture(PaintingTexture);
            }
            if (layoutTexture != null && layoutTexture.tilesCount > 0)
            {
                PaintingTextureTemp.Clear();
                layoutTexture.UnzipToTexture(PaintingTextureTemp);
                before = new TiledTexture(PaintingTextureTemp, tt.TilePositionList);
                tt.UnzipToTexture(PaintingTextureTemp);

                layoutTexture?.Dispose();
                layoutTexture = new TiledTexture(PaintingTextureTemp);
            }
            else
            {
                PaintingTextureTemp.Clear();
                before = new TiledTexture(PaintingTextureTemp, tt.TilePositionList);
                layoutTexture = new TiledTexture(tt);
            }
        }


        public override void Dispose()
        {

        }

        public bool saved = false;
    }
}