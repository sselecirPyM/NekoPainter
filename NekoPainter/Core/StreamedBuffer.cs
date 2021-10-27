using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CanvasRendering;
using System.IO;
using NekoPainter.Core.Util;

namespace NekoPainter.Core
{
    public class StreamedBuffer : IDisposable
    {
        public ConstantBuffer buffer;

        public ComputeBuffer buffer1;

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

        public ComputeBuffer GetComputeBuffer(DeviceResources device, int stride)
        {
            if (buffer1 == null || buffer1.size != memoryStream.GetBuffer().Length || buffer1.stride != stride)
            {
                buffer1?.Dispose();
                buffer1 = new ComputeBuffer(device, memoryStream.GetBuffer().Length, stride);
            }
            buffer1.SetData<byte>(memoryStream.GetBuffer());
            return buffer1;
        }
        public void Dispose()
        {
            buffer1?.Dispose();
            buffer?.Dispose();
        }
    }
}
