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
    public sealed partial class TimelineViewer : UserControl
    {
        public TimelineViewer()
        {
            this.InitializeComponent();
        }


        public Timeline Timeline
        {
            get { return (Timeline)GetValue(TimelineProperty); }
            set { SetValue(TimelineProperty, value); }
        }
        public static readonly DependencyProperty TimelineProperty =
            DependencyProperty.Register("Timeline", typeof(Timeline), typeof(TimelineViewer), new PropertyMetadata(null));



        private void UserControl_PointerWheelChanged(object sender, PointerRoutedEventArgs e)
        {
            RailLayout.Scaler *= (float)Math.Pow(1.0014450997779993488675056142818, e.GetCurrentPoint(this).Properties.MouseWheelDelta);
        }

        private void TimelineItem_DragStarting(UIElement sender, DragStartingEventArgs args)
        {
            var p1 = args.GetPosition(this);
            var p2 = args.GetPosition(sender);
            var p3 = p1.X - p2.X / logicDpiScale;
            args.Data.Properties.Add("test", p3);
        }
        float logicDpiScale = Windows.Graphics.Display.DisplayInformation.GetForCurrentView().LogicalDpi / 96.0f;

        private void TimelineItem_GotFocus(object sender, RoutedEventArgs e)
        {

        }

        private void ItemsRepeater_DragOver(object sender, DragEventArgs e)
        {
            e.Handled = true;
            if (!(e.DataView.Properties.Values.FirstOrDefault() is double)) return;
            Point point = e.GetPosition(this);
            e.AcceptedOperation = DataPackageOperation.Move;
            e.DragUIOverride.Caption = string.Format("{0}", (int)((point.X - ((double)e.DataView.Properties["test"])) / RailLayout.Scaler));
        }

        private void Border_DragEnter(object sender, DragEventArgs e)
        {

            if (e.Data == null) return;
            e.Handled = true;
        }
    }

    class TimelineRailLayout : VirtualizingLayout
    {
        #region Layout parameters
        const int c_railHeight = 40;


        public double Scaler
        {
            get { return (double)GetValue(ScalerProperty); }
            set { SetValue(ScalerProperty, value); }
        }
        public static readonly DependencyProperty ScalerProperty =
            DependencyProperty.Register("Scaler", typeof(double), typeof(TimelineRailLayout), new PropertyMetadata(1.0, LayoutChange));

        private static void LayoutChange(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ((TimelineRailLayout)d).InvalidateMeasure();
        }


        #endregion

        #region Setup / teardown

        protected override void InitializeForContextCore(VirtualizingLayoutContext context)
        {
            base.InitializeForContextCore(context);

            var state = context.LayoutState as TimelineRailState;
            if (state == null)
            {
                context.LayoutState = new TimelineRailState();
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
            var state = context.LayoutState as TimelineRailState;
            var scaler = Scaler;
            state.LayoutRects.Clear();
            int firstColumnIndex = context.ItemCount;
            int lastColumnIndex = 0;
            int lastFrameIndex = 0;
            int xFrameIndex = (int)(context.RealizationRect.Left / scaler) - 1;
            int yFrameIndex = (int)(context.RealizationRect.Right / scaler) + 1;
            for (int i = 0; i < context.ItemCount; i++)
            {
                var data = (TimelineRailItem)context.GetItemAt(i);
                firstColumnIndex = i;
                if (data.StartFrameIndex + data.ContinueFramesCount > xFrameIndex) break;
            }
            for (int i = firstColumnIndex; i < context.ItemCount; i++)
            {
                var data = (TimelineRailItem)context.GetItemAt(i);
                lastColumnIndex = i + 1;//考虑不被执行的情况
                if (data.StartFrameIndex > yFrameIndex) break;
            }
            for (int i = firstColumnIndex; i < lastColumnIndex; i++)
            {
                var data = (TimelineRailItem)context.GetItemAt(i);
                var container = context.GetOrCreateElementAt(i);

                var rect = new Rect(data.StartFrameIndex * scaler, 0, data.ContinueFramesCount * scaler, c_railHeight);
                state.LayoutRects.Add(rect);
                container.Measure(new Size(rect.Width, c_railHeight));
            }

            for (int i = 0; i < context.ItemCount; i++)
            {
                var data = (TimelineRailItem)context.GetItemAt(i);
                lastFrameIndex = Math.Max(lastFrameIndex, data.StartFrameIndex + data.ContinueFramesCount);
            }
            state.FirstRealizedIndex = firstColumnIndex;
            return new Size(lastFrameIndex * scaler, c_railHeight);
        }

        protected override Size ArrangeOverride(VirtualizingLayoutContext context, Size finalSize)
        {
            // walk through the cache of containers and arrange
            var state = context.LayoutState as TimelineRailState;
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

    internal class TimelineRailState
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
