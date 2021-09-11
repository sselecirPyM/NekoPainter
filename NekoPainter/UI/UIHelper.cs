using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NekoPainter.Util;
using NekoPainter.Controller;
using System.IO;

namespace NekoPainter.UI
{
    public static class UIHelper
    {
        public static bool selectFolder;
        public static DirectoryInfo folder;
        public static bool selectOpenFile;
        public static FileInfo openFile;
        public static bool selectSaveFile;
        public static FileInfo saveFile;

        public static bool createDocument;
        public static bool loadDocument;
        public static Util.CreateDocumentParameters createDocumentParameters;
        public static bool saveDocument;

        public static bool openDocument;
        public static string openDocumentPath = "";

        public static bool quit;

        public static void OnFrame()
        {
            //if (selectFolder.SetFalse())
            //{
            //    folder = await OpenResourceFolder();
            //}
            //if (selectSaveFile.SetFalse())
            //{
            //    var picker = new FileSavePicker()
            //    {
            //        SuggestedStartLocation = PickerLocationId.ComputerFolder,
            //    };
            //    picker.FileTypeChoices.Add("jpg", new[] { ".jpg" });
            //    picker.FileTypeChoices.Add("png", new[] { ".png" });

            //    saveFile = await picker.PickSaveFileAsync();
            //}
            //if (selectOpenFile.SetFalse())
            //{
            //    var picker = new FileOpenPicker()
            //    {
            //        SuggestedStartLocation = PickerLocationId.ComputerFolder,
            //    };
            //    picker.FileTypeFilter.Add(".jpg");
            //    picker.FileTypeFilter.Add(".png");
            //    picker.FileTypeFilter.Add(".tif");
            //    picker.FileTypeFilter.Add(".tga");

            //    openFile = await picker.PickSingleFileAsync();
            //}
            if (createDocument.SetFalse())
            {
                AppController.Instance.CreateDocument(createDocumentParameters);
            }
            if (openDocument.SetFalse())
            {
                AppController.Instance.OpenDocument(openDocumentPath);
            }
            if (saveDocument.SetFalse())
            {
                AppController.Instance.CurrentDCDocument.Save();
            }
        }

        //public static async Task<StorageFolder> OpenFolder(string identifier)
        //{
        //    FolderPicker folderPicker = new FolderPicker()
        //    {
        //        FileTypeFilter =
        //        {
        //            "*"
        //        },
        //        SuggestedStartLocation = PickerLocationId.ComputerFolder,
        //        ViewMode = PickerViewMode.Thumbnail,
        //        SettingsIdentifier = identifier,
        //    };
        //    StorageFolder folder = await folderPicker.PickSingleFolderAsync();
        //    return folder;
        //}

        //public static async Task<StorageFolder> OpenResourceFolder()
        //{
        //    FolderPicker folderPicker = new FolderPicker()
        //    {
        //        FileTypeFilter =
        //        {
        //            "*"
        //        },
        //        SuggestedStartLocation = PickerLocationId.ComputerFolder,
        //        ViewMode = PickerViewMode.Thumbnail,
        //        SettingsIdentifier = "ResourceFolder",
        //    };
        //    StorageFolder folder = await folderPicker.PickSingleFolderAsync();
        //    if (folder == null) return null;
        //    return folder;
        //}
    }
}
