using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Windows.Input;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using CanvasRendering;
using Windows.Storage.Pickers;
using Windows.Storage;
using Windows.UI.ViewManagement;
using DirectCanvas.Controller;
using Windows.UI.Xaml.Media.Imaging;
using Windows.UI.Core;
using Windows.System;
using Windows.Storage.Streams;
using Windows.UI.WindowManagement;
using Windows.UI.Xaml.Hosting;
using Vortice.DXGI;

namespace DirectCanvas
{
    /// <summary>
    /// 可用于自身或导航至 Frame 内部的空白页。
    /// </summary>
    public sealed partial class MainPage : Page
    {
        public MainPage()
        {
            this.InitializeComponent();
            currentController = AppController.Instance;
            currentController.mainPage = this;
            folderPicker.FileTypeFilter.Add("*");
            _MenuItem_New.Command = currentController.command_New;
            currentController.command_New.executeAction += ShowCreateDocumentPage;
            _MenuItem_Open.Command = currentController.command_Open;
            currentController.command_Open.executeAction += OpenDocumentAsync;
            _MenuItem_Save.Command = currentController.command_Save;
            currentController.command_Save.executeAction += SaveDocumentAsync;
            _MenuItem_Import.Command = currentController.command_Import;
            currentController.command_Import.executeAction += ImportDocumentAsync;
            _MenuItem_Export.Command = currentController.command_Export;
            currentController.command_Export.executeAction += ExportDocument;
            _MenuItem_Undo.Command = currentController.command_Undo;
            currentController.command_Undo.executeAction += AppController.Instance.CanvasRerender;
            _MenuItem_Redo.Command = currentController.command_Redo;
            currentController.command_Redo.executeAction += AppController.Instance.CanvasRerender;

            _MenuItem_ResetCanvasPosition.Command = currentController.command_ResetCanvasPosition;

            dcRenderView = currentController.graphicsContext;
            frame.Navigate(typeof(Pages.BlankPage));
        }
        AppController currentController;

        private void Panel_GotFocus(object sender, RoutedEventArgs e)
        {
            Canvas.SetZIndex(sender as UIElement, controlZIndexMax);
            controlZIndexMax++;
        }

        private void FLayoutPanel_PointerPressed(object sender, PointerRoutedEventArgs e)
        {
            Canvas.SetZIndex(sender as UIElement, controlZIndexMax);
            controlZIndexMax++;
        }

        public CSRect canvasview;

        GraphicsContext dcRenderView;

        private void ShowCreateDocumentPage()
        {
            frame.Navigate(typeof(Pages.CreateDocumentPage));
        }

        public readonly FolderPicker folderPicker = new FolderPicker() { SuggestedStartLocation = PickerLocationId.PicturesLibrary, ViewMode = PickerViewMode.Thumbnail };
        private async void OpenDocumentAsync()
        {
            StorageFolder selectedFolder = await folderPicker.PickSingleFolderAsync();
            if (selectedFolder == null) return;
            frame.Navigate(typeof(Pages.OpeningDocumentPage), new Util.CreateDocumentParameters() { Folder = selectedFolder });
        }

        private async void SaveDocumentAsync()
        {
            SavingDisplay.Visibility = Visibility.Visible;
            await currentController.CMDSaveDocument();
            SavingDisplay.Visibility = Visibility.Collapsed;
        }

        private void ExportDocument()
        {
            frame.Navigate(typeof(Pages.ExportPage));
        }

        private async void ImportDocumentAsync()
        {
            await currentController.CMDImportDocument();
        }

        private async void ExitApp(object sender, RoutedEventArgs e)
        {
            if (currentController.CurrentCanvasCase != null)
            {
                ContentDialog contentDialog = new ContentDialog()
                {
                    Content = "在退出之前保存文档？",
                    CloseButtonText = "取消",
                    PrimaryButtonText = "保存",
                    SecondaryButtonText = "不保存"
                };
                var result = await contentDialog.ShowAsync();
                if (result == ContentDialogResult.None)
                    return;
                else if (result == ContentDialogResult.Primary)
                {
                    await currentController.CMDSaveDocument();
                }
            }
            Application.Current.Exit();
        }

        private void _FullScreenButton_Click(object sender, RoutedEventArgs e)
        {
            ApplicationView view = ApplicationView.GetForCurrentView();
            if (view.IsFullScreenMode)
            {
                view.ExitFullScreenMode();
            }
            else
            {
                view.TryEnterFullScreenMode();
            }
        }

        int controlZIndexMax = 100;

