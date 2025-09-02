using System;
using System.Collections.Generic;
using UnityEngine;

namespace MVVM.Runtime.Binders
{
    public abstract class ObservableViewModelListBinderBase : CollectionBinderBase
    {
        [SerializeField] protected RectTransform container;
        [SerializeField] protected GameObject itemPrefab;

        protected object _listInstance;
        protected readonly List<GameObject> _spawnedItems = new();

        public override void Initialize()
        {
            base.Initialize();

            _listInstance = Getter();
            if (_listInstance == null)
            {
                Debug.LogError($"{name}: ViewModel property '{ViewModelProperty}' is null.", this);
                enabled = false;
                OnUiClear();
                return;
            }

            var listType = _listInstance.GetType();
            if (!listType.IsGenericType || listType.GetGenericTypeDefinition() != typeof(Collections.ObservableDataList<>))
            {
                Debug.LogError($"{name}: Property '{ViewModelProperty}' is not ObservableDataList<T>.", this);
                Disconnect();
                enabled = false;
                OnUiClear();
            }
        }

        protected abstract GameObject CreateItem(object item);

        protected virtual void ClearSpawned()
        {
            for (int i = 0; i < _spawnedItems.Count; i++)
            {
                var go = _spawnedItems[i];
                if (go != null)
                {
                    Destroy(go);
                }
            }

            _spawnedItems.Clear();
        }


        protected override void OnUiClear()
        {
            ClearSpawned();
        }

        protected override void OnUiInsert(int index, object element)
        {
            var go = CreateItem(element);
            if (go == null)
                return;

            var insertIndex = Math.Max(0, Math.Min(index, _spawnedItems.Count));
            _spawnedItems.Insert(insertIndex, go);

            if (container != null)
                go.transform.SetParent(container, false);

            go.transform.SetSiblingIndex(insertIndex);
        }

        protected override void OnUiRemove(int index)
        {
            if (index < 0 || index >= _spawnedItems.Count) 
                return;

            var go = _spawnedItems[index];
            if (go != null)
                Destroy(go);

            _spawnedItems.RemoveAt(index);
        }

        protected override void OnUiReplace(int index, object element)
        {
            if (index >= 0 && index < _spawnedItems.Count)
            {
                var old = _spawnedItems[index];
                if (old != null)
                    Destroy(old);
                
                var go = CreateItem(element);
                
                _spawnedItems[index] = go;
                if (go == null)
                    return;
                
                if (container != null)
                    go.transform.SetParent(container, false);
                
                go.transform.SetSiblingIndex(index);
            }
            else
            {
                var go = CreateItem(element);
                if (go != null)
                {
                    _spawnedItems.Add(go);
                    if (container != null) 
                        go.transform.SetParent(container, false);
                    go.transform.SetSiblingIndex(_spawnedItems.Count - 1);
                }
            }
        }

        protected override void OnUiMove(int oldIndex, int newIndex, int count)
        {
            if (count <= 0) 
                return;

            oldIndex = Math.Max(0, Math.Min(oldIndex, _spawnedItems.Count));
            newIndex = Math.Max(0, Math.Min(newIndex, _spawnedItems.Count));

            var actualCount = Math.Min(count, Math.Max(0, _spawnedItems.Count - oldIndex));
            if (actualCount <= 0) 
                return;

            var moved = _spawnedItems.GetRange(oldIndex, actualCount);
            _spawnedItems.RemoveRange(oldIndex, actualCount);

            var insertIndex = newIndex;
            if (newIndex > oldIndex) 
                insertIndex = newIndex - actualCount;
            insertIndex = Math.Max(0, Math.Min(insertIndex, _spawnedItems.Count));
            _spawnedItems.InsertRange(insertIndex, moved);

            for (int i = 0; i < moved.Count; i++)
            {
                var go = moved[i];
                if (go != null)
                    go.transform.SetSiblingIndex(insertIndex + i);
            }
        }

        protected override void OnUiRefresh()
        {
            // ничего по умолчанию; потомки могут переопределить если нужно (например, LayoutGroup.Refresh)
        }


        protected override void OnDestroy()
        {
            // CollectionBinderBase.OnDestroy() отписывает от коллекции
            base.OnDestroy();
            ClearSpawned();
        }
    }
}
