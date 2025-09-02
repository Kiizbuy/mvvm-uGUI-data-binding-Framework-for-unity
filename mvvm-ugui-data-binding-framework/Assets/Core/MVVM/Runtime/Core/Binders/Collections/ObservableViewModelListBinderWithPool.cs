using System.Collections.Generic;
using UnityEngine;

namespace MVVM.Runtime.Binders
{
    public sealed class ObservableViewModelListBinderWithPool : ObservableViewModelListBinderBase
    {
        private readonly Stack<GameObject> _pool = new();

        [Header("Pool settings")] 
        [SerializeField] private int maxPoolSize = 0; // 0 = infinity
        [SerializeField] private bool preWarmPool = false;
        [SerializeField] private int preWarmCount = 5;

        protected override GameObject CreateItem(object item)
        {
            GameObject go;
            if (_pool.Count > 0)
            {
                go = _pool.Pop();

                if (container != null)
                    go.transform.SetParent(container, false);

                go.SetActive(true);
            }
            else
            {
                go = Instantiate(itemPrefab);
                if (container != null)
                    go.transform.SetParent(container, false);
            }

            var templateVm = go.GetComponent<CollectionViewModelTemplate>();
            if (templateVm != null)
            {
                templateVm.InitChildBindings(item);
            }

            return go;
        }

        private void ReturnToPool(GameObject go)
        {
            if (go == null)
                return;

            go.SetActive(false);

            if (container != null)
                go.transform.SetParent(container, false);

            if (maxPoolSize <= 0 || _pool.Count < maxPoolSize)
            {
                _pool.Push(go);
            }
            else
            {
                Destroy(go);
            }
        }

        protected override void OnUiRemove(int index)
        {
            if (index < 0 || index >= _spawnedItems.Count)
                return;

            var go = _spawnedItems[index];
            ReturnToPool(go);
            _spawnedItems.RemoveAt(index);
        }

        protected override void OnUiReplace(int index, object element)
        {
            if (index >= 0 && index < _spawnedItems.Count)
            {
                var old = _spawnedItems[index];
                if (old != null)
                    ReturnToPool(old);

                var go = CreateItem(element);
                _spawnedItems[index] = go;
                
                if (go == null)
                {
                    return;
                }

                if (container != null)
                {
                    go.transform.SetParent(container, false);
                }

                go.transform.SetSiblingIndex(index);
            }
            else
            {
                var go = CreateItem(element);
                if (go == null)
                {
                    return;
                }

                _spawnedItems.Add(go);
                if (container != null)
                {
                    go.transform.SetParent(container, false);
                }

                go.transform.SetSiblingIndex(_spawnedItems.Count - 1);
            }
        }

        protected override void ClearSpawned()
        {
            foreach (var go in _spawnedItems)
            {
                if (go == null)
                {
                    continue;
                }


                go.SetActive(false);
                if (container != null)
                {
                    go.transform.SetParent(container, false);
                }

                if (maxPoolSize <= 0 || _pool.Count < maxPoolSize)
                {
                    _pool.Push(go);
                }
                else
                {
                    Destroy(go);
                }
            }

            _spawnedItems.Clear();
        }

        private void Start()
        {
            if (preWarmPool && Application.isPlaying)
            {
                PreWarm(preWarmCount);
            }
        }

        private void PreWarm(int count)
        {
            if (itemPrefab == null)
            {
                Debug.LogWarning($"{name}: Cannot prewarm pool — itemPrefab is null.", this);
                return;
            }

            if (count <= 0) return;

            while (_pool.Count < count)
            {
                var go = Instantiate(itemPrefab);
                if (container != null)
                {
                    go.transform.SetParent(container, false);
                }

                go.SetActive(false);

                if (maxPoolSize <= 0 || _pool.Count < maxPoolSize)
                {
                    _pool.Push(go);
                }
                else
                {
                    Destroy(go);
                    break;
                }
            }
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();

            while (_pool.Count > 0)
            {
                var go = _pool.Pop();
                if (go != null)
                {
                    Destroy(go);
                }
            }
        }
    }
}