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
        public StandardLayout(TiledTexture loadedData)
        {
            tiledTexture = loadedData;
        }
        public StandardLayout(RenderTexture Data)
        {
            tiledTexture = new TiledTexture(Data);
        }
        public StandardLayout(StandardLayout standardLayout, RenderTexture PaintingTexture)
        {
            if (standardLayout.activated)
            {
                tiledTexture = new TiledTexture(PaintingTexture);
            }
            else if (standardLayout.tiledTexture != null)
            {
                tiledTexture = new TiledTexture(standardLayout.tiledTexture);
            }
            BlendMode = standardLayout.BlendMode;
            Alpha = standardLayout.Alpha;

            guid = Guid.NewGuid();
        }

        public void Activate(RenderTexture PaintingTexture)
        {
            if (activated) return;
            activated = true;
            PaintingTexture.Clear();
            tiledTexture?.UnzipToTexture(PaintingTexture);
        }
        public void Deactivate(RenderTexture PaintingTexture)
        {
            if (!activated) return;
            activated = false;
            tiledTexture?.Dispose();
            tiledTexture = new TiledTexture(PaintingTexture);
        }
        public bool activated { get; private set; } = false;

        public void ReplaceTiles(TiledTexture tt, RenderTexture PaintingTextureTemp, RenderTexture PaintingTexture, out TiledTexture before)
        {
            if (activated)
            {
                before = new TiledTexture(PaintingTexture, tt.TilePositionList);
                tt.UnzipToTexture(PaintingTexture);
            }
            else if (tiledTexture != null && tiledTexture.tilesCount > 0)
            {
                PaintingTextureTemp.Clear();
                tiledTexture.UnzipToTexture(PaintingTextureTemp);
                before = new TiledTexture(PaintingTextureTemp, tt.TilePositionList);
                tt.UnzipToTexture(PaintingTextureTemp);

                tiledTexture?.Dispose();
                tiledTexture = new TiledTexture(PaintingTextureTemp);
            }
            else
            {
                PaintingTextureTemp.Clear();
                before = new TiledTexture(PaintingTextureTemp, tt.TilePositionList);
                tiledTexture = new TiledTexture(tt);
            }
        }


        public override void Dispose()
        {
            tiledTexture?.Dispose();
        }

        public TiledTexture tiledTexture { get; private set; }

        public bool saved = false;

        public bool PureLayout = false;
    }
}