using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq.Expressions;

namespace MVVM.Runtime.Binders
{
    public abstract class CollectionBinderBase : BinderBase
    {
        protected INotifyCollectionChanged SubscribedCollection;
        protected Type ElementType;
        protected Func<object, string> ElementToString;
        protected Func<object, int, object> GetAt;
        protected Func<object, int> GetCount;

        protected readonly List<object> Snapshot = new();

        protected override void OnViewModelValueChanged()
        {
            UnsubscribeCollection();

            var raw = Getter?.Invoke();
            ResetHelpers();

            if (raw == null)
            {
                Snapshot.Clear();
                OnUiClear();
                return;
            }

            SubscribeCollectionIfNeeded(raw);
            BuildElementHelpers(raw);

            OnUiUpdateBegin();
            try
            {
                RebuildSnapshotAndUi(raw);
            }
            finally
            {
                OnUiUpdateEnd();
            }

        }

        protected override void OnDestroy()
        {
            UnsubscribeCollection();
            base.OnDestroy();
        }

        private void ResetHelpers()
        {
            ElementType = null;
            ElementToString = null;
            GetAt = null;
            GetCount = null;
            Snapshot.Clear();
        }

        private void SubscribeCollectionIfNeeded(object raw)
        {
            if (raw is not INotifyCollectionChanged notifier) 
                return;
            
            SubscribedCollection = notifier;
            SubscribedCollection.CollectionChanged -= OnCollectionChanged;
            SubscribedCollection.CollectionChanged += OnCollectionChanged;
        }

        private void UnsubscribeCollection()
        {
            if (SubscribedCollection == null) 
                return;
            
            SubscribedCollection.CollectionChanged -= OnCollectionChanged;
            SubscribedCollection = null;
        }

        private void RebuildSnapshotAndUi(object raw)
        {
            Snapshot.Clear();

            if (GetCount != null && GetAt != null)
            {
                var cnt = GetCount(raw);
                for (int i = 0; i < cnt; i++)
                {
                    Snapshot.Add(GetAt(raw, i));
                }
            }
            else if (raw is IEnumerable en)
            {
                foreach (var it in en)
                {
                    Snapshot.Add(it);
                }
            }

            OnUiClear();

            for (int i = 0; i < Snapshot.Count; i++)
            {
                OnUiInsert(i, Snapshot[i]);
            }

            OnUiRefresh(); // optional hook to e.g. call RefreshShownValue
        }

        private void OnCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            OnUiUpdateBegin();
            try
            {
                if (ElementToString == null)
                {
                    RebuildSnapshotAndUi(Getter?.Invoke());
                    return;
                }

                switch (e.Action)
                {
                    case NotifyCollectionChangedAction.Add:
                        HandleAdd(e);
                        break;
                    case NotifyCollectionChangedAction.Remove:
                        HandleRemove(e);
                        break;
                    case NotifyCollectionChangedAction.Replace:
                        HandleReplace(e);
                        break;
                    case NotifyCollectionChangedAction.Move:
                        HandleMove(e);
                        break;
                    case NotifyCollectionChangedAction.Reset:
                    default:
                        RebuildSnapshotAndUi(Getter?.Invoke());
                        break;
                }

                OnUiRefresh();
            }
            finally
            {
                OnUiUpdateEnd();
            }
        }


        protected virtual void HandleAdd(NotifyCollectionChangedEventArgs e)
        {
            if (e.NewItems == null) 
                return;
            var insertIndex = e.NewStartingIndex >= 0 ? e.NewStartingIndex : Snapshot.Count;
           
            for (int i = 0; i < e.NewItems.Count; i++)
            {
                var item = e.NewItems[i];
                Snapshot.Insert(insertIndex + i, item);
                OnUiInsert(insertIndex + i, item);
            }
        }

        protected virtual void HandleRemove(NotifyCollectionChangedEventArgs e)
        {
            if (e.OldItems == null) 
                return;
            
            if (e.OldStartingIndex >= 0)
            {
                for (int i = 0; i < e.OldItems.Count; i++)
                {
                    var idx = e.OldStartingIndex;
                    if (idx >= 0 && idx < Snapshot.Count)
                    {
                        Snapshot.RemoveAt(idx);
                        OnUiRemove(idx);
                    }
                }
            }
            else
            {
                // fallback: find by object equality or string
                foreach (var oldItem in e.OldItems)
                {
                    var idx = Snapshot.FindIndex(s => ReferenceEquals(s, oldItem) || ElementToString(s) == ElementToString(oldItem));
                    if (idx >= 0)
                    {
                        Snapshot.RemoveAt(idx);
                        OnUiRemove(idx);
                    }
                }
            }
        }

        protected virtual void HandleReplace(NotifyCollectionChangedEventArgs e)
        {
            if (e.NewItems != null && e.OldItems != null && e.NewStartingIndex >= 0)
            {
                var idx = e.NewStartingIndex;
                for (int i = 0; i < e.NewItems.Count; i++)
                {
                    var pos = idx + i;
                    if (pos < Snapshot.Count)
                    {
                        Snapshot[pos] = e.NewItems[i];
                    }
                    else
                    {
                        Snapshot.Add(e.NewItems[i]);
                    }

                    OnUiReplace(pos, e.NewItems[i]);
                }
            }
            else
            {
                // fallback to rebuild
                RebuildSnapshotAndUi(Getter?.Invoke());
            }
        }

