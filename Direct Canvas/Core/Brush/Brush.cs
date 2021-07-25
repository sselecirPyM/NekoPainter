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
            for (int i = 0; i < c_parameterCount; i++)
            {
                Parameters[i] = new DCParameter();
            }
        }

        static string componentCode1;
        static string appUsedCultureName;

        static bool CultureCheck1(CreateBrushHelper createBrushHelper, string culture)
        {
            if (culture == null) culture = "";
            if (createBrushHelper.Culture == null) createBrushHelper.Culture = culture;
            bool isCurrentCulture = appUsedCultureName.Equals(culture, StringComparison.CurrentCultureIgnoreCase);
            bool inCurrentCulture = appUsedCultureName.Equals(createBrushHelper.Culture, StringComparison.CurrentCultureIgnoreCase);
            bool isSubstitute = culture.Equals(createBrushHelper.Culture, StringComparison.CurrentCultureIgnoreCase);
            if (isCurrentCulture ||
                isSubstitute ||
                (string.IsNullOrEmpty(culture) && !inCurrentCulture))
            {
                createBrushHelper.Culture = culture;
                return true;
            }
            else return false;
        }
        static bool CultureCheck2(DCParameter parameter, string culture)
        {
            if (culture == null) culture = "";
            if (parameter.Culture == null) parameter.Culture = culture;
            bool isCurrentCulture = appUsedCultureName.Equals(culture, StringComparison.CurrentCultureIgnoreCase);
            bool inCurrentCulture = appUsedCultureName.Equals(parameter.Culture, StringComparison.CurrentCultureIgnoreCase);
            bool isSubstitute = culture.Equals(parameter.Culture, StringComparison.CurrentCultureIgnoreCase);
            if (isCurrentCulture ||
                isSubstitute ||
                (string.IsNullOrEmpty(culture) && !inCurrentCulture))
            {
                parameter.Culture = culture;
                return true;
            }
            else return false;
        }

        public static async Task LoadStaticResourcesAsync()
        {
            appUsedCultureName = System.Threading.Thread.CurrentThread.CurrentUICulture.Name;
            componentCode1 = await DCUtil.ReadStringAsync("Shaders\\brush_c1.hlsl");
        }

        public static async Task<Brush[]> LoadFromFileAsync(DeviceResources deviceResources, StorageFile file)
        {
            Stream stream = await file.OpenStreamForReadAsync();
            XmlReaderSettings setting1 = new XmlReaderSettings();
            setting1.IgnoreComments = true;
            XmlReader xmlReader = XmlReader.Create(stream, setting1);
            Dictionary<string, string> code = new Dictionary<string, string>();
            List<CreateBrushHelper> createBrushHelpers = new List<CreateBrushHelper>();
            List<DCParameter> dcBrushParameters = new List<DCParameter>();
            bool isSelectionBrush = false;
            for (int i = 0; i < c_parameterCount; i++)
            {
                dcBrushParameters.Add(new DCParameter() { Name = string.Format("参数{0}", i + 1) });
            }
            while (xmlReader.Read())
            {
                if (xmlReader.NodeType == XmlNodeType.Element)
                {
                    switch (xmlReader.Name)
                    {
                        case "Brush":
                            bool.TryParse(xmlReader.GetAttribute("SelectionBrush"), out isSelectionBrush);
                            break;
                        case "Code":
                            string stage = xmlReader.GetAttribute("Stage");
                            code[stage] = xmlReader.ReadElementContentAsString();
                            break;
                        case "Parameter":
                            int.TryParse(xmlReader.GetAttribute("Index"), out int index);
                            dcBrushParameters[index].Type = xmlReader.GetAttribute("Type");
                            if (string.IsNullOrEmpty(dcBrushParameters[index].Type))
                                dcBrushParameters[index].Type = "TextBox";
                            if (int.TryParse(xmlReader.GetAttribute("MaxValue"), out int maxValue)) dcBrushParameters[index].MaxValue = maxValue;
                            else dcBrushParameters[index].MaxValue = int.MaxValue;
                            if (int.TryParse(xmlReader.GetAttribute("MinValue"), out int minValue)) dcBrushParameters[index].MinValue = minValue;
                            else dcBrushParameters[index].MinValue = int.MinValue;
                            while (xmlReader.Read())
                            {
                                if (xmlReader.NodeType == XmlNodeType.Element)
                                {
                                    switch (xmlReader.Name)
                                    {
                                        case "Name":
                                            if (CultureCheck2(dcBrushParameters[index], xmlReader.GetAttribute("Culture")))
                                            {
                                                dcBrushParameters[index].Name = xmlReader.ReadElementContentAsString();
                                            }
                                            break;
                                        case "Description":
                                            if (CultureCheck2(dcBrushParameters[index], xmlReader.GetAttribute("Culture")))
                                            {
                                                dcBrushParameters[index].Description = xmlReader.ReadElementContentAsString();
                                            }
                                            break;
                                    }
                                }
                                else if (xmlReader.NodeType == XmlNodeType.EndElement && xmlReader.Name == "Parameter")
                                    break;
                            }
                            break;
                        case "ParametersGroup":
                            CreateBrushHelper createBrushHelper = new CreateBrushHelper();
                            createBrushHelper.ImagePath = xmlReader.GetAttribute("Image");
                            for (int i = 0; i < c_refTextureCount; i++)
                                createBrushHelper.refTexturePath[i] = xmlReader.GetAttribute(string.Format("RefTexture{0}", i + 1));
                            if (!float.TryParse(xmlReader.GetAttribute("DefaultSize"), out createBrushHelper.BrushSize)) createBrushHelper.BrushSize = 40.0f;
                            for (int i = 0; i < c_parameterCount; i++)
                            {
                                int.TryParse(xmlReader.GetAttribute(string.Format("Parameter{0}", i + 1)), out createBrushHelper.parametersValue[i]);
                            }
                            while (xmlReader.Read())
                            {
                                if (xmlReader.NodeType == XmlNodeType.Element)
                                {
                                    switch (xmlReader.Name)
                                    {
                                        case "Name":
                                            if (CultureCheck1(createBrushHelper, xmlReader.GetAttribute("Culture")))
                                            {
                                                createBrushHelper.Name = xmlReader.ReadElementContentAsString();
                                            }
                                            continue;
                                        case "Description":
                                            if (CultureCheck1(createBrushHelper, xmlReader.GetAttribute("Culture")))
                                            {
                                                createBrushHelper.Description = xmlReader.ReadElementContentAsString();
                                            }
                                            continue;
                                    }
                                    xmlReader.Skip();
                                }
                                else if (xmlReader.NodeType == XmlNodeType.EndElement && xmlReader.Name == "ParametersGroup")
                                    break;
                            }
                            createBrushHelpers.Add(createBrushHelper);
                            break;
                    }
                }
            }
            string x = componentCode1;
            ComputeShader begin = null;
            ComputeShader drawing = null;
            ComputeShader end = null;

            Parallel.Invoke(
                () => begin = ComputeShader.CompileAndCreate(deviceResources, Encoding.UTF8.GetBytes(x.Replace("#define codehere", code["Begin"]))),
                () => drawing = ComputeShader.CompileAndCreate(deviceResources, Encoding.UTF8.GetBytes(x.Replace("#define codehere", code["Doing"]))),
                () => end = ComputeShader.CompileAndCreate(deviceResources, Encoding.UTF8.GetBytes(x.Replace("#define codehere", code["End"])))
                );


            Brush[] brushes = new Brush[createBrushHelpers.Count];
            for (int i = 0; i < createBrushHelpers.Count; i++)
            {
                Brush brush = new Brush(begin, drawing, end);
                brush.Name = createBrushHelpers[i].Name;
                brush.Description = createBrushHelpers[i].Description;
                brush.ImagePath = createBrushHelpers[i].ImagePath;
                brush.Size = createBrushHelpers[i].BrushSize;
                for (int j = 0; j < c_refTextureCount; j++)
                    brush.RefTexturePath[j] = createBrushHelpers[i].refTexturePath[j];
                for (int j = 0; j < c_parameterCount; j++)
                {
                    brush.Parameters[j].Name = dcBrushParameters[j].Name;
                    brush.Parameters[j].Description = dcBrushParameters[j].Description;
                    brush.Parameters[j].Type = dcBrushParameters[j].Type;
                    brush.Parameters[j].MaxValue = dcBrushParameters[j].MaxValue;
                    brush.Parameters[j].MinValue = dcBrushParameters[j].MinValue;
                    brush.Parameters[j].Value = createBrushHelpers[i].parametersValue[j];
                }
                brushes[i] = brush;
            }
            return brushes;
        }

        public async Task SaveToFileAsync(StorageFile file)
        {

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

        public readonly ComputeShader cBegin;
        public readonly ComputeShader cDoing;
        public readonly ComputeShader cEnd;

        public string ImagePath;
        public string[] RefTexturePath = new string[c_refTextureCount];

        public float Size { get; set; }

        public DCParameter[] Parameters = new DCParameter[c_parameterCount];

        public RenderTexture[] refTexture = new RenderTexture[c_refTextureCount];

        public string Name { get; set; }
        public string Description { get; set; }

        public const int c_parameterCount = 32;
        public const int c_refTextureCount = 2;

        class CreateBrushHelper
        {
            public string Name = "";
            public string Description = "";
            public string ImagePath = "";
            public string Culture = null;
            public string[] refTexturePath = new string[Brush.c_refTextureCount];
            public int[] parametersValue = new int[Brush.c_parameterCount];
            public float BrushSize;
        }
    }

    public class Brush1
    {
        public string Name;
        public string Description;
        public Guid Guid;
        public string Code;
    }
}
