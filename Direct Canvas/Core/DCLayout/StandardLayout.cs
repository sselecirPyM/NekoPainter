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
        //public StandardLayout(TiledTexture loadedData)
        //{
        //    tiledTexture = loadedData;
        //}
        //public StandardLayout(RenderTexture Data)
        //{
        //    tiledTexture = new TiledTexture(Data);
        //}
        public StandardLayout(StandardLayout standardLayout)
        {
            //if (standardLayout.activated)
            //{
            //    tiledTexture = new TiledTexture(PaintingTexture);
            //}
            //else if (standardLayout.tiledTexture != null)
            //{
            //    tiledTexture = new TiledTexture(standardLayout.tiledTexture);
            //}
            BlendMode = standardLayout.BlendMode;
            Alpha = standardLayout.Alpha;
            Color = standardLayout.Color;
            IsPureLayout = standardLayout.IsPureLayout;

            guid = Guid.NewGuid();
        }

        //public void Activate(RenderTexture PaintingTexture)
        //{
        //    if (activated) return;
        //    activated = true;
        //    PaintingTexture.Clear();
        //    tiledTexture?.UnzipToTexture(PaintingTexture);
        //}

        public static void Activate(RenderTexture PaintingTexture, TiledTexture tiledTexture)
        {
            PaintingTexture.Clear();
            tiledTexture?.UnzipToTexture(PaintingTexture);
        }
        //public void Deactivate(RenderTexture PaintingTexture)
        //{
        //    if (!activated) return;
        //    activated = false;
        //    tiledTexture?.Dispose();
        //    tiledTexture = new TiledTexture(PaintingTexture);
        //}
        //public static void Deactivate(RenderTexture PaintingTexture, ref TiledTexture tiledTexture)
        //{
        //    tiledTexture?.Dispose();
        //    tiledTexture = new TiledTexture(PaintingTexture);
        //}
        //public bool activated { get; private set; } = false;

        //public void ReplaceTiles(TiledTexture tt, ref TiledTexture layoutTexture, RenderTexture PaintingTextureTemp, RenderTexture PaintingTexture, out TiledTexture before)
        //{
        //    if (activated)
        //    {
        //        before = new TiledTexture(PaintingTexture, tt.TilePositionList);
        //        tt.UnzipToTexture(PaintingTexture);
        //    }
        //    else if (layoutTexture != null && layoutTexture.tilesCount > 0)
        //    {
        //        PaintingTextureTemp.Clear();
        //        layoutTexture.UnzipToTexture(PaintingTextureTemp);
        //        before = new TiledTexture(PaintingTextureTemp, tt.TilePositionList);
        //        tt.UnzipToTexture(PaintingTextureTemp);

        //        layoutTexture?.Dispose();
        //        layoutTexture = new TiledTexture(PaintingTextureTemp);
        //    }
        //    else
        //    {
        //        PaintingTextureTemp.Clear();
        //        before = new TiledTexture(PaintingTextureTemp, tt.TilePositionList);
        //        layoutTexture = new TiledTexture(tt);
        //    }
        //}

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
            //tiledTexture?.Dispose();
        }

        //public TiledTexture tiledTexture;

        public bool saved = false;

        public bool IsPureLayout = false;
    }
}