using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DirectCanvas.Core.Director
{
    public abstract class TimelineRailItem : INotifyPropertyChanged
    {
        public int StartFrameIndex;
        public int ContinueFramesCount;

        public event PropertyChangedEventHandler PropertyChanged;
        protected void PropertyChange(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public virtual void ToFrameIndex(IDictionary<Guid, LayoutPropertyBag> dictionary, float frameIndex)
        {

        }

        public virtual void Effect(IDictionary<Guid, LayoutPropertyBag> dictionary, float input)
        {

        }

        public virtual void EndingEffect(IDictionary<Guid, LayoutPropertyBag> dictionary)
        {

        }
    }
}
