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
        public int Width;
        public int Height;
        public int bufferCount;
        public string Name;
        public CreateDocumentResourcesOption CreateDocumentResourcesOption;
    }
}
