using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using DirectCanvas.Core;
using CanvasRendering;

namespace DirectCanvas.Layout
{
    public class PureLayout : PictureLayout
    {
        public PureLayout(DeviceResources deviceResources)
        {
        }

        public PureLayout(PureLayout copySource)
        {
            Hidden = copySource.Hidden;
            Alpha = copySource.Alpha;
            guid = Guid.NewGuid();

            BlendMode = copySource.BlendMode;
            Color = copySource.Color;
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
        public override Guid BlendMode
        {
            get => _blendMode; set
            {
                if (_blendMode == value) return;
                _blendMode = value;
                PropChange("BlendMode");
                PropChange("TypeDesc");
            }
        }

        public Vector4 Color
        {
            get => _color; set
            {
                _color = value;
                PropChange("Color");
            }
        }
        public Vector4 _color;

        public override void Dispose()
        {

        }
    }
}