        protected virtual void HandleMove(NotifyCollectionChangedEventArgs e)
        {
            if (e.OldStartingIndex < 0 || e.NewStartingIndex < 0 || e.OldItems == null)
            {
                RebuildSnapshotAndUi(Getter?.Invoke());
                return;
            }

            var oldIndex = e.OldStartingIndex;
            var newIndex = e.NewStartingIndex;
            var count = e.OldItems.Count;

            if (count <= 0 || oldIndex == newIndex) return;

            oldIndex = Math.Max(0, Math.Min(oldIndex, Snapshot.Count));
            newIndex = Math.Max(0, Math.Min(newIndex, Snapshot.Count));

            if (oldIndex + count > Snapshot.Count)
                count = Math.Max(0, Snapshot.Count - oldIndex);
            if (count <= 0) return;

            var moved = Snapshot.GetRange(oldIndex, count);
            Snapshot.RemoveRange(oldIndex, count);

            var insertIndex = newIndex;
            if (newIndex > oldIndex) insertIndex = newIndex - count;
            insertIndex = Math.Max(0, Math.Min(insertIndex, Snapshot.Count));

            Snapshot.InsertRange(insertIndex, moved);
            OnUiMove(oldIndex, insertIndex, count);
        }

        protected abstract void OnUiClear();
        protected abstract void OnUiInsert(int index, object element);
        protected abstract void OnUiRemove(int index);
        protected abstract void OnUiReplace(int index, object element);
        protected abstract void OnUiMove(int oldIndex, int newIndex, int count);
        protected virtual void OnUiRefresh() { }
        protected virtual void OnUiUpdateBegin() { }
        protected virtual void OnUiUpdateEnd() { }


        protected void BuildElementHelpers(object raw)
        {
            ElementType = GetEnumerableElementTypeFromInstance(raw);
            ElementToString = CreateElementConverter(ElementType);
            BuildIndexedAccessors(raw, ElementType);
        }

        private static Type GetEnumerableElementTypeFromInstance(object enumerableInstance)
        {
            if (enumerableInstance == null)
                return null;
            var t = enumerableInstance.GetType();
            if (t.IsArray) 
                return t.GetElementType();
            
            if (t.IsGenericType && t.GetGenericTypeDefinition() == typeof(IEnumerable<>))
                return t.GetGenericArguments()[0];
            
            foreach (var iface in t.GetInterfaces())
            {
                if (iface.IsGenericType && iface.GetGenericTypeDefinition() == typeof(IEnumerable<>))
                    return iface.GetGenericArguments()[0];
            }
            return null;
        }

        private static Func<object, string> CreateElementConverter(Type elementType)
        {
            if (elementType == null) 
                return o => o?.ToString() ?? string.Empty;
            if (elementType == typeof(string)) 
                return o => o as string ?? string.Empty;
            
            if (typeof(IFormattable).IsAssignableFrom(elementType) ||
                typeof(IConvertible).IsAssignableFrom(elementType))
                return o => o?.ToString() ?? string.Empty;
            
            if (elementType.IsEnum) 
                return o => o?.ToString() ?? string.Empty;
            var tc = TypeDescriptor.GetConverter(elementType);
            
            if (tc.CanConvertTo(typeof(string)))
            {
                return o =>
                {
                    if (o == null) 
                        return string.Empty;
                    try
                    {
                        var conv = tc.ConvertTo(o, typeof(string));
                        return conv?.ToString() ?? string.Empty;
                    }
                    catch
                    {
                        return o?.ToString() ?? string.Empty;
                    }
                };
            }
            return o => o?.ToString() ?? string.Empty;
        }

        private void BuildIndexedAccessors(object collectionInstance, Type elementType)
        {
            GetAt = null;
            GetCount = null;
            if (collectionInstance == null) return;

            switch (collectionInstance)
            {
                case IList:
                    GetCount = coll => ((IList)coll).Count;
                    GetAt = (coll, idx) => ((IList)coll)[idx];
                    return;
            }

            var concreteType = collectionInstance.GetType();

            Type listIface = null;
            foreach (var iface in concreteType.GetInterfaces())
            {
                if (!iface.IsGenericType)
                    continue;
                
                var def = iface.GetGenericTypeDefinition();
                
                if (elementType != null && iface.GetGenericArguments()[0] != elementType) 
                    continue;
                
                if (def == typeof(IList<>) || def == typeof(IReadOnlyList<>))
                {
                    listIface = iface;
                    break;
                }
            }

            if (listIface != null && elementType != null)
            {
                var collParam = Expression.Parameter(typeof(object), "coll");
                var castColl = Expression.Convert(collParam, listIface);
                var countProp = listIface.GetProperty("Count");
                
                if (countProp != null)
                {
                    var countExpr = Expression.Property(castColl, countProp);
                    GetCount = Expression.Lambda<Func<object, int>>(countExpr, collParam).Compile();
                }

                var idxParam = Expression.Parameter(typeof(int), "idx");
                var itemProp = listIface.GetProperty("Item");
                
                if (itemProp != null)
                {
                    var itemExpr = Expression.Property(castColl, itemProp, idxParam);
                    var boxed = Expression.Convert(itemExpr, typeof(object));
                    GetAt = Expression.Lambda<Func<object, int, object>>(boxed, collParam, idxParam).Compile();
                }
            }
        }
    }
}
