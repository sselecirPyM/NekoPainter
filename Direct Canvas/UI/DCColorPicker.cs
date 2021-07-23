using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.UI;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Documents;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;

// The Templated Control item template is documented at https://go.microsoft.com/fwlink/?LinkId=234235

namespace DirectCanvas.UI
{
    public sealed class DCColorPicker : Control
    {
        public DCColorPicker()
        {
            this.DefaultStyleKey = typeof(DCColorPicker);
        }

        protected override void OnApplyTemplate()
        {
            base.OnApplyTemplate();
            picker1 = (ColorSpectrum)GetTemplateChild("Picker1");
            picker2 = (ColorSpectrum)GetTemplateChild("Picker2");
            picker1.ColorChanged -= Picker1_ColorChanged;
            picker1.ColorChanged += Picker1_ColorChanged;
            picker2.ColorChanged -= Picker2_ColorChanged;
            picker2.ColorChanged += Picker2_ColorChanged;
        }

        ColorSpectrum picker1;
        ColorSpectrum picker2;

        
        public Color Color
        {
            get { return (Color)GetValue(ColorProperty); }
            set
            {
                if (Color == value||muted) return;
                muted = true;
                SetValue(ColorProperty, value);
                picker2.Color = value;
                System.Numerics.Vector4 color = picker1.HsvColor;
                picker1.HsvColor = new System.Numerics.Vector4(picker2.HsvColor.X, color.Y, color.Z, color.W);
                muted = false;
            }
        }
        public static readonly DependencyProperty ColorProperty =
            DependencyProperty.Register("Color", typeof(Color), typeof(DCColorPicker), new PropertyMetadata(Color.FromArgb(0xff, 0xff, 0xff, 0xff)));
        
        public string Header
        {
            get { return (string)GetValue(HeaderProperty); }
            set { SetValue(HeaderProperty, value); }
        }
        public static readonly DependencyProperty HeaderProperty =
            DependencyProperty.Register("Header", typeof(string), typeof(DCColorPicker), new PropertyMetadata(""));

        public event TypedEventHandler<DCColorPicker, EventArgs> ColorChanged;

        bool muted = false;

        private void Picker1_ColorChanged(ColorSpectrum sender, ColorChangedEventArgs args)
        {
            if (muted) return;
            muted = true;
            var x = sender.HsvColor;
            var color = picker2.HsvColor;
            picker2.HsvColor = new System.Numerics.Vector4(sender.HsvColor.X, color.Y, color.Z, color.W);
            Color = picker2.Color;
            ColorChanged?.Invoke(this, new EventArgs());
            muted = false;
        }
        private void Picker2_ColorChanged(ColorSpectrum sender, ColorChangedEventArgs args)
        {
            if (muted) return;
            Color = picker2.Color;
        }
    }
}
