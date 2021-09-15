using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CanvasRendering;
using NekoPainter.Util;
using System.IO;

namespace NekoPainter.Core
{
    public class StreamedConstantBuffer : IDisposable
    {
        public ConstantBuffer buffer;

        public DeviceResources deviceResources;

        public MemoryStream memoryStream;

        public BinaryWriterPlus writer;

        public BinaryWriterPlus Begin()
        {
            if (writer == null)
            {
                memoryStream = new MemoryStream();
                writer = new BinaryWriterPlus(memoryStream);
            }
            writer.Seek(0, SeekOrigin.Begin);
            return writer;
        }

        public ConstantBuffer GetBuffer(DeviceResources device)
        {
            if (buffer == null || buffer.size != memoryStream.GetBuffer().Length)
            {
                buffer?.Dispose();
                buffer = new ConstantBuffer(device, memoryStream.GetBuffer().Length);
            }
            buffer.UpdateResource<byte>(memoryStream.GetBuffer());
            return buffer;
        }
        public void Dispose()
        {
            buffer?.Dispose();
        }
    }
}
