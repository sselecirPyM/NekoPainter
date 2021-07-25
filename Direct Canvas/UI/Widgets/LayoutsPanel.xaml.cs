using DirectCanvas.Layout;
using DirectCanvas.UI.Controller;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
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
using Windows.ApplicationModel.DataTransfer;

//https://go.microsoft.com/fwlink/?LinkId=234236 上介绍了“用户控件”项模板

namespace DirectCanvas.UI.Widgets
{
    public sealed partial class LayoutsPanel : UserControl
    {
        public LayoutsPanel()
        {
            this.InitializeComponent();
            LayoutsListView.SelectionChanged += LayoutsListView_SelectionChanged;
        }

        private void LayoutsListView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (e.AddedItems.Count != 0 && e.AddedItems[0] is StandardLayout selectedLayout)
            {
                lock (canvasCase)
                {
                    canvasCase.SetActivatedLayout(selectedLayout);
                }
            }
        }

        public PictureLayout SelectedPictureLayout
        {
            get { return (PictureLayout)GetValue(SelectedPictureLayoutProperty); }
            set { SetValue(SelectedPictureLayoutProperty, value); canvasCase.SelectedLayout = (PictureLayout)GetValue(SelectedPictureLayoutProperty); }
        }

        // Using a DependencyProperty as the backing store for SelectedPictureLayout.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty SelectedPictureLayoutProperty =
            DependencyProperty.Register("SelectedPictureLayout", typeof(PictureLayout), typeof(LayoutsPanel), new PropertyMetadata(null));


        CanvasCase canvasCase;

        public void SetCanvasCase(CanvasCase canvasCase)
        {
            this.canvasCase = canvasCase;
            LayoutsListView.ItemsSource = canvasCase.Layouts;
        }

        private void NewLayout(object sender, RoutedEventArgs e)
        {
            if (LayoutsListView.SelectedIndex != -1)
            {
                canvasCase.NewStandardLayout(LayoutsListView.SelectedIndex, 0);
            }
            else if (canvasCase != null)
            {
                canvasCase.NewStandardLayout(0, 0);
            }
        }
        //private void NewPureLayout(object sender, RoutedEventArgs e)
        //{
        //    if (LayoutsListView.SelectedIndex != -1)
        //    {
        //        canvasCase.NewPureLayout(LayoutsListView.SelectedIndex, 0);
        //    }
        //    else if (canvasCase != null)
        //    {
        //        canvasCase.NewPureLayout(0, 0);
        //    }
        //}
        private void CopyLayout(object sender, RoutedEventArgs e)
        {
            if (LayoutsListView.SelectedIndex != -1)
            {
                canvasCase.CopyLayout(LayoutsListView.SelectedIndex);
                AppController.Instance.CanvasRerender();
            }
            else if (canvasCase != null)
            {
            }
        }
        //private void CopyBuffer0(object sender, RoutedEventArgs e)
        //{
        //    if (LayoutsListView.SelectedIndex != -1)
        //    {
        //        canvasCase.CopyBuffer(LayoutsListView.SelectedIndex, 0);
        //        AppController.Instance.CanvasRerender();
        //    }
        //    else if (canvasCase != null)
        //    {
        //        canvasCase.CopyBuffer(0, 0);
        //        AppController.Instance.CanvasRerender();
        //    }
        //}
        private void DeleteLayout(object sender, RoutedEventArgs e)
        {
            if (LayoutsListView.SelectedIndex != -1)
            {
                canvasCase.DeleteLayout(LayoutsListView.SelectedIndex);
                AppController.Instance.CanvasRerender();
            }
        }

        private void ShowFlyoutMenu(object sender, RoutedEventArgs e)
        {
            (sender as UIElement).ContextFlyout.ShowAt(sender as FrameworkElement);
        }
    }

    class ExplorerItemTemplateSelector : DataTemplateSelector
    {
        public DataTemplate StandardLayoutTemplate { get; set; }
        public DataTemplate PureLayoutTemplate { get; set; }

        protected override DataTemplate SelectTemplateCore(object item)
        {
            if (item.GetType() == typeof(StandardLayout))
            {
                return StandardLayoutTemplate;
            }
            //else if (item.GetType() == typeof(PureLayout))
            //{
            //    return PureLayoutTemplate;
            //}
            throw new NotImplementedException();
        }
    }
}
