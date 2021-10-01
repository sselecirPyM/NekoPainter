using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NekoPainter.Util;
using NekoPainter.Controller;
using System.IO;
using System.Runtime.InteropServices;

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
        public static FileFormat.CreateDocumentParameters createDocumentParameters;
        public static bool saveDocument;

        public static bool openDocument;
        public static string openDocumentPath = "";

        public static string importImagePath = "";
        public static string exportImagePath = "";

        public static bool quit;

        public static void OnFrame()
        {
            if (selectFolder.SetFalse())
            {
                string path = OpenResourceFolder();
                if (!string.IsNullOrEmpty(path))
                    folder = new DirectoryInfo(path);
            }
            if (selectSaveFile.SetFalse())
            {
                FileOpenDialog fileDialog = new FileOpenDialog()
                {
                    //title = "select save file",
                    file = new string(new char[256]),
                    fileTitle = new string(new char[256]),
                    initialDir = AppController.Instance.CurrentDCDocument.Folder.FullName,
                    filter = ".jpg\0*.jpg\0.png\0*.png\0.tga\0*.tga\0All Files\0*.*\0\0",
                    defExt = "jpg",
                    flags = 0x00080000 | 0x00001000 | 0x00000800 | 0x00000200 | 0x00000008,
                    structSize = Marshal.SizeOf(typeof(FileOpenDialog))
                };
                fileDialog.maxFile = fileDialog.file.Length;
                fileDialog.maxFileTitle = fileDialog.fileTitle.Length;
                if (GetSaveFileName(fileDialog))
                {
                    saveFile = new FileInfo(fileDialog.file);
                }
            }
            if (selectOpenFile.SetFalse())
            {
                FileOpenDialog fileDialog = new FileOpenDialog()
                {
                    //title = "select save file",
                    file = new string(new char[256]),
                    fileTitle = new string(new char[256]),
                    initialDir = AppController.Instance.CurrentDCDocument.Folder.FullName,
                    filter = ".jpg\0*.jpg\0.png\0*.png\0.tga\0*.tga\0All Files\0*.*\0\0",
                    defExt = "jpg",
                    flags = 0x00080000 | 0x00001000 | 0x00000800 | 0x00000200 | 0x00000008,
                    structSize = Marshal.SizeOf(typeof(FileOpenDialog))
                };
                fileDialog.maxFile = fileDialog.file.Length;
                fileDialog.maxFileTitle = fileDialog.fileTitle.Length;
                if (GetOpenFileName(fileDialog))
                {
                    openFile = new FileInfo(fileDialog.file);
                }
            }
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
        [DllImport("Comdlg32.dll", SetLastError = true, ThrowOnUnmappableChar = true, CharSet = CharSet.Auto)]
        public static extern bool GetOpenFileName([In, Out] FileOpenDialog ofn);

        [DllImport("Comdlg32.dll", SetLastError = true, ThrowOnUnmappableChar = true, CharSet = CharSet.Auto)]
        public static extern bool GetSaveFileName([In, Out] FileOpenDialog ofn);

        [DllImport("shell32.dll", SetLastError = true, ThrowOnUnmappableChar = true, CharSet = CharSet.Auto)]
        public static extern IntPtr SHBrowseForFolder([In, Out] OpenDialogDir ofn);

        [DllImport("shell32.dll", SetLastError = true, ThrowOnUnmappableChar = true, CharSet = CharSet.Auto)]
        public static extern bool SHGetPathFromIDList([In] IntPtr pidl, [In, Out] char[] fileName);

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

        public static string OpenResourceFile(string filter)
        {
            FileOpenDialog dialog = new FileOpenDialog();
            dialog.structSize = Marshal.SizeOf(typeof(FileOpenDialog));
            dialog.filter = filter;
            dialog.file = new string(new char[2000]);
            dialog.maxFile = dialog.file.Length;

            dialog.initialDir = Environment.GetFolderPath(Environment.SpecialFolder.MyComputer);
            dialog.flags = 0x00000008;
            GetOpenFileName(dialog);
            var chars = dialog.file.ToCharArray();

            return new string(chars, 0, Array.IndexOf(chars, '\0'));
        }

        public static string OpenResourceFolder()
        {
            OpenDialogDir openDialogDir = new OpenDialogDir();
            openDialogDir.pszDisplayName = new string(new char[2000]);
            openDialogDir.lpszTitle = "Open Project";
            IntPtr pidlPtr = SHBrowseForFolder(openDialogDir);
            char[] charArray = new char[2000];
            Array.Fill(charArray, '\0');

            SHGetPathFromIDList(pidlPtr, charArray);
            int length = Array.IndexOf(charArray, '\0');
            string fullDirPath = new String(charArray, 0, length);

            return fullDirPath;
        }
    }
}

[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
public class FileOpenDialog
{
    public int structSize = 0;
    public IntPtr dlgOwner = IntPtr.Zero;
    public IntPtr instance = IntPtr.Zero;
    public String filter = null;
    public String customFilter = null;
    public int maxCustFilter = 0;
    public int filterIndex = 0;
    public String file = null;
    public int maxFile = 0;
    public String fileTitle = null;
    public int maxFileTitle = 0;
    public String initialDir = null;
    public String title = null;
    public int flags = 0;
    public short fileOffset = 0;
    public short fileExtension = 0;
    public String defExt = null;
    public IntPtr custData = IntPtr.Zero;
    public IntPtr hook = IntPtr.Zero;
    public String templateName = null;
    public IntPtr reservedPtr = IntPtr.Zero;
    public int reservedInt = 0;
    public int flagsEx = 0;
}
[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
public class OpenDialogDir
{
    public IntPtr hwndOwner = IntPtr.Zero;
    public IntPtr pidlRoot = IntPtr.Zero;
    public String pszDisplayName = null;
    public String lpszTitle = null;
    public UInt32 ulFlags = 0;
    public IntPtr lpfn = IntPtr.Zero;
    public IntPtr lParam = IntPtr.Zero;
    public int iImage = 0;
}
