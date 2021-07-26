using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Storage;
using System.Xml;
using System.IO;
using System.Runtime.InteropServices.WindowsRuntime;
using CanvasRendering;
using Windows.ApplicationModel;
using Windows.Storage.Streams;
using DirectCanvas.Util;
using System.Xml.Serialization;

namespace DirectCanvas.Core
{
    public class BlendMode
    {
        public BlendMode(ComputeShader[] cX)
        {
            csBlend = cX;
        }
        static string[] componentCode = new string[c_csBlendCount];
        public const int c_parameterCount = 32;
        const int c_csBlendCount = 7;

        static string appUsedCultureName;
        public static async Task LoadStaticResourcesAsync()
        {
            appUsedCultureName = System.Threading.Thread.CurrentThread.CurrentUICulture.Name;
            componentCode[0] = await DCUtil.ReadStringAsync("Shaders\\blendmode_c1.hlsl");
            componentCode[1] = await DCUtil.ReadStringAsync("Shaders\\blendmode_c2.hlsl");
            componentCode[2] = await DCUtil.ReadStringAsync("Shaders\\blendmode_c3.hlsl");
            componentCode[3] = await DCUtil.ReadStringAsync("Shaders\\blendmode_c4.hlsl");
            componentCode[4] = await DCUtil.ReadStringAsync("Shaders\\blendmode_c5.hlsl");
            componentCode[5] = await DCUtil.ReadStringAsync("Shaders\\blendmode_c6.hlsl");
            componentCode[6] = await DCUtil.ReadStringAsync("Shaders\\blendmode_c7.hlsl");
        }

        //static bool CultureCheck2(DCParameter parameter, string culture)
        //{
        //    if (culture == null) culture = "";
        //    if (parameter.Culture == null) parameter.Culture = culture;
        //    bool isCurrentCulture = appUsedCultureName.Equals(culture, StringComparison.CurrentCultureIgnoreCase);
        //    bool inCurrentCulture = appUsedCultureName.Equals(parameter.Culture, StringComparison.CurrentCultureIgnoreCase);
        //    bool isSubstitute = culture.Equals(parameter.Culture, StringComparison.CurrentCultureIgnoreCase);
        //    if (isCurrentCulture ||
        //        isSubstitute ||
        //        (string.IsNullOrEmpty(culture) && !inCurrentCulture))
        //    {
        //        parameter.Culture = culture;
        //        return true;
        //    }
        //    else return false;
        //}

        //static bool CultureCheck2(ref string curCul, string culture)
        //{
        //    if (culture == null) culture = "";
        //    if (curCul == null) curCul = culture;
        //    bool isCurrentCulture = appUsedCultureName.Equals(culture, StringComparison.CurrentCultureIgnoreCase);
        //    bool inCurrentCulture = appUsedCultureName.Equals(curCul, StringComparison.CurrentCultureIgnoreCase);
        //    bool isSubstitute = culture.Equals(curCul, StringComparison.CurrentCultureIgnoreCase);
        //    if (isCurrentCulture ||
        //        isSubstitute ||
        //        (string.IsNullOrEmpty(culture) && !inCurrentCulture))
        //    {
        //        curCul = culture;
        //        return true;
        //    }
        //    else return false;
        //}
        public static XmlSerializer xmlSerializer = new XmlSerializer(typeof(BlendModeCode));

