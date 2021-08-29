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
        public static async Task<Brush[]> LoadFromFileAsync(StorageFile file)
        {
            Stream stream = await file.OpenStreamForReadAsync();


            var brush1 = (BrushCode)xmlSerializer.Deserialize(stream);

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

            Brush brush = new Brush(fCode.ToString());
            brush.Name = brush1.Name;
            brush.Description = brush1.Description;
            brush.Size = brush1.BrushSize;
            brush.Parameters = brush1.Parameters;
            Brush[] brushes = new Brush[1] { brush };
            return brushes;
        }

        static XmlSerializer xmlSerializer = new XmlSerializer(typeof(BrushCode));
        static string componentCode1;
        static string appUsedCultureName;

        public static async Task LoadStaticResourcesAsync()
        {
            appUsedCultureName = System.Threading.Thread.CurrentThread.CurrentUICulture.Name;
            componentCode1 = await DCUtil.ReadStringAsync("Shaders\\brush_c1.hlsl");
        }

        private Brush(string generatedCode)
        {
            this.generatedCode = generatedCode;
        }

        public void CheckBrush(DeviceResources device)
        {
            if (cBegin == null)
            {
                cBegin = ComputeShader.CompileAndCreate(device, Encoding.UTF8.GetBytes(componentCode1.Replace("#define codehere", generatedCode)));
                cDoing = cBegin;
                cEnd = cBegin;
            }
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

        public string generatedCode;

        public ComputeShader cBegin;
        public ComputeShader cDoing;
        public ComputeShader cEnd;

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
