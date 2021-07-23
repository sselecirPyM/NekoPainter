using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using Windows.UI;
using Windows.UI.Xaml.Data;

namespace DirectCanvas.Util.Converter
{
    public class Vector4ToColor : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            Vector4 vector4 = (Vector4)value;
            return new Color()
            {
                R = (byte)MathF.Round(vector4.X * 255),
                G = (byte)MathF.Round(vector4.Y * 255),
                B = (byte)MathF.Round(vector4.Z * 255),
                A = (byte)MathF.Round(vector4.W * 255)
            };
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {

            return DCUtil.ToVector4((Color)value);
        }
    }
}