        public static async Task<BlendMode> LoadFromFileAsync(DeviceResources deviceResources, StorageFile file)
        {
            Stream stream = await file.OpenStreamForReadAsync();
            BlendModeCode blendModeCode =(BlendModeCode) xmlSerializer.Deserialize(stream);

            ComputeShader[] shaders = new ComputeShader[c_csBlendCount];
            Parallel.For(0, c_csBlendCount, (int i) =>
            {
                shaders[i] = ComputeShader.CompileAndCreate(deviceResources, Encoding.UTF8.GetBytes(componentCode[i].Replace("#define codehere", blendModeCode.Code)));
            });

            BlendMode blendMode = new BlendMode(shaders);
            blendMode.Name = blendModeCode.Name;
            blendMode.Description = blendModeCode.Description;
            blendMode.Guid = blendModeCode.Guid;

            return blendMode;
        }
        //public static async Task<BlendMode> LoadFromFileAsync(DeviceResources deviceResources, StorageFile file)
        //{
        //    Stream stream = await file.OpenStreamForReadAsync();
        //    XmlReaderSettings setting1 = new XmlReaderSettings();
        //    setting1.IgnoreComments = true;
        //    XmlReader xmlReader = XmlReader.Create(stream, setting1);
        //    string code = null;
        //    string name = "";
        //    string description = "";
        //    //string imagePath = "";
        //    Guid guid = Guid.Empty;
        //    //DCParameter[] dcParameters = new DCParameter[c_parameterCount];
        //    //for (int i = 0; i < c_parameterCount; i++)
        //    //{
        //    //    dcParameters[i] = new DCParameter();
        //    //}
        //    while (xmlReader.Read())
        //    {
        //        if (xmlReader.NodeType == XmlNodeType.Element)
        //        {
        //            switch (xmlReader.Name)
        //            {
        //                case "BlendMode":
        //                    //guid = Guid.Parse(xmlReader.GetAttribute("Guid"));
        //                    //imagePath = xmlReader.GetAttribute("Image");
        //                    break;
        //                case "Code":
        //                    code = xmlReader.ReadElementContentAsString();
        //                    break;
        //                //case "Parameter":
        //                //    int.TryParse(xmlReader.GetAttribute("Index"), out int index);
        //                //    dcParameters[index].Type = xmlReader.GetAttribute("Type");
        //                //    if (string.IsNullOrEmpty(dcParameters[index].Type))
        //                //        dcParameters[index].Type = "TextBox";
        //                //    if (int.TryParse(xmlReader.GetAttribute("MaxValue"), out int maxValue)) dcParameters[index].MaxValue = maxValue;
        //                //    else dcParameters[index].MaxValue = int.MaxValue;
        //                //    if (int.TryParse(xmlReader.GetAttribute("MinValue"), out int minValue)) dcParameters[index].MinValue = minValue;
        //                //    else dcParameters[index].MinValue = int.MinValue;
        //                //    while (xmlReader.Read())
        //                //    {
        //                //        if (xmlReader.NodeType == XmlNodeType.Element)
        //                //        {
        //                //            switch (xmlReader.Name)
        //                //            {
        //                //                case "Name":
        //                //                    if (CultureCheck2(dcParameters[index], xmlReader.GetAttribute("Culture")))
        //                //                    {
        //                //                        dcParameters[index].Name = xmlReader.ReadElementContentAsString();
        //                //                    }
        //                //                    continue;
        //                //                case "Description":
        //                //                    if (CultureCheck2(dcParameters[index], xmlReader.GetAttribute("Culture")))
        //                //                    {
        //                //                        dcParameters[index].Description = xmlReader.ReadElementContentAsString();
        //                //                    }
        //                //                    continue;
        //                //            }
        //                //            xmlReader.Skip();
        //                //        }
        //                //        else if (xmlReader.NodeType == XmlNodeType.EndElement && xmlReader.Name == "Parameter")
        //                //            break;
        //                //    }
        //                //    break;
        //                case "Guid":
        //                    guid = Guid.Parse(xmlReader.ReadElementContentAsString());
        //                    break;
        //                case "Name":
        //                    name = xmlReader.ReadElementContentAsString();
        //                    break;
        //                case "Description":
        //                    description = xmlReader.ReadElementContentAsString();
        //                    break;
        //            }
        //        }
        //    }
        //    ComputeShader[] shaders = new ComputeShader[c_csBlendCount];
        //    Parallel.For(0, c_csBlendCount, (int i) =>
        //    {
        //        shaders[i] = ComputeShader.CompileAndCreate(deviceResources, Encoding.UTF8.GetBytes(componentCode[i].Replace("#define codehere", code)));
        //    });
        //    BlendMode blendMode = new BlendMode(shaders);
        //    blendMode.Name = name;
        //    blendMode.Guid = guid;
        //    blendMode.Description = description;
        //    //for (int j = 0; j < c_parameterCount; j++)
        //    //{
        //    //    blendMode.Parameters[j] = new DCParameter();
        //    //    blendMode.Parameters[j].Name = dcParameters[j].Name;
        //    //    blendMode.Parameters[j].Description = dcParameters[j].Description;
        //    //    blendMode.Parameters[j].Type = dcParameters[j].Type;
        //    //    blendMode.Parameters[j].MaxValue = dcParameters[j].MaxValue;
        //    //    blendMode.Parameters[j].MinValue = dcParameters[j].MinValue;
        //    //    blendMode.Parameters[j].Value = 0;
        //    //}
        //    return blendMode;
        //}

        public void Blend(RenderTexture source, RenderTexture target, ConstantBuffer parametersData, int ofs, int size)
        {
            int width = source.width;
            int height = source.height;

            int x = (width + 31) / 32;
            int y = (height + 31) / 32;
            csBlend[0].SetSRV(source, 0);
            csBlend[0].SetUAV(target, 0);
            csBlend[0].SetCBV(parametersData, 0, ofs, size);
            csBlend[0].Dispatch(x, y, 1);
        }

        public void Blend(TiledTexture source, RenderTexture target, ConstantBuffer parametersData, int ofs, int size)
        {
            if (source.tilesCount == 0) return;
            csBlend[1].SetSRV(source.BlocksData, 0);
            csBlend[1].SetSRV(source.BlocksOffsetsData, 1);
            csBlend[1].SetUAV(target, 0);
            csBlend[1].SetCBV(parametersData, 0, ofs, size);
            csBlend[1].Dispatch(1, 1, (source.tilesCount + 15) / 16);
        }

