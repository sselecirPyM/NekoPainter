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

namespace DirectCanvas.UI
{
    public partial class ResizablePanel : UserControl
    {
        public ResizablePanel()
        {
            this.InitializeComponent();
        }

        public void Panel_Drag(object sender, DragDeltaEventArgs e)
        {
            var parent = VisualTreeHelper.GetParent(this) as Canvas;
            double parentWidth = parent.ActualWidth;
            double parentHeight = parent.ActualHeight;
            if (parentWidth < Width) Width = parentWidth;
            if (parentHeight < Height) Height = parentHeight;

            double Horizontal = Math.Clamp(Canvas.GetLeft(this) + e.HorizontalChange, 0, parentWidth - Width);
            Canvas.SetLeft(this, Horizontal);

            double vertical = Math.Clamp(Canvas.GetTop(this) + e.VerticalChange, 0, parentHeight - Height);
            Canvas.SetTop(this, vertical);
        }

        public void Resizer_TL(object sender, DragDeltaEventArgs e)
        {
            ResizeTop(e);
            ResizeLeft(e);
        }

        public void Resizer_T(object sender, DragDeltaEventArgs e)
        {
            ResizeTop(e);
        }

        public void Resizer_TR(object sender, DragDeltaEventArgs e)
        {
            ResizeTop(e);
            ResizeRight(e);
        }

        public void Resizer_L(object sender, DragDeltaEventArgs e)
        {
            ResizeLeft(e);
        }

        public void Resizer_R(object sender, DragDeltaEventArgs e)
        {
            ResizeRight(e);
        }

        public void Resizer_BL(object sender, DragDeltaEventArgs e)
        {
            ResizeBottom(e);
            ResizeLeft(e);
        }

        public void Resizer_B(object sender, DragDeltaEventArgs e)
        {
            ResizeBottom(e);
        }

        public void Resizer_BR(object sender, DragDeltaEventArgs e)
        {
            ResizeBottom(e);
            ResizeRight(e);
        }

        private void ResizeTop(DragDeltaEventArgs e)
        {
            double deltaVertical;
            deltaVertical = Math.Max(Math.Min(e.VerticalChange, ActualHeight - MinHeight), -Canvas.GetTop(this));
            Canvas.SetTop(this, Canvas.GetTop(this) + deltaVertical);
            Height -= deltaVertical;
        }

        private void ResizeLeft(DragDeltaEventArgs e)
        {
            double deltaHorizontal;
            deltaHorizontal = Math.Max(Math.Min(e.HorizontalChange, ActualWidth - MinWidth), -Canvas.GetLeft(this));
            Canvas.SetLeft(this, Canvas.GetLeft(this) + deltaHorizontal);
            Width -= deltaHorizontal;
        }

        private void ResizeBottom(DragDeltaEventArgs e)
        {
            var parent = VisualTreeHelper.GetParent(this) as Canvas;
            double parentHeight = parent.ActualHeight;

            double deltaVertical;
            deltaVertical = Math.Max(Math.Min(-e.VerticalChange, ActualHeight - MinHeight), Canvas.GetTop(this) + Height - parentHeight);
            Height -= deltaVertical;
        }

        private void ResizeRight(DragDeltaEventArgs e)
        {
            var parent = VisualTreeHelper.GetParent(this) as Canvas;
            double parentWidth = parent.ActualWidth;

            double deltaHorizontal;
            deltaHorizontal = Math.Max(Math.Min(-e.HorizontalChange, ActualWidth - MinWidth), Canvas.GetLeft(this) + Width - parentWidth);
            Width -= deltaHorizontal;
        }
    }
}
