using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace DirectCanvas.Core
{
    [XmlType("Parameter")]
    public class DCParameter
    {
        [XmlAttribute]
        public string Name;
        public string Description;
        public double Value;
        public double MaxValue = int.MaxValue;
        public double MinValue = int.MinValue;
        public string Culture = null;
        public string Type;
        public bool IsFloat { get => true; }
    }
    [XmlType("Parameter")]
    public struct ParameterN
    {
        [XmlAttribute]
        public string Name;
        public double X;
        public double Y;
        public double Z;
        public double W;
    }
}
