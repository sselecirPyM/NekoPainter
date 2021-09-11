using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NekoPainter.Core;
using System.IO;

namespace NekoPainter.FileFormat
{
    public static class NekoPainterLayoutFormat
    {
        static byte[] header = { 0x44, 0x43, 0x4c, 0x46 };//DCLF
        public static void SaveToFile(this PictureLayout layout, LivedNekoPainterDocument document, FileInfo file)
        {
            var stream = file.OpenWrite();
            TiledTexture tex0 = null;
            int count;
            if (document.LayoutTex.TryGetValue(layout.guid, out var tiledTexture))
            {
                tex0 = new TiledTexture(tiledTexture);
                count = tex0.tilesCount;
            }
            else
            {
                count = 0;
            }
            BinaryWriter dataWriter = new BinaryWriter(stream);


            dataWriter.Write(header);
            dataWriter.Write(layout.guid.ToByteArray());
            dataWriter.Write(count);

            if (count != 0)
            {
                byte[] oData = new byte[count * 8];
                byte[] bData = new byte[count * 1024];
                tex0.BlocksData.GetData<byte>(bData);
                tex0.BlocksOffsetsData.GetData<byte>(oData);
                tex0.Dispose();//此时为临时创建的TiledTexture
                dataWriter.Write(oData);
                dataWriter.Write(bData);
            }
            dataWriter.Flush();
            layout.saved = true;
            dataWriter.Dispose();
            stream.Dispose();

        }

        public static Guid LoadFromFile(LivedNekoPainterDocument document, FileInfo file)
        {
            var stream = file.OpenRead();
            BinaryReader reader = new BinaryReader(stream);

            reader.ReadInt32();//header
            Guid readedGuid = new Guid(reader.ReadBytes(16));
            int count = reader.ReadInt32();
            byte[] oData = reader.ReadBytes(count * 8);
            byte[] bData = reader.ReadBytes(count * 1024);

            TiledTexture tTex = new TiledTexture(document.DeviceResources, bData, oData);
            document.LayoutTex[readedGuid] = tTex;
            return readedGuid;
        }
    }
}
