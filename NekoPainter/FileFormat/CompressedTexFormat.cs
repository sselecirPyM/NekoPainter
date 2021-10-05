using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NekoPainter.Core;
using System.IO;

namespace NekoPainter.FileFormat
{
    public static class CompressedTexFormat
    {
        static byte[] header = { 0x44, 0x43, 0x4c, 0x46 };
        public static void SaveToFile(this PictureLayout layout, LivedNekoPainterDocument document, FileInfo file)
        {
            var stream = file.OpenWrite();
            int count;
            if (document.LayoutTex.TryGetValue(layout.guid, out var tex0))
            {
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
                var bData = tex0.GetBlocksData();
                var oData = tex0.GetBlocksOffsetsData();
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

            TiledTexture tTex = new TiledTexture(bData, oData);
            document.LayoutTex[readedGuid] = tTex;
            return readedGuid;
        }
    }
}
