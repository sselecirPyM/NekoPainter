using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Numerics;
using Windows.UI;
using Windows.ApplicationModel;
using Windows.Storage;
using Windows.Storage.Streams;

namespace DirectCanvas.Util
{
    public class DCUtil
    {
        public static Color ToColor(Vector4 color)
        {
            return new Color()
            {
                R = (byte)MathF.Round(color.X * 255),
                G = (byte)MathF.Round(color.Y * 255),
                B = (byte)MathF.Round(color.Z * 255),
                A = (byte)MathF.Round(color.W * 255)
            };
        }

        public static Vector4 ToVector4(Color color)
        {
            return new Vector4(color.R / 255.0f, color.G / 255.0f, color.B / 255.0f, color.A / 255.0f);
        }

        public static async Task<string> ReadStringAsync(string localPath)
        {
            StorageFile openFile = await Package.Current.InstalledLocation.GetFileAsync(localPath);
            IRandomAccessStream openStream = await openFile.OpenAsync(FileAccessMode.Read);
            DataReader dataReader = new DataReader(openStream);
            await dataReader.LoadAsync((uint)openStream.Size);
            return dataReader.ReadString((uint)openStream.Size);
        }
    }
}
