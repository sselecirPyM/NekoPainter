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
    public class AnimationTagRail : ICollection<AnimationTag>, INotifyCollectionChanged, INotifyPropertyChanged, IList<AnimationTag>
    {
        /// <summary>
        /// 这个轨道所更改的属性的索引 0即为混合模式参数1
        /// </summary>
        public int propertyIndex;
        public AnimationTagType RailType;
        public ObservableCollection<AnimationTag> tagCollection = new ObservableCollection<AnimationTag>();

        public bool Muted
        {
            get { return _muted; }
            set
            {
                if (_muted == value) return;
                _muted = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("Muted"));
            }
        }
        bool _muted;

        public string Name
        {
            get { return _name; }
            set
            {
                if (_name == value) return;
                _name = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("Name"));
            }
        }
        string _name = "";

        public AnimationTagRail()
        {
            tagCollection.CollectionChanged += CollectionChanged;
            ((INotifyPropertyChanged)tagCollection).PropertyChanged += PropertyChanged;
        }

        public int GetAnimationLength()
        {
            if (tagCollection.Count > 0)
            {
                return tagCollection.Last().FrameIndex;
            }
            else return 0;
        }

        public void Sort()
        {
            tagCollection.CollectionChanged -= CollectionChanged;
            ((INotifyPropertyChanged)tagCollection).PropertyChanged -= PropertyChanged;
            tagCollection = new ObservableCollection<AnimationTag>(tagCollection.OrderBy(i => i, new AnimationTagComparer()));
            tagCollection.CollectionChanged += CollectionChanged;
            ((INotifyPropertyChanged)tagCollection).PropertyChanged += PropertyChanged;
        }
        #region collectionInterface
        public int Count => ((ICollection<AnimationTag>)tagCollection).Count;

        public bool IsReadOnly => ((ICollection<AnimationTag>)tagCollection).IsReadOnly;

        public AnimationTag this[int index] { get => ((IList<AnimationTag>)tagCollection)[index]; set => ((IList<AnimationTag>)tagCollection)[index] = value; }

        public event NotifyCollectionChangedEventHandler CollectionChanged;

        public event PropertyChangedEventHandler PropertyChanged;

        public void Add(AnimationTag item)
        {
            ((ICollection<AnimationTag>)tagCollection).Add(item);
        }

        public void Clear()
        {
            ((ICollection<AnimationTag>)tagCollection).Clear();
        }

        public bool Contains(AnimationTag item)
        {
            return ((ICollection<AnimationTag>)tagCollection).Contains(item);
        }

        public void CopyTo(AnimationTag[] array, int arrayIndex)
        {
            ((ICollection<AnimationTag>)tagCollection).CopyTo(array, arrayIndex);
        }

        public IEnumerator<AnimationTag> GetEnumerator()
        {
            return ((ICollection<AnimationTag>)tagCollection).GetEnumerator();
        }

        public bool Remove(AnimationTag item)
        {
            return ((ICollection<AnimationTag>)tagCollection).Remove(item);
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return ((ICollection<AnimationTag>)tagCollection).GetEnumerator();
        }

        public int IndexOf(AnimationTag item)
        {
            return ((IList<AnimationTag>)tagCollection).IndexOf(item);
        }

        public void Insert(int index, AnimationTag item)
        {
            ((IList<AnimationTag>)tagCollection).Insert(index, item);
        }

        public void RemoveAt(int index)
        {
            ((IList<AnimationTag>)tagCollection).RemoveAt(index);
        }
        #endregion
    }

    class AnimationTagComparer : IComparer<AnimationTag>
    {
        public int Compare(AnimationTag x, AnimationTag y)
        {
            return x.FrameIndex.CompareTo(y.FrameIndex);
        }
    }

    public enum AnimationTagType
    {
        LayoutPropertyTag,
        LayoutVisibleTag
    }
}