        public async System.Threading.Tasks.Task AfterOpen()
        {
            CanvasCase canvasCase = currentController.CurrentCanvasCase;

            DocumentTitle.SetBinding(TextBlock.TextProperty, new Binding() { Path = new PropertyPath("Name"), Source = canvasCase, Mode = BindingMode.TwoWay });
            DCUI_Canvas.SetCanvasCase(canvasCase);
            //LayoutsPanel.SetCanvasCase(canvasCase);
            //BrushPanel.SetCanvasCase(canvasCase);
            //ColorAndOtherPanel.SetCanvasCase(canvasCase);
            //BlendModePanel.SetCanvasCase(canvasCase);
            //LayoutInfoPanel.SetCanvasCase(canvasCase);
            //foreach (Core.Brush brush in canvasCase.PaintAgent.brushes)
            //{
            //    if (!string.IsNullOrEmpty(brush.ImagePath))
            //    {
            //        try
            //        {
            //            StorageFile f = await currentController.CurrentDCDocument.brushesFolder.GetFileAsync(brush.ImagePath);
            //            BitmapImage image = new BitmapImage();
            //            image.SetSource(await f.OpenReadAsync());
            //            ImageBrush imageBrush = new ImageBrush { ImageSource = image };
            //            brush.UIBrush = imageBrush;
            //        }
            //        catch { brush.UIBrush = new SolidColorBrush(Windows.UI.Color.FromArgb(0xFF, 0xC0, 0xC0, 0xC0)); }
            //    }
            //    else
            //    {
            //        brush.UIBrush = new SolidColorBrush(Windows.UI.Color.FromArgb(0xFF, 0xC0, 0xC0, 0xC0));
            //    }
            //    for (int i = 0; i < Core.Brush.c_refTextureCount; i++)
            //        if (!string.IsNullOrEmpty(brush.RefTexturePath[i]))
            //        {
            //            try
            //            {
            //                StorageFile f = await currentController.CurrentDCDocument.brushesFolder.GetFileAsync(brush.RefTexturePath[i]);
            //                var stream = await f.OpenStreamForReadAsync();
            //                byte[] data = AppController.GetImageData(stream, out int width, out int height);
            //                RenderTexture renderTexture = new RenderTexture(canvasCase.DeviceResources, width, height, Format.R32G32B32A32_Float, false);
            //                renderTexture.ReadImageData1(data, width, height, currentController.computeShaders["CImport"]);
            //                brush.refTexture[i] = renderTexture;
            //            }
            //            catch { }
            //        }
            //}
        }

        private void _MenuItem_About_Click(object sender, RoutedEventArgs e)
        {
            frame.Navigate(typeof(Pages.AboutSoftwarePage));
        }

        private void _MenuItem_HelpHelp_Click(object sender, RoutedEventArgs e)
        {

        }

        private void _MenuItem_Settings_Click(object sender, RoutedEventArgs e)
        {
            frame.Navigate(typeof(Pages.SettingsPage));
        }

        private async void _MenuItem_ReferencePicture_Click(object sender, RoutedEventArgs e)
        {
            FileOpenPicker fileOpenPicker1 = new FileOpenPicker();
            fileOpenPicker1.FileTypeFilter.Add(".bmp");
            fileOpenPicker1.FileTypeFilter.Add(".jpg");
            fileOpenPicker1.FileTypeFilter.Add(".jpeg");
            fileOpenPicker1.FileTypeFilter.Add(".png");
            fileOpenPicker1.FileTypeFilter.Add(".gif");
            fileOpenPicker1.FileTypeFilter.Add(".tif");
            StorageFile file = await fileOpenPicker1.PickSingleFileAsync();
            if (file == null) return;
            AppWindow referencePicture = await AppWindow.TryCreateAsync();
            Image imageControl = new Image();
            var img1 = new BitmapImage();
            try
            {
                await img1.SetSourceAsync(await file.OpenReadAsync());
                imageControl.Source = img1;
                ElementCompositionPreview.SetAppWindowContent(referencePicture, imageControl);
                if (referencePicture.Presenter.IsPresentationSupported(AppWindowPresentationKind.CompactOverlay))
                {
                    referencePicture.Title = "参考图片";
                    if (referencePicture.Presenter.RequestPresentation(AppWindowPresentationKind.CompactOverlay))
                    {
                        referencePicture.RequestSize(new Size(img1.PixelWidth, img1.PixelHeight));
                        await referencePicture.TryShowAsync();
                        img1.Play();
                    }
                }
            }
            catch
            {

            }
        }

        private void Page_KeyDown(object sender, KeyRoutedEventArgs e)
        {
            var ctrlState = CoreWindow.GetForCurrentThread().GetKeyState(VirtualKey.Control);
            var isCtrlPressed = ctrlState.HasFlag(CoreVirtualKeyStates.Down) || ctrlState.HasFlag(CoreVirtualKeyStates.Locked);
            if (e.Key == VirtualKey.S && isCtrlPressed)
            {
                e.Handled = true;
                if (currentController.command_Save.CanExecute(this)) currentController.command_Save.Execute(this);
            }
            else if (e.Key == VirtualKey.Z && isCtrlPressed)
            {
                e.Handled = true;
                if (currentController.command_Undo.CanExecute(this)) currentController.command_Undo.Execute(this);
            }
            else if (e.Key == VirtualKey.Y && isCtrlPressed)
            {
                e.Handled = true;
                if (currentController.command_Redo.CanExecute(this)) currentController.command_Redo.Execute(this);
            }
        }
    }
}
