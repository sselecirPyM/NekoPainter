using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using DirectCanvas.Core.Director;
using Windows.Foundation;
using Windows.Storage;
using System.IO;

namespace DirectCanvas.FileFormat
{
    //public static class DirectCanvasAnimationFormat
    //{
    //    public static IAsyncOperation<Animation> LoadFromFileAsync(StorageFile file)
    //    {
    //        return Task<Animation>.Run(async () =>
    //        {
    //            Animation animation = new Animation();
    //            await ReloadAsync(file, animation);
    //            return animation;
    //        }).AsAsyncOperation();
    //    }

    //    public static async Task ReloadAsync(StorageFile file, Animation animation)
    //    {
    //        animation.Initialize();
    //        var stream = (await file.OpenAsync(FileAccessMode.Read)).AsStream();
    //        var reader = XmlReader.Create(stream);
    //        while (reader.Read())
    //        {
    //            if (reader.NodeType == XmlNodeType.Element && reader.Name == "DCAnimation")
    //            {
    //                string name = reader.GetAttribute("Name");
    //                if (name != null) animation.Name = name;
    //                if (Guid.TryParse(reader.GetAttribute("Guid"), out Guid guid))
    //                {
    //                    animation.Guid = guid;
    //                    animation.Saved = true;
    //                }
    //                else
    //                {
    //                    animation.Guid = Guid.NewGuid();
    //                }
    //                if (Guid.TryParse(reader.GetAttribute("TargetGuid"), out Guid _targetGuid))
    //                    animation.TargetGuid = _targetGuid;
    //                while (reader.Read())
    //                {
    //                    if (reader.NodeType == XmlNodeType.Element)
    //                    {
    //                        if (reader.Name == "Rail")
    //                        {
    //                            var rail = new AnimationTagRail();
    //                            switch (reader.GetAttribute("Type"))
    //                            {
    //                                case "Property":
    //                                    rail.RailType = AnimationTagType.LayoutPropertyTag;
    //                                    break;
    //                                case "Visible":
    //                                    rail.RailType = AnimationTagType.LayoutVisibleTag;
    //                                    break;
    //                            }
    //                            if (int.TryParse(reader.GetAttribute("Index"), out int _index))
    //                                rail.propertyIndex = _index;
    //                            if (bool.TryParse(reader.GetAttribute("Muted"), out bool _muted))
    //                                rail.Muted = _muted;

    //                            while (reader.Read())
    //                            {
    //                                if (reader.NodeType == XmlNodeType.Element)
    //                                {
    //                                    if (reader.Name == "Tag")
    //                                    {
    //                                        if (int.TryParse(reader.GetAttribute("Frame"), out int _frameIndex) &&
    //                                        int.TryParse(reader.GetAttribute("Value"), out int _value))
    //                                        {
    //                                            rail.Add(new AnimationTag() { FrameIndex = _frameIndex, Value = _value });
    //                                        }
    //                                    }
    //                                    reader.Skip();
    //                                }
    //                                else if (reader.NodeType == XmlNodeType.EndElement && reader.Name == "Rail")
    //                                    break;
    //                            }
    //                            animation.animationTagRails.Add(rail);
    //                        }
    //                        reader.Skip();
    //                    }
    //                    else if (reader.NodeType == XmlNodeType.EndElement && reader.Name == "Animation")
    //                        break;
    //                }
    //            }
    //        }
    //    }

    //    public static async Task SaveToFileAsync(Animation animation, StorageFile file)
    //    {
    //        var stream = (await file.OpenAsync(FileAccessMode.ReadWrite)).AsStream();
    //        XmlWriter writer = XmlWriter.Create(stream, new XmlWriterSettings() { Encoding = Encoding.UTF8, Indent = true });
    //        writer.WriteStartDocument();
    //        writer.WriteStartElement("DCAnimation");
    //        if (!string.IsNullOrEmpty(animation.Name))
    //            writer.WriteAttributeString("Name", animation.Name);
    //        writer.WriteAttributeString("Guid", animation.Guid.ToString());
    //        writer.WriteAttributeString("TargetGuid", animation.TargetGuid.ToString());

    //        for (int ia = 0; ia < animation.animationTagRails.Count; ia++)
    //        {
    //            var rail = animation.animationTagRails[ia];
    //            writer.WriteStartElement("Rail");
    //            switch (rail.RailType)
    //            {
    //                case AnimationTagType.LayoutPropertyTag:
    //                    writer.WriteAttributeString("Type", "Property");
    //                    writer.WriteAttributeString("Index", rail.propertyIndex.ToString());
    //                    break;
    //                case AnimationTagType.LayoutVisibleTag:
    //                    writer.WriteAttributeString("Type", "Visible");
    //                    break;
    //            }
    //            if (rail.Muted)
    //                writer.WriteAttributeString("Muted", rail.Muted.ToString());
    //            for (int ib = 0; ib < rail.Count; ib++)
    //            {
    //                var tag = rail[ib];
    //                writer.WriteStartElement("Tag");
    //                writer.WriteAttributeString("Frame", tag.FrameIndex.ToString());
    //                writer.WriteAttributeString("Value", tag.Value.ToString());
    //                writer.WriteEndElement();
    //            }
    //            writer.WriteEndElement();
    //        }
    //        writer.WriteEndDocument();
    //        writer.Flush();
    //        stream.Dispose();
    //    }
    //}
}
