using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
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
    public sealed class FloatingPanel : ContentControl
    {
        public FloatingPanel()
        {
            this.DefaultStyleKey = typeof(FloatingPanel);
        }

        public string Header
        {
            get { return (string)GetValue(HeaderProperty); }
            set { SetValue(HeaderProperty, value); }
        }

        public static readonly DependencyProperty HeaderProperty =
            DependencyProperty.Register("Header", typeof(string), typeof(FloatingPanel), new PropertyMetadata(""));

        public bool Visible
        {
            get { return (bool)GetValue(VisibleProperty); }
            set { SetValue(VisibleProperty, value); Visibility = value ? Visibility.Visible : Visibility.Collapsed; }
        }

        public static readonly DependencyProperty VisibleProperty =
            DependencyProperty.Register("Visible", typeof(bool), typeof(FloatingPanel), new PropertyMetadata(true));

        protected override void OnApplyTemplate()
        {
            base.OnApplyTemplate();
            dragThumb = (Thumb)GetTemplateChild("DragThumb");
            _rTL = (Thumb)GetTemplateChild("_rTL");
            _rT = (Thumb)GetTemplateChild("_rT");
            _rTR = (Thumb)GetTemplateChild("_rTR");
            _rL = (Thumb)GetTemplateChild("_rL");
            _rR = (Thumb)GetTemplateChild("_rR");
            _rBL = (Thumb)GetTemplateChild("_rBL");
            _rB = (Thumb)GetTemplateChild("_rB");
            _rBR = (Thumb)GetTemplateChild("_rBR");
            closeButtuon = (Button)GetTemplateChild("CloseButton");
            dragThumb.DragDelta -= Panel_Drag;
            dragThumb.DragDelta += Panel_Drag;
            _rTL.DragDelta -= Resizer_TL;
            _rTL.DragDelta += Resizer_TL;
            _rT.DragDelta -= Resizer_T;
            _rT.DragDelta += Resizer_T;
            _rTR.DragDelta -= Resizer_TR;
            _rTR.DragDelta += Resizer_TR;
            _rL.DragDelta -= Resizer_L;
            _rL.DragDelta += Resizer_L;
            _rR.DragDelta -= Resizer_R;
            _rR.DragDelta += Resizer_R;
            _rBL.DragDelta -= Resizer_BL;
            _rBL.DragDelta += Resizer_BL;
            _rB.DragDelta -= Resizer_B;
            _rB.DragDelta += Resizer_B;
            _rBR.DragDelta -= Resizer_BR;
            _rBR.DragDelta += Resizer_BR;
            dragThumb.DragStarted -= Control_Drag;
            dragThumb.DragStarted += Control_Drag;
            _rTL.DragStarted -= Control_Drag;
            _rTL.DragStarted += Control_Drag;
            _rT.DragStarted -= Control_Drag;
            _rT.DragStarted += Control_Drag;
            _rTR.DragStarted -= Control_Drag;
            _rTR.DragStarted += Control_Drag;
            _rL.DragStarted -= Control_Drag;
            _rL.DragStarted += Control_Drag;
            _rR.DragStarted -= Control_Drag;
            _rR.DragStarted += Control_Drag;
            _rBL.DragStarted -= Control_Drag;
            _rBL.DragStarted += Control_Drag;
            _rB.DragStarted -= Control_Drag;
            _rB.DragStarted += Control_Drag;
            _rBR.DragStarted -= Control_Drag;
            _rBR.DragStarted += Control_Drag;

            closeButtuon.Click -= CloseButtuon_Click;
            closeButtuon.Click += CloseButtuon_Click;
        }

        private void CloseButtuon_Click(object sender, RoutedEventArgs e)
        {
            this.Visible = false;
        }

        Thumb dragThumb;
        Thumb _rTL;
        Thumb _rT;
        Thumb _rTR;
        Thumb _rL;
        Thumb _rR;
        Thumb _rBL;
        Thumb _rB;
        Thumb _rBR;
        Button closeButtuon;

        double canvasLeftOrigin;
        double canvasTopOrigin;
        double canvasLeftDelta;
        double canvasTopDelta;
        double widthOrigin;
        double heightOrigin;

        public void Panel_Drag(object sender, DragDeltaEventArgs e)
        {
            var parent = VisualTreeHelper.GetParent(this) as Canvas;
            double parentWidth = parent.ActualWidth;
            double parentHeight = parent.ActualHeight;
            if (parentWidth < Width) Width = parentWidth;
            if (parentHeight < Height) Height = parentHeight;

            canvasLeftDelta += e.HorizontalChange;
            canvasTopDelta += e.VerticalChange;
            double Horizontal = Math.Clamp(canvasLeftOrigin + canvasLeftDelta, 0, parentWidth - Width);
            Canvas.SetLeft(this, Horizontal);

            double vertical = Math.Clamp(canvasTopOrigin + canvasTopDelta, 0, parentHeight - Height);
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
            canvasTopDelta += e.VerticalChange;
            double deltaVertical;
            deltaVertical = Math.Max(Math.Min(canvasTopDelta, heightOrigin - MinHeight), -canvasTopOrigin);
            Canvas.SetTop(this, canvasTopOrigin + deltaVertical);
            Height = heightOrigin - deltaVertical;
        }

        private void ResizeLeft(DragDeltaEventArgs e)
        {
            canvasLeftDelta += e.HorizontalChange;
            double deltaHorizontal;
            deltaHorizontal = Math.Max(Math.Min(canvasLeftDelta, widthOrigin - MinWidth), -canvasLeftOrigin);
            Canvas.SetLeft(this, canvasLeftOrigin + deltaHorizontal);
            Width = widthOrigin - deltaHorizontal;
        }

        private void ResizeBottom(DragDeltaEventArgs e)
        {
            canvasTopDelta += e.VerticalChange;
            var parent = VisualTreeHelper.GetParent(this) as Canvas;
            double parentHeight = parent.ActualHeight;

            double deltaVertical;
            deltaVertical = Math.Max(Math.Min(-canvasTopDelta, heightOrigin - MinHeight), canvasTopOrigin + heightOrigin - parentHeight);
            Height = heightOrigin - deltaVertical;
        }

        private void ResizeRight(DragDeltaEventArgs e)
        {
            canvasLeftDelta += e.HorizontalChange;
            var parent = VisualTreeHelper.GetParent(this) as Canvas;
            double parentWidth = parent.ActualWidth;

            double deltaHorizontal;
            deltaHorizontal = Math.Max(Math.Min(-canvasLeftDelta, widthOrigin - MinWidth), canvasLeftOrigin + widthOrigin - parentWidth);
            Width = widthOrigin - deltaHorizontal;
        }


        public event RoutedEventHandler Click;

        private void Control_Drag(object sender, RoutedEventArgs e)
        {
            canvasLeftOrigin = Canvas.GetLeft(this);
            canvasTopOrigin = Canvas.GetTop(this);
            canvasLeftDelta = 0;
            canvasTopDelta = 0;
            widthOrigin = Width;
            heightOrigin = Height;
            Click?.Invoke(this, new RoutedEventArgs());
        }
    }
}
