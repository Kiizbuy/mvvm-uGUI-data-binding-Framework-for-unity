using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;

namespace MVVM.Runtime.Collections
{
    public sealed class ObservableDataList<T> : IList<T>,
        INotifyCollectionChanged,
        INotifyPropertyChanged
    {
        public event NotifyCollectionChangedEventHandler CollectionChanged;
        public event PropertyChangedEventHandler PropertyChanged;
        
        private const string PropertyListId = "Item[]";
        
        private readonly List<T> _inner = new List<T>();
  
        public T this[int index]
        {
            get => _inner[index];
            set
            {
                var old = _inner[index];
                if (EqualityComparer<T>.Default.Equals(old, value))
                    return;

                _inner[index] = value;
                OnPropertyChanged(PropertyListId);
                OnCollectionChanged(new NotifyCollectionChangedEventArgs(
                    NotifyCollectionChangedAction.Replace, 
                    value, 
                    old, 
                    index)
                );
            }
        }

        public int Count => _inner.Count;
        public bool IsReadOnly => ((ICollection<T>) _inner).IsReadOnly;

        public void Add(T item)
        {
            _inner.Add(item);
            OnPropertyChanged(nameof(Count));
            OnCollectionChanged(new NotifyCollectionChangedEventArgs(
                NotifyCollectionChangedAction.Add, item, _inner.Count - 1));
        }
        
        public void AddRange(IEnumerable<T> items) => AddRange(items, suppressNotification: false);

        public void AddRange(IEnumerable<T> items, bool suppressNotification)
        {
            if (items == null) 
                throw new ArgumentNullException(nameof(items));

            var list = items as IList<T> ?? items.ToList();

            if (list.Count == 0)
                return;

            var startIndex = _inner.Count;
            _inner.AddRange(list);

            if (suppressNotification)
                return;

            OnPropertyChanged(nameof(Count));
            OnCollectionChanged(new NotifyCollectionChangedEventArgs(
                NotifyCollectionChangedAction.Add,
                (IList)list,
                startIndex));
        }
        
        public void Clear()
        {
            _inner.Clear();
            OnPropertyChanged(nameof(Count));
            OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
        }

        public bool Contains(T item) => _inner.Contains(item);
        public void CopyTo(T[] array, int arrayIndex) => _inner.CopyTo(array, arrayIndex);
        public IEnumerator<T> GetEnumerator() => _inner.GetEnumerator();
        public int IndexOf(T item) => _inner.IndexOf(item);

        public void Insert(int index, T item)
        {
            _inner.Insert(index, item);
            OnPropertyChanged(nameof(Count));
            OnCollectionChanged(new NotifyCollectionChangedEventArgs(
                NotifyCollectionChangedAction.Add, item, index));
        }

        public bool Remove(T item)
        {
            var idx = _inner.IndexOf(item);
            if (idx < 0) return false;

            _inner.RemoveAt(idx);
            OnPropertyChanged(nameof(Count));
            OnCollectionChanged(new NotifyCollectionChangedEventArgs(
                NotifyCollectionChangedAction.Remove, item, idx));
            return true;
        }

        public void RemoveAt(int index)
        {
            var old = _inner[index];
            _inner.RemoveAt(index);
            OnPropertyChanged(nameof(Count));
            OnCollectionChanged(new NotifyCollectionChangedEventArgs(
                NotifyCollectionChangedAction.Remove, old, index));
        }

        IEnumerator IEnumerable.GetEnumerator() => ((IEnumerable) _inner).GetEnumerator();

        private void OnCollectionChanged(NotifyCollectionChangedEventArgs e) =>
            CollectionChanged?.Invoke(this, e);

        private void OnPropertyChanged(string propertyName) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}