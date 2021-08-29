using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Storage;

namespace DirectCanvas.Util
{
    public enum CreateDocumentResourcesOption
    {
        Standard = 0,
        Plugin = 1
    }
    public class CreateDocumentParameters
    {
        public StorageFolder Folder;
        public int Width = 1024;
        public int Height = 1024;
        public string Name = "";
        public CreateDocumentResourcesOption CreateDocumentResourcesOption;
    }
}
