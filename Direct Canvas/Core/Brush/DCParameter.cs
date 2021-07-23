using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DirectCanvas.Core
{
    public class DCParameter : INotifyPropertyChanged
    {
        public string Name;
        public string Description;
        public int Value { get => _value; set { _value = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("Value")); } }
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
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("ViewTextBox"));
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("ViewSlider"));
            }
        }
        string _type;
        public bool ViewTextBox { get => "TextBox".Equals(Type, StringComparison.CurrentCultureIgnoreCase) || "fTextBox".Equals(Type, StringComparison.CurrentCultureIgnoreCase); }
        public bool ViewSlider { get => "Slider".Equals(Type, StringComparison.CurrentCultureIgnoreCase); }
        public bool IsFloat { get => "fTextBox".Equals(Type, StringComparison.CurrentCultureIgnoreCase); }

        public event PropertyChangedEventHandler PropertyChanged;
    }
}
