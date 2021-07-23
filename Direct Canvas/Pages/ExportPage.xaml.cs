using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Storage;
using Windows.Storage.Pickers;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using System.Runtime.InteropServices;

// https://go.microsoft.com/fwlink/?LinkId=234238 上介绍了“空白页”项模板

namespace DirectCanvas.Pages
{
    /// <summary>
    /// 可用于自身或导航至 Frame 内部的空白页。
    /// </summary>
    public sealed partial class ExportPage : Page
    {
        public ExportPage()
        {
            this.InitializeComponent();
            fileSavePicker.FileTypeChoices.Add("png图像", new string[] { ".png" });
            fileSavePicker.FileTypeChoices.Add("jpg图像", new string[] { ".jpg" });
        }

        FileSavePicker fileSavePicker = new FileSavePicker() { SuggestedStartLocation = PickerLocationId.PicturesLibrary };

        CanvasCase canvasCase = UI.Controller.AppController.Instance.CurrentCanvasCase;
        public bool ExportCondition
        {
            get { return (bool)GetValue(ExportConditionProperty); }
            set { SetValue(ExportConditionProperty, value); }
        }
        public static readonly DependencyProperty ExportConditionProperty =
            DependencyProperty.Register("ExportCondition", typeof(bool), typeof(ExportPage), new PropertyMetadata(false));



        public string SaveFilePath
        {
            get { return (string)GetValue(SaveFilePathProperty); }
            set { SetValue(SaveFilePathProperty, value); }
        }
        public static readonly DependencyProperty SaveFilePathProperty =
            DependencyProperty.Register("SaveFilePath", typeof(string), typeof(ExportPage), new PropertyMetadata(""));



        void ExportConditionCheck()
        {
            ExportCondition = saveFile != null;
        }

        StorageFile saveFile;
        private async void Button_Browse_Click(object sender, RoutedEventArgs e)
        {
            fileSavePicker.SuggestedFileName = canvasCase.Name;
            StorageFile storageFile = await fileSavePicker.PickSaveFileAsync();
            if (storageFile == null) return;
            saveFile = storageFile;
            SaveFilePath = saveFile.Path;
            ExportConditionCheck();
        }

        private void Button_Cancel_Click(object sender, RoutedEventArgs e)
        {
            Frame.GoBack();
        }

        private void Button_Close_Click(object sender, RoutedEventArgs e)
        {
            Frame.Navigate(typeof(BlankPage));
        }

        private async void Button_Export_Click(object sender, RoutedEventArgs e)
        {
            Frame.Navigate(typeof(BlankPage));
            await FileIO.WriteBytesAsync(saveFile, new byte[0]);
            var IRStream = await saveFile.OpenAsync(FileAccessMode.ReadWrite);
            var stream = IRStream.AsStream();

            byte[] imageData = canvasCase.RenderTarget[0].GetData(UI.Controller.AppController.Instance.computeShaders["CExport"]);
            int width = canvasCase.RenderTarget[0].width;
            int height = canvasCase.RenderTarget[0].height;
            var image = GetImage(width, height, imageData);

            switch (saveFile.FileType.ToLower())
            {
                case ".png":
                    image.SaveAsPng(stream);
                    break;
                case ".jpg":
                    image.SaveAsJpeg(stream);
                    break;
            }

            stream.Flush();
            stream.Close();
        }

        static Image<RgbaVector> GetImage(int width, int height, byte[] imageData)
        {
            Image<RgbaVector> image = new Image<RgbaVector>(width, height);
            image.Frames[0].TryGetSinglePixelSpan(out var span1);
            imageData.CopyTo(MemoryMarshal.Cast<RgbaVector, byte>(span1));
            return image;
        }
    }
}
