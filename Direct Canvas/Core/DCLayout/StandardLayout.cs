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
            if (tiledTexture != null)
            {
                tiledTexture.UnzipToTexture(PaintingTexture);
            }
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
                before = new TiledTexture(PaintingTexture, tt.TilesList);
                tt.UnzipToTexture(PaintingTexture);
            }
            else if (tiledTexture != null && tiledTexture.tilesCount > 0)
            {
                PaintingTextureTemp.Clear();
                tiledTexture.UnzipToTexture(PaintingTextureTemp);
                before = new TiledTexture(PaintingTextureTemp, tt.TilesList);
                tt.UnzipToTexture(PaintingTextureTemp);

                tiledTexture?.Dispose();
                tiledTexture = new TiledTexture(PaintingTextureTemp);
            }
            else
            {
                PaintingTextureTemp.Clear();
                before = new TiledTexture(PaintingTextureTemp, tt.TilesList);
                tiledTexture = new TiledTexture(tt);
            }
        }

        public override bool Hidden
        {
            get => _hidden; set
            {
                if (_hidden == value) return;
                _hidden = value;
                PropChange("Hidden");
            }
        }

        public override float Alpha
        {
            get => _alpha; set
            {
                if (_alpha == value) return;
                _alpha = value;
                PropChange("Alpha");
            }
        }

        public override void Dispose()
        {
            tiledTexture?.Dispose();
        }

        public override Guid BlendMode
        {
            get => _blendMode; set
            {
                _blendMode = value;

                PropChange("BlendMode");
                PropChange("TypeDesc");
            }
        }

        public TiledTexture tiledTexture { get; private set; }

        public bool saved = false;

        public bool PureLayout = false;
        public Vector4 Color;
    }
}