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

// https://go.microsoft.com/fwlink/?LinkId=234238 上介绍了“空白页”项模板

namespace DirectCanvas.Pages
{
    /// <summary>
    /// 可用于自身或导航至 Frame 内部的空白页。
    /// </summary>
    public sealed partial class CreateDocumentPage : Page
    {
        public CreateDocumentPage()
        {
            this.InitializeComponent();
            folderPicker = new FolderPicker();
            folderPicker.FileTypeFilter.Add("*");
            folderPicker.SuggestedStartLocation = PickerLocationId.PicturesLibrary;
            folderPicker.ViewMode = PickerViewMode.Thumbnail;
        }
        public StorageFolder pickedDocumentLocation;
        FolderPicker folderPicker;

        public string path
        {
            get { return (string)GetValue(pathProperty); }
            set { SetValue(pathProperty, value); }
        }
        public static readonly DependencyProperty pathProperty =
            DependencyProperty.Register("path", typeof(string), typeof(CreateDocumentPage), new PropertyMetadata(""));

        public string DocumentName
        {
            get { return (string)GetValue(DocumentNameProperty); }
            set { SetValue(DocumentNameProperty, value); CheckCreateConditions(); }
        }
        public static readonly DependencyProperty DocumentNameProperty =
            DependencyProperty.Register("DocumentName", typeof(string), typeof(CreateDocumentPage), new PropertyMetadata(""));

        public int str_width
        {
            get { return (int)GetValue(str_widthProperty); }
            set
            {
                SetValue(str_widthProperty, value);
                CheckCreateConditions();
            }
        }
        public static readonly DependencyProperty str_widthProperty =
            DependencyProperty.Register("str_width", typeof(int), typeof(CreateDocumentPage), new PropertyMetadata(1024));

        public int str_height
        {
            get { return (int)GetValue(str_heightProperty); }
            set
            {
                SetValue(str_heightProperty, value);
                CheckCreateConditions();
            }
        }
        public static readonly DependencyProperty str_heightProperty =
            DependencyProperty.Register("str_height", typeof(int), typeof(CreateDocumentPage), new PropertyMetadata(1024));



        public bool CreateCondition
        {
            get { return (bool)GetValue(CreateConditionProperty); }
            set { SetValue(CreateConditionProperty, value); }
        }

        // Using a DependencyProperty as the backing store for CreateCondition.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty CreateConditionProperty =
            DependencyProperty.Register("CreateCondition", typeof(bool), typeof(CreateDocumentPage), new PropertyMetadata(false));



        private async void Button_Browse_Click(object sender, RoutedEventArgs e)
        {
            StorageFolder selectedFolder = await folderPicker.PickSingleFolderAsync();
            if (selectedFolder == null) return;
            pickedDocumentLocation = selectedFolder;
            path = pickedDocumentLocation.Path;
            CheckCreateConditions();
        }

        private void Button_Create_Click(object sender, RoutedEventArgs e)
        {
            Frame.Navigate(typeof(CreatingDocumentPage), new Util.CreateDocumentParameters()
            {
                Folder = pickedDocumentLocation,
                Width = str_width,
                Height = str_height,
                Name = DocumentName,
                bufferCount = int.Parse((string)ComboBox_RenderBufferNum.SelectionBoxItem),
                CreateDocumentResourcesOption = (Util.CreateDocumentResourcesOption)ComboBox_CreateDocumentResourcesOption.SelectedIndex
            });
        }

        private void Button_Cancel_Click(object sender, RoutedEventArgs e)
        {
            Frame.GoBack();
        }

        private void Button_Close_Click(object sender, RoutedEventArgs e)
        {
            Frame.Navigate(typeof(BlankPage));
        }


        private void CheckCreateConditions()
        {
            CreateCondition =
                pickedDocumentLocation != null &&
                !string.IsNullOrEmpty(DocumentName) &&
                str_width > 0 &&
                str_height > 0;
        }
    }
}
