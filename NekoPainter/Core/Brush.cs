using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CanvasRendering;
using System.IO;
using System.Xml;
using NekoPainter.Util;
using System.Xml.Serialization;

namespace NekoPainter.Core
{
    public class Brush : IDisposable, IComparable<Brush>
    {
        public static Brush LoadFromFileAsync(FileInfo file)
        {
            Stream stream = file.OpenRead();


            var brush1 = (BrushSerialized)xmlSerializer.Deserialize(stream);

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
                InputInfo InputDatas[4];
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
            return brush;
        }

        static XmlSerializer xmlSerializer = new XmlSerializer(typeof(BrushSerialized));
        static string componentCode1;
        static string appUsedCultureName;

        public static void LoadStaticResourcesAsync()
        {
            appUsedCultureName = System.Threading.Thread.CurrentThread.CurrentUICulture.Name;
            componentCode1 = File.ReadAllText("Shaders\\brush_c1.hlsl");
        }

        private Brush(string generatedCode)
        {
            this.generatedCode = generatedCode;
        }

        public void CheckBrush(DeviceResources device)
        {
            if (shader == null)
            {
                shader = ComputeShader.CompileAndCreate(device, Encoding.UTF8.GetBytes(componentCode1.Replace("#define codehere", generatedCode)));
            }
        }

        public void Dispose()
        {
            shader.Dispose();
        }

        public int CompareTo(Brush other)
        {
            return Name.CompareTo(other.Name);
        }

        public string generatedCode;

        public ComputeShader shader;

        public string Image;

        public float Size { get; set; }

        public DCParameter[] Parameters;

        public string Name { get; set; }
        public string Description { get; set; }
        [XmlIgnore]
        public string path;
    }
    [XmlType("Brush")]
    public class BrushSerialized
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
