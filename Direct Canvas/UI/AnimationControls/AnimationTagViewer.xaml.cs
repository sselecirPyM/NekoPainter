using DirectCanvas.Core.Director;
using Microsoft.UI.Xaml.Controls;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.ApplicationModel.DataTransfer;
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

namespace DirectCanvas.UI.AnimationControls
{
    public sealed partial class AnimationTagViewer : UserControl
    {
        public AnimationTagViewer()
        {
            this.InitializeComponent();
        }

        public Animation Animation
        {
            get { return (Animation)GetValue(AnimationProperty); }
            set { SetValue(AnimationProperty, value); }
        }
        public static readonly DependencyProperty AnimationProperty =
            DependencyProperty.Register("Animation", typeof(Animation), typeof(AnimationTagViewer), new PropertyMetadata(null));

        
        private void UserControl_PointerWheelChanged(object sender, PointerRoutedEventArgs e)
        {
            RailLayout.Scaler *= (float)Math.Pow(1.0014450997779993488675056142818, e.GetCurrentPoint(this).Properties.MouseWheelDelta);
        }


        private void Tag_DragStarting(UIElement sender, DragStartingEventArgs args)
        {
            var p1 = args.GetPosition(this);
            var p2 = args.GetPosition(sender);
            var p3 = p1.X - p2.X / logicDpiScale;
            args.Data.Properties.Add("PosX", p3);
            var uiElement = sender;
            for (int i = 0; i < 3; i++)
            {
                uiElement = (UIElement)VisualTreeHelper.GetParent(uiElement);
            }
            args.Data.Properties.Add("Rail", ((ItemsRepeater)uiElement).ItemsSource);
            args.Data.Properties.Add("Data", (sender as Grid).DataContext);
            args.Data.Properties.Add("owner", this);
        }
        float logicDpiScale = Windows.Graphics.Display.DisplayInformation.GetForCurrentView().LogicalDpi / 96.0f;

        private void Tag_GotFocus(object sender, RoutedEventArgs e)
        {

        }

        private void Border_DragOver(object sender, DragEventArgs e)
        {
            e.Handled = true;
            if (!(e.DataView.Properties.TryGetValue("owner", out var o) && o == this))
            {
                return;
            }
            Point point = e.GetPosition(this);
            e.AcceptedOperation = DataPackageOperation.Move;
            int delta = (int)((point.X - ((double)e.DataView.Properties["PosX"])) / RailLayout.Scaler);
            e.DragUIOverride.Caption = string.Format("帧{1} 移动{0}", delta, delta + ((AnimationTag)e.DataView.Properties["Data"]).FrameIndex);
        }

        private void Border_DragEnter(object sender, DragEventArgs e)
        {

            if (e.Data == null) return;
            e.Handled = true;
        }

        private void Border_Drop(object sender, DragEventArgs e)
        {
            Point point = e.GetPosition(this);
            ((AnimationTag)e.DataView.Properties["Data"]).FrameIndex += (int)((point.X - ((double)e.DataView.Properties["PosX"])) / RailLayout.Scaler);
            ((AnimationTagRail)e.DataView.Properties["Rail"]).Sort();
            RailLayout.InvalidateMeasurePublicMethod();
        }
    }

    class AnimationTagViewLayout : VirtualizingLayout
    {
        #region Layout parameters
        const int c_railHeight = 24;


        public double Scaler
        {
            get { return (double)GetValue(ScalerProperty); }
            set { SetValue(ScalerProperty, value); }
        }
        public static readonly DependencyProperty ScalerProperty =
            DependencyProperty.Register("Scaler", typeof(double), typeof(AnimationTagViewLayout), new PropertyMetadata(1.0, LayoutChanged));

        private static void LayoutChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ((AnimationTagViewLayout)d).InvalidateMeasure();
        }

        public void InvalidateMeasurePublicMethod()
        {
            InvalidateMeasure();
        }

        #endregion

        #region Setup / teardown

        protected override void InitializeForContextCore(VirtualizingLayoutContext context)
        {
            base.InitializeForContextCore(context);

            var state = context.LayoutState as AnimationTagViewLayoutState;
            if (state == null)
            {
                context.LayoutState = new AnimationTagViewLayoutState();
            }
        }

        protected override void UninitializeForContextCore(VirtualizingLayoutContext context)
        {
            base.UninitializeForContextCore(context);

            // clear any state
            context.LayoutState = null;
        }

        #endregion

        #region Layout

        protected override Size MeasureOverride(VirtualizingLayoutContext context, Size availableSize)
        {
            var state = context.LayoutState as AnimationTagViewLayoutState;
            var scaler = Scaler;
            state.LayoutRects.Clear();
            int firstColumnIndex = context.ItemCount;
            int lastColumnIndex = 0;
            int lastFrameIndex = 0;
            int xFrameIndex = (int)(context.RealizationRect.Left / scaler) - 1;
            int yFrameIndex = (int)(context.RealizationRect.Right / scaler) + 1;
            for (int i = 0; i < context.ItemCount; i++)
            {
                var data = (AnimationTag)context.GetItemAt(i);
                firstColumnIndex = i;
                if (data.FrameIndex > xFrameIndex) break;
            }
            for (int i = firstColumnIndex; i < context.ItemCount; i++)
            {
                var data = (AnimationTag)context.GetItemAt(i);
                lastColumnIndex = i + 1;//考虑不被执行的情况
                if (data.FrameIndex > yFrameIndex) break;
            }
            for (int i = firstColumnIndex; i < lastColumnIndex; i++)
            {
                var data = (AnimationTag)context.GetItemAt(i);
                var container = context.GetOrCreateElementAt(i);

                var rect = new Rect(data.FrameIndex * scaler, 4, 16, 16);
                state.LayoutRects.Add(rect);
                container.Measure(new Size(rect.Width, c_railHeight));
            }

            if (context.ItemCount > 0)
            {
                lastFrameIndex = ((AnimationTag)context.GetItemAt(context.ItemCount - 1)).FrameIndex;
            }

            state.FirstRealizedIndex = firstColumnIndex;
            return new Size(lastFrameIndex * scaler + 300, c_railHeight);
        }

        protected override Size ArrangeOverride(VirtualizingLayoutContext context, Size finalSize)
        {
            // walk through the cache of containers and arrange
            var state = context.LayoutState as AnimationTagViewLayoutState;
            var virtualContext = context as VirtualizingLayoutContext;
            int currentIndex = state.FirstRealizedIndex;

            foreach (var arrangeRect in state.LayoutRects)
            {
                var container = virtualContext.GetOrCreateElementAt(currentIndex);
                container.Arrange(arrangeRect);
                currentIndex++;
            }

            return finalSize;
        }

        #endregion
        #region Helper methods
        #endregion
    }

    internal class AnimationTagViewLayoutState
    {
        public int FirstRealizedIndex { get; set; }

        public List<Rect> LayoutRects
        {
            get
            {
                if (_layoutRects == null)
                {
                    _layoutRects = new List<Rect>();
                }

                return _layoutRects;
            }
        }

        private List<Rect> _layoutRects;
    }
}