        public void Blend(RenderTexture source, RenderTexture target, List<Int2> part, ConstantBuffer parametersData, int ofs, int size)
        {
            if (part == null || part.Count == 0) return;
            int z = (part.Count + 15) / 16;
            ComputeBuffer buf_Part = new ComputeBuffer(source.GetDeviceResources(), part.Count, 8, part.ToArray());
            csBlend[2].SetSRV(source, 0);
            csBlend[2].SetSRV(buf_Part, 1);
            csBlend[2].SetUAV(target, 0);
            csBlend[2].SetCBV(parametersData, 0, ofs, size);
            csBlend[2].Dispatch(1, 1, z);
            buf_Part.Dispose();
        }

        public void Blend(TiledTexture source, RenderTexture target, List<Int2> part, ConstantBuffer parametersData, int ofs, int size)
        {
            if (part == null || part.Count == 0 || source.tilesCount == 0) return;
            List<int> hParts = new List<int>();
            for (int i = 0; i < part.Count; i++)
            {
                //if (source.TilesStatus.TryGetValue(part[i], out int tIndex))
                //{
                //    hParts.Add(tIndex);
                //}
                int tIndex = source.TilesStatus[part[i]];
                if (tIndex != -1)
                {
                    hParts.Add(tIndex);
                }
            }
            if (hParts.Count == 0) return;
            int z = (hParts.Count + 15) / 16;
            ComputeBuffer buf_Index = new ComputeBuffer(source.deviceResources, hParts.Count, 4, hParts.ToArray());
            csBlend[3].SetSRV(source.BlocksData, 0);
            csBlend[3].SetSRV(buf_Index, 1);
            csBlend[3].SetSRV(source.BlocksOffsetsData, 2);
            csBlend[3].SetUAV(target, 0);
            csBlend[3].SetCBV(parametersData, 0, ofs, size);
            csBlend[3].Dispatch(1, 1, z);
            buf_Index.Dispose();
        }

        public void Blend3Indicate(TiledTexture source, RenderTexture target, List<int> indicate, ConstantBuffer parametersData, int ofs, int size)
        {
            if (indicate == null || indicate.Count == 0 || source.tilesCount == 0) return;
            int z = (indicate.Count + 15) / 16;
            ComputeBuffer buf_Index = new ComputeBuffer(source.deviceResources, indicate.Count, 4, indicate.ToArray());
            csBlend[3].SetSRV(source.BlocksData, 0);
            csBlend[3].SetSRV(buf_Index, 1);
            csBlend[3].SetSRV(source.BlocksOffsetsData, 2);
            csBlend[3].SetUAV(target, 0);
            csBlend[3].SetCBV(parametersData, 0, ofs, size);
            csBlend[3].Dispatch(1, 1, z);
            buf_Index.Dispose();
        }

        public void BlendPure(RenderTexture target, ConstantBuffer parametersData, int ofs, int size)
        {

            int width = target.width;
            int height = target.height;

            int x = (width + 31) / 32;
            int y = (height + 31) / 32;
            csBlend[4].SetUAV(target, 0);
            csBlend[4].SetCBV(parametersData, 0, ofs, size);
            csBlend[4].Dispatch(x, y, 1);
        }

        //public void BlendColor(RenderTexture target, List<Int2> part, ConstantBuffer parametersData)
        //{
        //    if (part == null || part.Count == 0) return;
        //    int z = (part.Count + 15) / 16;
        //    ComputeBuffer buf_Part = new ComputeBuffer(target.GetDeviceResources(), part.Count, 8, part.ToArray());
        //    csBlend[5].SetSRV(buf_Part, 0);
        //    csBlend[5].SetUAV(target, 0);
        //    csBlend[5].SetCBV(parametersData, 1);
        //    csBlend[5].Dispatch(1, 1, z);
        //    buf_Part.Dispose();
        //}

        //public void Blend7(RenderTexture source, RenderTexture target, ConstantBuffer parametersData, ConstantBuffer selectionOffsetData)
        //{
        //    int width = source.width;
        //    int height = source.height;

        //    int x = (width + 31) / 32;
        //    int y = (height + 31) / 32;
        //    csBlend[0].SetSRV(source, 0);
        //    csBlend[6].SetUAV(target, 0);
        //    csBlend[6].SetCBV(parametersData, 0);
        //    csBlend[6].SetCBV(selectionOffsetData, 1);
        //    csBlend[6].Dispatch(x, y, 1);
        //}

        private readonly ComputeShader[] csBlend = new ComputeShader[c_csBlendCount];

        //public DCParameter[] Parameters = new DCParameter[c_parameterCount];

        public string Name { get; set; }
        public string Description { get; set; }

        public Guid Guid { get; set; }

        public override string ToString()
        {
            return Name;
        }
    }
    [XmlRoot("BlendMode")]
    public class BlendModeCode
    {
        public Guid Guid;
        public string Name;
        public string Description;
        public string Code;
        public string Image;
    }
}
