using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Text;
using System.Threading.Tasks;
using DirectCanvas.Layout;
using Windows.Foundation;
using Windows.Storage;
using Windows.Storage.Streams;

namespace DirectCanvas.FileFormat
{
    public static class DirectCanvasLayoutFormat
    {
        static byte[] header = { 0x44, 0x43, 0x4c, 0x46 };//DCLF
        public static async Task SaveToFileAsync(this StandardLayout layout,CanvasCase canvasCase, StorageFile file)
        {
            var stream = await file.OpenAsync(FileAccessMode.ReadWrite);
            int count;
            TiledTexture tex0 = null;
            if (layout is StandardLayout standardLayout)
            {

                if (standardLayout.activated)
                {
                    tex0 = new TiledTexture(canvasCase.PaintingTexture);
                    count = tex0.tilesCount;
                }
                else if (standardLayout.tiledTexture != null)
                {
                    tex0 = new TiledTexture(standardLayout.tiledTexture);
                    count = tex0.tilesCount;
                }
                else
                {
                    count = 0;
                }
                DataWriter dataWriter = new DataWriter(stream);


                dataWriter.WriteBytes(header);
                dataWriter.WriteGuid(layout.guid);
                dataWriter.WriteInt32(count);

                if (count != 0)
                {
                    byte[] oData = new byte[count * 8];
                    byte[] bData = new byte[count * 1024];
                    tex0.BlocksData.GetData<byte>(bData);
                    tex0.BlocksOffsetsData.GetData<byte>(oData);
                    if (!standardLayout.activated) tex0.Dispose();//此时为临时创建的TiledTexture
                    dataWriter.WriteBytes(oData);
                    dataWriter.WriteBytes(bData);
                }
                await dataWriter.StoreAsync();
                layout.saved = true;
                dataWriter.Dispose();
                stream.Dispose();
            }
        }

        public static IAsyncOperation<StandardLayout> LoadFromFileAsync(CanvasCase canvasCase, StorageFile file)
        {
            var task1 = Task<StandardLayout>.Run(async () =>
               {
                   var stream = await file.OpenReadAsync();
                   DataReader reader = new DataReader(stream);
                   await reader.LoadAsync((uint)stream.Size);

                   reader.ReadInt32();//header
                   Guid readedGuid = reader.ReadGuid();
                   int count = reader.ReadInt32();
                   byte[] oData = new byte[count * 8];
                   byte[] bData = new byte[count * 1024];
                   reader.ReadBytes(oData);
                   reader.ReadBytes(bData);

                   TiledTexture tTex = new TiledTexture(canvasCase.DeviceResources, bData, oData);
                   StandardLayout loadedLayout = new StandardLayout(tTex);
                   loadedLayout.saved = true;
                   loadedLayout.guid = readedGuid;

                   return loadedLayout;
               });
            return task1.AsAsyncOperation();
        }
    }
}
