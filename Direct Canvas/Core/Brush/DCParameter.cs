using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DirectCanvas.Core
{
    public class DCParameter
    {
        public string Name;
        public string Description;
        public int Value;
        public float fValue;
        public int MaxValue;
        public int MinValue;
        public string Culture = null;
        public string Type { get; set; }
        public bool IsFloat { get => "fTextBox".Equals(Type, StringComparison.CurrentCultureIgnoreCase); }
    }
}
