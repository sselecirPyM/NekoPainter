using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

//https://go.microsoft.com/fwlink/?LinkId=234236 上介绍了“用户控件”项模板

namespace DirectCanvas.UI.Widgets
{
    public sealed partial class DCUIControl_Parameter : UserControl
    {
        public DCUIControl_Parameter()
        {
            this.InitializeComponent();
        }

        private void ParameterSlider_ValueChanged(object sender, RangeBaseValueChangedEventArgs e)
        {
            if (muted) return;
            DCBrushParameter.Value = (int)e.NewValue;
            ValueChanged?.Invoke(this, new EventArgs());
        }

        public Core.DCParameter DCBrushParameter
        {
            get { return (Core.DCParameter)GetValue(DCBrushParameterProperty); }
            set
            {
                muted = true;
                SetValue(DCBrushParameterProperty, value);
                if (value != null)
                {
                    if (value.ViewSlider)
                    {
                        ParameterSlider.Maximum = value.MaxValue;
                        ParameterSlider.Minimum = value.MinValue;
                        ParameterSlider.Value = value.Value;

                    }
                    else if (value.ViewTextBox)
                    {
                        if (value.IsFloat)
                        {
                            ParameterTextBox.Text = value.fValue.ToString();
                        }
                        else
                        {
                            ParameterTextBox.Text = value.Value.ToString();
                        }
                    }
                }
                muted = false;
            }
        }

        // Using a DependencyProperty as the backing store for DCBrushParameter.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty DCBrushParameterProperty =
            DependencyProperty.Register("DCBrushParameter", typeof(Core.DCParameter), typeof(DCUIControl_Parameter), new PropertyMetadata(null));

        bool muted = false;

        public event EventHandler ValueChanged;

        private void TextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (muted) return;
            if (DCBrushParameter.IsFloat)
            {
                if (float.TryParse((sender as TextBox).Text, out float value) && DCBrushParameter.fValue != value)
                {
                    DCBrushParameter.fValue = value;
                    ValueChanged?.Invoke(this, new EventArgs());
                }
            }
            else if (int.TryParse((sender as TextBox).Text, out int value) && DCBrushParameter.Value != value)
            {
                DCBrushParameter.Value = value;
                ValueChanged?.Invoke(this, new EventArgs());
            }
        }
    }
}
