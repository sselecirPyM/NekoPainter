using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Storage;
using CanvasRendering;
using System.IO;
using System.Xml;
using Windows.ApplicationModel;
using Windows.Storage.Streams;
using DirectCanvas.Util;
using System.Xml.Serialization;

namespace DirectCanvas.Core
{
    public class Brush : IDisposable, IComparable<Brush>
    {
        private Brush(ComputeShader cBegin, ComputeShader cDoing, ComputeShader cEnd)
        {
            this.cBegin = cBegin;
            this.cDoing = cDoing;
            this.cEnd = cEnd;
        }

        static string componentCode1;
        static string appUsedCultureName;

        public static async Task LoadStaticResourcesAsync()
        {
            appUsedCultureName = System.Threading.Thread.CurrentThread.CurrentUICulture.Name;
            componentCode1 = await DCUtil.ReadStringAsync("Shaders\\brush_c1.hlsl");
        }

        public static async Task<Brush[]> LoadFromFileAsync(DeviceResources deviceResources, StorageFile file)
        {
            Stream stream = await file.OpenStreamForReadAsync();

            var brush1 = (BrushCode)xmlSerializer.Deserialize(stream);
            string x = componentCode1;
            ComputeShader begin = null;
            //ComputeShader drawing = null;
            //ComputeShader end = null;


            StringBuilder fCode = new StringBuilder();
            fCode.Append(@"
cbuffer BrushData: register(b0)
{
                float4 BrushColor;
                float4 BrushColor2;
                float4 BrushColor3;
                float4 BrushColor4;
                float BrushSize;
                int UseSelection;
                float2 BrushDataPreserved;
                InputInfo InputDatas[8];
");
            if (brush1.Parameters != null)
            {
                for (int i = 0; i < brush1.Parameters.Length; i++)
                {
                    fCode.Append("float P_");
                    fCode.Append(brush1.Parameters[i].Name);
                    fCode.Append(";\n");
                }
            }
            fCode.Append("}\n");
            fCode.Append(brush1.Code);

            begin = ComputeShader.CompileAndCreate(deviceResources, Encoding.UTF8.GetBytes(x.Replace("#define codehere", fCode.ToString())));

            //Parallel.Invoke(
            //     () => begin = ComputeShader.CompileAndCreate(deviceResources, Encoding.UTF8.GetBytes(x.Replace("#define codehere", code["Begin"]))),
            //     () => drawing = ComputeShader.CompileAndCreate(deviceResources, Encoding.UTF8.GetBytes(x.Replace("#define codehere", code["Doing"]))),
            //     () => end = ComputeShader.CompileAndCreate(deviceResources, Encoding.UTF8.GetBytes(x.Replace("#define codehere", code["End"])))
            //     );


            Brush brush = new Brush(begin, begin, begin);
            brush.Name = brush1.Name;
            brush.Description = brush1.Description;
            brush.Size = brush1.BrushSize;
            brush.Parameters = brush1.Parameters;
            Brush[] brushes = new Brush[1];
            brushes[0] = brush;
            return brushes;
        }

        public void Dispose()
        {
            cBegin.Dispose();
            cDoing.Dispose();
            cEnd.Dispose();
        }

        public int CompareTo(Brush other)
        {
            return Name.CompareTo(other.Name);
        }

        static XmlSerializer xmlSerializer = new XmlSerializer(typeof(BrushCode));

        public readonly ComputeShader cBegin;
        public readonly ComputeShader cDoing;
        public readonly ComputeShader cEnd;

        public string Image;

        public float Size { get; set; }

        public DCParameter[] Parameters;

        public string Name { get; set; }
        public string Description { get; set; }
    }
    [XmlType("Brush")]
    public class BrushCode
    {
        public string Name;
        public string Description;
        public Guid Guid;
        public string Code;
        public string Image;
        public float BrushSize = 40.0f;
        public DCParameter[] Parameters;
    }
}
