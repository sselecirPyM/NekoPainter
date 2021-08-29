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
            AppController appController = new AppController();
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
            currentController.command_Undo.executeAction += AppController.Instance.CanvasRender;
            _MenuItem_Redo.Command = currentController.command_Redo;
            currentController.command_Redo.executeAction += AppController.Instance.CanvasRender;

            //_MenuItem_ResetCanvasPosition.Command = currentController.command_ResetCanvasPosition;

            dcRenderView = currentController.graphicsContext;
            frame.Navigate(typeof(Pages.BlankPage));
        }
        AppController currentController;

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
            //frame.Navigate(typeof(Pages.OpeningDocumentPage), new Util.CreateDocumentParameters() { Folder = selectedFolder });

            await AppController.Instance.OpenDocument(selectedFolder);
            AppController.Instance.mainPage.AfterOpen();
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

        public void AfterOpen()
        {
            CanvasCase canvasCase = currentController.CurrentCanvasCase;

            DCUI_Canvas.SetCanvasCase(canvasCase);
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
