using DirectCanvas.Controller;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

// https://go.microsoft.com/fwlink/?LinkId=234238 上介绍了“空白页”项模板

namespace DirectCanvas.Pages
{
    /// <summary>
    /// 可用于自身或导航至 Frame 内部的空白页。
    /// </summary>
    public sealed partial class SettingsPage : Page
    {
        public SettingsPage()
        {
            this.InitializeComponent();
            BackgroundColorPicker.Color = Util.DCUtil.ToColor(AppController.Instance.currentAppSettings.BackGroundColor);
        }

        private void ColorPicker_ColorChanged(ColorPicker sender, ColorChangedEventArgs args)
        {
            AppController.Instance.currentAppSettings.BackGroundColor =Util.DCUtil.ToVector4(args.NewColor);
        }


        private void Button_Close_Click(object sender, RoutedEventArgs e)
        {
            Frame.Navigate(typeof(BlankPage));
        }
    }
}
