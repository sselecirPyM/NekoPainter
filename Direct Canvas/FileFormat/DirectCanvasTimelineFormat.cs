using DirectCanvas.Core.Director;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using Windows.Foundation;
using Windows.Storage;

namespace DirectCanvas.FileFormat
{
    //public static class DirectCanvasTimelineFormat
    //{

    //    public static IAsyncOperation<Timeline> LoadFromFileAsync(StorageFile file)
    //    {
    //        return Task<Timeline>.Run(async () =>
    //        {
    //            Timeline timeline = new Timeline();
    //            await ReloadAsync(file, timeline);
    //            return timeline;
    //        }).AsAsyncOperation();
    //    }

    //    public static async Task ReloadAsync(StorageFile file, Timeline timeline)
    //    {
    //        timeline.Initialiaze();
    //        var stream = (await file.OpenAsync(FileAccessMode.Read)).AsStream();
    //        var reader = XmlReader.Create(stream);
    //        while (reader.Read())
    //        {
    //            if (reader.NodeType == XmlNodeType.Element && reader.Name == "DCTimeline")
    //            {
    //                string name = reader.GetAttribute("Name");
    //                if (name != null) timeline.Name = name;
    //                if (Guid.TryParse(reader.GetAttribute("Guid"), out Guid guid))
    //                {
    //                    timeline.Guid = guid;
    //                    timeline.Saved = true;
    //                }
    //                else
    //                {
    //                    timeline.Guid = Guid.NewGuid();
    //                }
    //                while (reader.Read())
    //                {
    //                    if (reader.NodeType == XmlNodeType.Element)
    //                    {
    //                        if (reader.Name == "Rail")
    //                        {
    //                            var rail = new TimelineRail();
    //                            while (reader.Read())
    //                            {
    //                                if (reader.NodeType == XmlNodeType.Element)
    //                                {
    //                                    if (reader.Name == "AnimationTrack")
    //                                    {
    //                                        if (int.TryParse(reader.GetAttribute("StartFrame"), out int _startFrameIndex) &&
    //                                        int.TryParse(reader.GetAttribute("ContinueFrames"), out int _continueFramesCount) &&
    //                                        Guid.TryParse(reader.GetAttribute("TargetGuid"), out Guid _guid))
    //                                        {
    //                                            var animationTrack = new AnimationTrack()
    //                                            {
    //                                                StartFrameIndex = _startFrameIndex,
    //                                                ContinueFramesCount = _continueFramesCount,
    //                                                animationGuid = _guid
    //                                            };
    //                                            rail.Add(animationTrack);
    //                                        }
    //                                    }
    //                                    reader.Skip();
    //                                }
    //                                else if (reader.NodeType == XmlNodeType.EndElement && reader.Name == "Rail")
    //                                    break;
    //                            }
    //                            timeline.rails.Add(rail);
    //                        }
    //                        reader.Skip();
    //                    }
    //                    else if (reader.NodeType == XmlNodeType.EndElement && reader.Name == "Animation")
    //                        break;
    //                }
    //            }
    //        }
    //    }

    //    public static async Task SaveToFileAsync(Timeline timeline, StorageFile file)
    //    {
    //        var stream = (await file.OpenAsync(FileAccessMode.ReadWrite)).AsStream();
    //        XmlWriter writer = XmlWriter.Create(stream, new XmlWriterSettings() { Encoding = Encoding.UTF8, Indent = true });
    //        writer.WriteStartDocument();
    //        writer.WriteStartElement("DCTimeline");
    //        if (!string.IsNullOrEmpty(timeline.Name))
    //            writer.WriteAttributeString("Name", timeline.Name);
    //        writer.WriteAttributeString("Guid", timeline.Guid.ToString());

    //        for (int ia = 0; ia < timeline.rails.Count; ia++)
    //        {
    //            var rail = timeline.rails[ia];
    //            writer.WriteStartElement("Rail");
    //            for (int ib = 0; ib < rail.Count; ib++)
    //            {
    //                var track = rail[ib];
    //                if (track is AnimationTrack animationTrack)
    //                {
    //                    writer.WriteStartElement("AnimationTrack");
    //                    writer.WriteAttributeString("StartFrame", track.StartFrameIndex.ToString());
    //                    writer.WriteAttributeString("ContinueFrames", track.ContinueFramesCount.ToString());
    //                    writer.WriteAttributeString("TargetGuid", animationTrack.animationGuid.ToString());
    //                    writer.WriteEndElement();

    //                }
    //            }
    //            writer.WriteEndElement();
    //        }
    //        writer.WriteEndDocument();
    //        writer.Flush();
    //        stream.Dispose();
    //    }
    //}
}
