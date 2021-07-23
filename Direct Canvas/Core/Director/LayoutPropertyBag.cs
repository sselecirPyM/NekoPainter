using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DirectCanvas.Core.Director
{
    public class LayoutPropertyBag
    {
        public int[] propertyValues = new int[BlendMode.c_parameterCount];
        public bool[] propertyValueUsed = new bool[BlendMode.c_parameterCount];
        public bool hidden = false;
        public bool hiddenUsed = false;
    }
}
