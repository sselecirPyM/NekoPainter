using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace DirectCanvas.Core
{
    [XmlType("Parameter")]
    public class DCParameter
    {
        public string Name;
        public string Description;
        public int Value;
        public float fValue;
        public double MaxValue = int.MaxValue;
        public double MinValue = int.MinValue;
        public string Culture = null;
        public string Type;
        public bool IsFloat { get => string.IsNullOrEmpty(Type); }
    }
}
