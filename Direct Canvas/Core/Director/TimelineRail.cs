using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DirectCanvas.Core.Director
{
    public class TimelineRail : ICollection<TimelineRailItem>, INotifyCollectionChanged, INotifyPropertyChanged, IList<TimelineRailItem>
    {
        public TimelineRail()
        {
            tracks.CollectionChanged += CollectionChanged;
            ((INotifyPropertyChanged)tracks).PropertyChanged += PropertyChanged;
        }

        public ObservableCollection<TimelineRailItem> tracks = new ObservableCollection<TimelineRailItem>();


        public void Sort()
        {
            tracks.CollectionChanged -= CollectionChanged;
            ((INotifyPropertyChanged)tracks).PropertyChanged -= PropertyChanged;
            tracks = new ObservableCollection<TimelineRailItem>(tracks.OrderBy(i => i, new TimelineRailItemComparer()));
            tracks.CollectionChanged += CollectionChanged;
            ((INotifyPropertyChanged)tracks).PropertyChanged += PropertyChanged;
        }

        #region collectionInterface
        public TimelineRailItem this[int index] { get => ((IList<TimelineRailItem>)tracks)[index]; set => ((IList<TimelineRailItem>)tracks)[index] = value; }

        public int Count => ((IList<TimelineRailItem>)tracks).Count;

        public bool IsReadOnly => ((IList<TimelineRailItem>)tracks).IsReadOnly;

        public event NotifyCollectionChangedEventHandler CollectionChanged;
        public event PropertyChangedEventHandler PropertyChanged;

        public void Add(TimelineRailItem item)
        {
            ((IList<TimelineRailItem>)tracks).Add(item);
        }

        public void Clear()
        {
            ((IList<TimelineRailItem>)tracks).Clear();
        }

        public bool Contains(TimelineRailItem item)
        {
            return ((IList<TimelineRailItem>)tracks).Contains(item);
        }

        public void CopyTo(TimelineRailItem[] array, int arrayIndex)
        {
            ((IList<TimelineRailItem>)tracks).CopyTo(array, arrayIndex);
        }

        public IEnumerator<TimelineRailItem> GetEnumerator()
        {
            return ((IList<TimelineRailItem>)tracks).GetEnumerator();
        }

        public int IndexOf(TimelineRailItem item)
        {
            return ((IList<TimelineRailItem>)tracks).IndexOf(item);
        }

        public void Insert(int index, TimelineRailItem item)
        {
            ((IList<TimelineRailItem>)tracks).Insert(index, item);
        }

        public bool Remove(TimelineRailItem item)
        {
            return ((IList<TimelineRailItem>)tracks).Remove(item);
        }

        public void RemoveAt(int index)
        {
            ((IList<TimelineRailItem>)tracks).RemoveAt(index);
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return ((IList<TimelineRailItem>)tracks).GetEnumerator();
        }
        #endregion
    }

    class TimelineRailItemComparer : IComparer<TimelineRailItem>
    {
        public int Compare(TimelineRailItem x, TimelineRailItem y)
        {
            return (x.StartFrameIndex + x.ContinueFramesCount).CompareTo(y.StartFrameIndex + y.ContinueFramesCount);
        }
    }
}
