using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DirectCanvas.Core.Director
{
    //public class Timeline
    //{
    //    public string Name = "";
    //    public Guid Guid;
    //    public bool Saved;
    //    public ObservableCollection<TimelineRail> rails = new ObservableCollection<TimelineRail>();
    //    public void ToFrameIndex(IDictionary<Guid, LayoutPropertyBag> dictionary, float frameIndex)
    //    {
    //        for (int ia = 0; ia < rails.Count; ia++)
    //        {
    //            var rail = rails[ia];
    //            for (int ib = 0; ib < rail.Count; ib++)
    //            {
    //                var item = rail[ib];
    //                if (frameIndex >= item.StartFrameIndex + item.ContinueFramesCount)
    //                {
    //                    item.EndingEffect(dictionary);
    //                }
    //                else if (frameIndex > item.StartFrameIndex)
    //                {
    //                    rail[ib].ToFrameIndex(dictionary, frameIndex - rail[ib].StartFrameIndex);
    //                }
    //            }
    //        }
    //    }

    //    public void Initialiaze()
    //    {
    //        rails.Clear();
    //        Guid = Guid.Empty;
    //        Saved = false;
    //    }
    //}
}
