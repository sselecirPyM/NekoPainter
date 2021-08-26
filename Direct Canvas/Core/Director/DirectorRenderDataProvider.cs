using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DirectCanvas.Core;

namespace DirectCanvas.Core.Director
{
    //public class DirectorRenderDataProvider : RenderDataProvider
    //{
    //    public override void GetData(PictureLayout layout, int[] outData, out bool hidden)
    //    {
    //        if (LayoutPropertyBagMap.TryGetValue(layout.guid, out var layoutPropertyBag))
    //        {
    //            hidden = layoutPropertyBag.hiddenUsed ? layoutPropertyBag.hidden : layout.Hidden;
    //            for (int j = 0; j < Core.BlendMode.c_parameterCount; j++)
    //            {
    //                outData[j] = layoutPropertyBag.propertyValueUsed[j] ? layoutPropertyBag.propertyValues[j] : layout.Parameters[j].Value;
    //            }
    //        }
    //        else
    //        {
    //            hidden = layout.Hidden;
    //            for (int j = 0; j < Core.BlendMode.c_parameterCount; j++)
    //            {
    //                outData[j] = layout.Parameters[j].Value;
    //            }
    //        }
    //    }
    //    public Dictionary<Guid, LayoutPropertyBag> LayoutPropertyBagMap = new Dictionary<Guid, LayoutPropertyBag>();
    //}
}
