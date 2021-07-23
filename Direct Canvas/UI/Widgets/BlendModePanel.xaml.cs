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
    public sealed partial class BlendModePanel : UserControl
    {
        public BlendModePanel()
        {
            this.InitializeComponent();
        }

        CanvasCase canvasCase;

        public void SetCanvasCase(CanvasCase canvasCase)
        {
            this.canvasCase = canvasCase;
            BlendModeListView.ItemsSource = canvasCase.blendModes;
        }

        private void BlendModeListView_DragItemsStarting(object sender, DragItemsStartingEventArgs e)
        {
            Core.BlendMode c = (Core.BlendMode)e.Items[0];
            e.Data.Properties.Add("blendmode", c);
        }
    }
}
