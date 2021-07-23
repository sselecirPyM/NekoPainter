using DirectCanvas.Layout;
using DirectCanvas.UI.Controller;
using DirectCanvas.Util.Converter;
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

namespace DirectCanvas.UI.Widgets
{
    public sealed partial class LayoutInfoPanel : UserControl
    {
        public LayoutInfoPanel()
        {
            this.InitializeComponent();
            ParameterListView.ItemsSource = DCParameters;
        }

        CanvasCase canvasCase;

        public void SetCanvasCase(CanvasCase canvasCase)
        {
            this.canvasCase = canvasCase;

            title1.SetBinding(TextBlock.VisibilityProperty, new Binding() { Path = new PropertyPath("CurrentPictureLayout"), Source = this, Mode = BindingMode.OneWay, Converter = new IsPureLayout() });
            colorPicker1.SetBinding(ColorPicker.VisibilityProperty, new Binding() { Path = new PropertyPath("CurrentPictureLayout"), Source = this, Mode = BindingMode.OneWay, Converter = new IsPureLayout() });
            colorPicker1.SetBinding(ColorPicker.ColorProperty, new Binding() { Path = new PropertyPath("CurrentPictureLayout.Color"), Source = this, Mode = BindingMode.TwoWay, Converter = new Vector4ToColor() });
            dispatcherTimer.Interval = new TimeSpan(0, 0, 0, 0, 5);
            dispatcherTimer.Tick += DispatcherTimer_Tick;
            dispatcherTimer.Start();
        }

        public PictureLayout CurrentPictureLayout
        {
            get { return (PictureLayout)GetValue(CurrentPictureLayoutProperty); }
            set
            {
                SetValue(CurrentPictureLayoutProperty, value);
                if (value != null)
                {
                    //for (int i = 0; i < Core.BlendMode.c_parameterCount; i++)
                    //{
                    //    DCParameters[i] = value.Parameters[i];
                    //}
                }
                else
                {
                    //for (int i = 0; i < Core.BlendMode.c_parameterCount; i++)
                    //{
                    //    DCParameters[i] = null;
                    //}
                }
            }
        }
        public static readonly DependencyProperty CurrentPictureLayoutProperty =
            DependencyProperty.Register("CurrentPictureLayout", typeof(PictureLayout), typeof(LayoutInfoPanel), new PropertyMetadata(null));

        ObservableCollection<Core.DCParameter> DCParameters = new ObservableCollection<Core.DCParameter>(new Core.DCParameter[Core.BlendMode.c_parameterCount]);
        
        private void Grid_DragEnter(object sender, DragEventArgs e)
        {
            if (e.Data.Properties.Values.FirstOrDefault() is Core.BlendMode x && CurrentPictureLayout != null)
            {
                e.Handled = true;
                e.AcceptedOperation = DataPackageOperation.Link;
                e.DragUIOverride.Caption = CurrentPictureLayout.Name;
            }
        }

        private void Grid_DragLeave(object sender, DragEventArgs e)
        {
            if (e.Data.Properties.Values.FirstOrDefault() is Core.BlendMode x && CurrentPictureLayout != null)
            {
                e.Handled = true;
                e.AcceptedOperation = DataPackageOperation.None;
                e.DragUIOverride.Caption = "";
            }
        }

        private void Grid_Drop(object sender, DragEventArgs e)
        {
            if (e.Data.Properties.Values.FirstOrDefault() is Core.BlendMode x && CurrentPictureLayout != null)
            {
                e.Handled = true;
                AppController.Instance.CurrentCanvasCase.SetBlendMode(CurrentPictureLayout, x);
                AppController.Instance.CanvasRerender();

                //for (int i = 0; i < Core.BlendMode.c_parameterCount; i++)
                //{
                //    DCParameters[i] = CurrentPictureLayout.Parameters[i];
                //}
            }
        }

        private void TextChanged(object sender, TextChangedEventArgs e)
        {
            if (canvasCase == null) return;
            AppController.Instance.CanvasRerender();
        }
        DispatcherTimer dispatcherTimer = new DispatcherTimer();

        private void ColorPicker1_ColorChanged(ColorPicker sender, ColorChangedEventArgs args)
        {
            if (canvasCase == null) return;
            PresentTask = true;
        }
        bool PresentTask = false;
        private void DispatcherTimer_Tick(object sender, object e)
        {
            if (!PresentTask) return;
            PresentTask = false;
            AppController.Instance.CanvasRerender();
        }

        private void DCUIControl_Parameter_ValueChanged(object sender, EventArgs e)
        {
            CurrentPictureLayout.blendModeUsedDataUpdated = false;
            AppController.Instance.CanvasRerender();
        }
    }
}
