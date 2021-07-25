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
        public int Value { get => _value; set { _value = value; } }
        public float fValue { get => _fValue; set => _fValue = value; }
        public int _value;
        public float _fValue;
        public int MaxValue;
        public int MinValue;
        public string Culture = null;
        public string Type
        {
            get => _type; set
            {
                _type = value;
            }
        }
        string _type;
        public bool IsFloat { get => "fTextBox".Equals(Type, StringComparison.CurrentCultureIgnoreCase); }
    }
}
