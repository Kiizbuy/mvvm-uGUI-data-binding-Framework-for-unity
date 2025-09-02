using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace MVVM.Runtime.Binders
{
    [AddComponentMenu("MVVM/Binders/Infinite Scroller Binder")]
    public class InfiniteScrollerViewModelListBinder : ObservableViewModelListBinderBase
    {
        [Header("Layout")] [SerializeField] private bool vertical = true;
        [SerializeField] private float itemSize = 100f;
        [SerializeField] private float spacing = 0f;
        [SerializeField] private int bufferItems = 2;

        [Header("View")] [SerializeField] private RectTransform viewport;

        [Header("LayoutGroup support")] [SerializeField]
        private LayoutGroup layoutGroup;

        [SerializeField] private GridLayoutGroup gridLayout;
        [SerializeField] private Transform poolHolder;
        [Header("Pools")] [SerializeField] private int maxPoolSize = 0;
        [SerializeField] private bool preWarmPool = false;
        [SerializeField] private int preWarmCount = 5;

        [Header("Remap settings")]
        [SerializeField, Tooltip("Максимальный размер блока для ремапа. 0 = авто (видимое окно + buffer).")]
        private int remapMaxCount = 0;

        private readonly Stack<GameObject> _pool = new();
        private readonly Dictionary<int, GameObject> _active = new(); // index -> go
        private bool _isUpdating = false;

        private float FullItemPrimary => (UseGrid ? PrimaryGridCellSize : itemSize) + PrimarySpacing;

        private bool UseGrid => gridLayout != null;
        private HorizontalOrVerticalLayoutGroup HVLayout => layoutGroup as HorizontalOrVerticalLayoutGroup;

        private float PrimarySpacing
        {
            get
            {
                if (UseGrid)
                {
                    return (vertical ? gridLayout.spacing.y : gridLayout.spacing.x);
                }

                return HVLayout != null ? HVLayout.spacing : spacing;
            }
        }

        private float PrimaryGridCellSize => vertical
            ? (gridLayout != null ? gridLayout.cellSize.y : itemSize)
            : (gridLayout != null ? gridLayout.cellSize.x : itemSize);

        protected virtual void Start()
        {
            if (preWarmPool && Application.isPlaying)
            {
                PreWarm(preWarmCount);
            }

            var scroll = viewport != null ? viewport.GetComponentInParent<ScrollRect>() : null;
            if (scroll != null)
            {
                scroll.onValueChanged.RemoveListener(OnScrollChanged);
                scroll.onValueChanged.AddListener(OnScrollChanged);
            }

            UpdateContentSize();
            UpdateVisible(true);
        }

        protected virtual void OnDestroy()
        {
            foreach (var kv in _active)
            {
                if (kv.Value != null)
                {
                    Destroy(kv.Value);
                }
            }

            _active.Clear();

            while (_pool.Count > 0)
            {
                var go = _pool.Pop();
                if (go != null)
                {
                    Destroy(go);
                }
            }
        }

        public void PreWarm(int count)
        {
            if (itemPrefab == null)
            {
                return;
            }

            if (count <= 0)
            {
                return;
            }

            while (_pool.Count < count)
            {
                var go = Instantiate(itemPrefab);
                if (poolHolder != null)
                {
                    go.transform.SetParent(poolHolder, false);
                }
                else if (container != null)
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

        private GameObject Rent()
        {
            GameObject go;
            if (_pool.Count > 0)
            {
                go = _pool.Pop();
                if (container != null) go.transform.SetParent(container, false);
                go.SetActive(true);
            }
            else
            {
                go = Instantiate(itemPrefab);
                if (container != null) go.transform.SetParent(container, false);
            }

            return go;
        }

        private void ReturnToPool(GameObject go)
        {
            if (go == null)
            {
                return;
            }

            go.SetActive(false);

            if (poolHolder != null)
            {
                go.transform.SetParent(poolHolder, false);
            }
            else if (container != null)
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

        protected override GameObject CreateItem(object item)
        {
            var go = Rent();
            if (go == null)
            {
                return null;
            }

            var templateVm = go.GetComponent<CollectionViewModelTemplate>();
            if (templateVm != null)
            {
                templateVm.InitChildBindings(item);
            }
            else
            {
                var binder = go.GetComponentInChildren<BinderBase>();
                if (binder != null && item is MonoBehaviour mb)
                {
                    binder.ViewModel = mb;
                }
            }

            return go;
        }

        protected override void OnUiClear()
        {
            foreach (var kv in _active)
            {
                ReturnToPool(kv.Value);
            }

            _active.Clear();

            UpdateContentSize();
        }

        protected override void OnUiInsert(int index, object element)
        {
            if (_active.Count > 0)
            {
                var keys = new List<int>(_active.Keys);
                keys.Sort();
                for (var i = keys.Count - 1; i >= 0; i--)
                {
                    var k = keys[i];
                    if (k >= index)
                    {
                        var go = _active[k];
                        _active.Remove(k);
                        _active[k + 1] = go;
                        if (layoutGroup != null || gridLayout != null)
                        {
                            go.transform.SetSiblingIndex(k + 1);
                        }
                        else
                        {
                            PositionItem(go, k + 1);
                        }
                    }
                }
            }

            UpdateContentSize();
            UpdateVisible();
        }

        protected override void OnUiRemove(int index)
        {
            if (_active.TryGetValue(index, out var go))
            {
                ReturnToPool(go);
                _active.Remove(index);
            }

            if (_active.Count > 0)
            {
                var keys = new List<int>(_active.Keys);
                keys.Sort();
                for (var i = 0; i < keys.Count; i++)
                {
                    var k = keys[i];
                    if (k > index)
                    {
                        var g = _active[k];
                        _active.Remove(k);
                        var newIdx = k - 1;
                        _active[newIdx] = g;
                        
                        if (layoutGroup != null || gridLayout != null)
                        {
                            g.transform.SetSiblingIndex(newIdx);
                        }
                        else
                        {
                            PositionItem(g, newIdx);
                        }
                    }
                }
            }

            UpdateContentSize();
            UpdateVisible();
        }

        protected override void OnUiReplace(int index, object element)
        {
            if (_active.TryGetValue(index, out var go) && go != null)
            {
                var templateVm = go.GetComponent<CollectionViewModelTemplate>();
                if (templateVm != null)
                {
                    templateVm.InitChildBindings(element);
                }
            }
        }

        protected override void OnUiMove(int oldIndex, int newIndex, int count)
        {
            if (count <= 0 || oldIndex < 0 || newIndex < 0)
            {
                UpdateContentSize();
                UpdateVisible();
                return;
            }

            var total = Snapshot.Count;
            if (oldIndex >= total || newIndex >= total || oldIndex + count > total)
            {
                UpdateContentSize();
                UpdateVisible();
                return;
            }

            int threshold;
            if (remapMaxCount > 0)
            {
                threshold = remapMaxCount;
            }
            else
            {
                var viewSize = (vertical ? viewport.rect.height : viewport.rect.width);
                var visibleCountApprox = Mathf.CeilToInt(viewSize / FullItemPrimary);
                threshold = visibleCountApprox + bufferItems;
            }

            if (count > threshold)
            {
                UpdateContentSize();
                UpdateVisible();
                return;
            }

            RemapActiveIndices(oldIndex, newIndex, count);
            UpdateContentSize();
            UpdateVisible();
        }

        protected override void OnUiRefresh()
        {
            UpdateContentSize();
            UpdateVisible();
        }

        private void RemapActiveIndices(int oldIndex, int newIndex, int count)
        {
            if (count <= 0)
            {
                return;
            }

            var total = Snapshot.Count;
            var newActive = new Dictionary<int, GameObject>(Mathf.Max(16, _active.Count));

            foreach (var kv in _active)
            {
                var i = kv.Key;
                var go = kv.Value;
                int mapped;

                if (i >= oldIndex && i < oldIndex + count)
                {
                    mapped = newIndex + (i - oldIndex);
                }
                else if (oldIndex < newIndex)
                {
                    var oldEnd = oldIndex + count - 1;
                    mapped = i > oldEnd && i <= newIndex ? i - count : i;
                }
                else if (newIndex < oldIndex)
                {
                    mapped = i >= newIndex && i < oldIndex ? i + count : i;
                }
                else
                {
                    mapped = i;
                }

                if (mapped < 0 || mapped >= total)
                {
                    ReturnToPool(go);
                    continue;
                }

                if (newActive.TryGetValue(mapped, out _))
                {
                    // collision — return current
                    ReturnToPool(go);
                    continue;
                }

                // reposition: for layout groups use siblingIndex, otherwise manual PositionItem
                if (layoutGroup != null || gridLayout != null)
                {
                    go.transform.SetSiblingIndex(mapped);
                }
                else
                {
                    PositionItem(go, mapped);
                }

                newActive[mapped] = go;
            }

            _active.Clear();
            foreach (var (key, value) in newActive)
            {
                _active[key] = value;
            }
        }

        private void OnScrollChanged(Vector2 _)
        {
            UpdateVisible();
        }

        private void UpdateContentSize()
        {
            if (container == null) return;

            var total = Snapshot.Count;

            if (UseGrid)
            {
                var cols = GetGridColumns();
                var rows = Mathf.CeilToInt((float) total / Mathf.Max(1, cols));
                var pad = gridLayout.padding;
                var spacing = gridLayout.spacing;
                var width = pad.left + pad.right + cols * gridLayout.cellSize.x + Math.Max(0, cols - 1) * spacing.x;
                var height = pad.top + pad.bottom + rows * gridLayout.cellSize.y + Math.Max(0, rows - 1) * spacing.y;

                var size = container.sizeDelta;
                size.x = width;
                size.y = height;
                container.sizeDelta = size;
            }
            else if (HVLayout != null)
            {
                // For Horizontal/VerticalLayoutGroup we control primary axis size via itemSize + spacing * count, but actual layout can change.
                var totalPrimary = total <= 0 ? 0f : total * (itemSize + HVLayout.spacing) - HVLayout.spacing;
                var size = container.sizeDelta;
                if (vertical) size.y = totalPrimary;
                else size.x = totalPrimary;
                container.sizeDelta = size;
            }
            else
            {
                var totalPrimary = total <= 0 ? 0f : total * FullItemPrimary - PrimarySpacing;
                var size = container.sizeDelta;
                if (vertical) size.y = totalPrimary;
                else size.x = totalPrimary;
                container.sizeDelta = size;
            }
        }

        private int GetGridColumns()
        {
            if (gridLayout == null)
            {
                return 1;
            }
            if (gridLayout.constraint == GridLayoutGroup.Constraint.FixedColumnCount)
            {
                return Mathf.Max(1, gridLayout.constraintCount);
            }

            var cellW = gridLayout.cellSize.x + gridLayout.spacing.x;
            
            if (cellW <= 0)
            {
                return 1;
            }

            var width = Mathf.Max(1f, container.rect.width);
            return Mathf.Max(1, Mathf.FloorToInt((width + gridLayout.spacing.x) / cellW));
        }

        private void UpdateVisible(bool force = false)
        {
            if (_isUpdating)
            {
                return;
            }

            if (container == null || viewport == null || itemPrefab == null)
            {
                return;
            }

            _isUpdating = true;
            try
            {
                var total = Snapshot.Count;
                if (total == 0)
                {
                    foreach (var kv in _active) ReturnToPool(kv.Value);
                    _active.Clear();
                    
                    if (layoutGroup != null || gridLayout != null)
                    {
                        LayoutRebuilder.MarkLayoutForRebuild(container);
                    }

                    return;
                }

                CalculateVisibleIndexRange(out var first, out var last);

                if (first > last || first >= total || last < 0)
                {
                    foreach (var kv in _active)
                    {
                        ReturnToPool(kv.Value);
                    }

                    _active.Clear();
                    if (layoutGroup != null || gridLayout != null)
                    {
                        LayoutRebuilder.MarkLayoutForRebuild(container);
                    }

                    return;
                }

                first = Mathf.Max(0, first);
                last = Mathf.Min(total - 1, last);

                var desired = new HashSet<int>();
                for (var i = first; i <= last; i++)
                {
                    desired.Add(i);
                }

                var toRemove = new List<int>();
                foreach (var kv in _active)
                {
                    if (!desired.Contains(kv.Key))
                    {
                        toRemove.Add(kv.Key);
                    }
                }

                foreach (var idx in toRemove)
                {
                    var go = _active[idx];
                    ReturnToPool(go);
                    _active.Remove(idx);
                }

                for (var idx = first; idx <= last; idx++)
                {
                    if (_active.ContainsKey(idx))
                    {
                        continue;
                    }

                    var element = idx < Snapshot.Count ? Snapshot[idx] : null;
                    if (element == null)
                    {
                        continue;
                    }

                    var go = Rent();

                    if (layoutGroup != null || gridLayout != null)
                    {
                        // let layout group handle positioning; set siblingIndex to index
                        go.transform.SetParent(container, false);
                        go.transform.SetSiblingIndex(Mathf.Clamp(idx, 0, container.childCount));
                    }
                    else
                    {
                        // manual positioning
                        PositionItem(go, idx);
                    }

                    var templateVm = go.GetComponent<CollectionViewModelTemplate>();
                    if (templateVm != null)
                    {
                        templateVm.InitChildBindings(element);
                    }
                    else
                    {
                        var binder = go.GetComponentInChildren<BinderBase>();
                        if (binder != null && element is MonoBehaviour mb)
                        {
                            binder.ViewModel = mb;
                        }
                    }

                    _active[idx] = go;
                }

                // If we used a LayoutGroup, signal a rebuild once
                if (layoutGroup != null || gridLayout != null)
                {
                    LayoutRebuilder.MarkLayoutForRebuild(container);
                }
            }
            finally
            {
                _isUpdating = false;
            }
        }

        private void CalculateVisibleIndexRange(out int firstIndex, out int lastIndex)
        {
            firstIndex = 0;
            lastIndex = -1;
            var total = Snapshot.Count;
            
            if (total == 0)
            {
                return;
            }

            if (UseGrid)
            {
                var cols = GetGridColumns();
                var primaryStep = gridLayout.cellSize.y + gridLayout.spacing.y;
                var offset = container.anchoredPosition.y;
                if (offset < 0)
                {
                    offset = 0;
                }

                var viewSize = viewport.rect.height;

                var firstRow = Mathf.FloorToInt(offset / primaryStep) - bufferItems;
                var lastRow = Mathf.CeilToInt((offset + viewSize) / primaryStep) + bufferItems - 1;

                firstRow = Mathf.Max(0, firstRow);
                lastRow = Mathf.Min(Mathf.CeilToInt((float) total / cols) - 1, lastRow);

                firstIndex = firstRow * cols;
                lastIndex = Mathf.Min(total - 1, (lastRow + 1) * cols - 1);
            }
            else if (HVLayout != null)
            {
                var primaryStep = itemSize + HVLayout.spacing;
                var offset = vertical ? container.anchoredPosition.y : -container.anchoredPosition.x;
                if (offset < 0)
                {
                    offset = 0;
                }

                var viewSize = vertical ? viewport.rect.height : viewport.rect.width;

                var first = Mathf.FloorToInt(offset / primaryStep) - bufferItems;
                var last = Mathf.CeilToInt((offset + viewSize) / primaryStep) + bufferItems - 1;
                firstIndex = Mathf.Max(0, first);
                lastIndex = Mathf.Min(total - 1, last);
            }
            else
            {
                // manual fallback (same as earlier)
                var primaryStep = FullItemPrimary;
                var offset = vertical ? container.anchoredPosition.y : -container.anchoredPosition.x;
                if (offset < 0)
                {
                    offset = 0;
                }

                var viewSize = vertical ? viewport.rect.height : viewport.rect.width;

                var first = Mathf.FloorToInt(offset / primaryStep) - bufferItems;
                var last = Mathf.CeilToInt((offset + viewSize) / primaryStep) + bufferItems - 1;
                firstIndex = Mathf.Max(0, first);
                lastIndex = Mathf.Min(total - 1, last);
            }
        }

        private void PositionItem(GameObject go, int index)
        {
            if (go == null || container == null)
            {
                return;
            }

            var rt = go.transform as RectTransform;
            
            if (rt == null)
            {
                return;
            }

            if (UseGrid)
            {
                // grid handled by LayoutGroup; we shouldn't be here normally
                go.transform.SetParent(container, false);
                go.transform.SetSiblingIndex(Mathf.Clamp(index, 0, container.childCount));
                return;
            }

            if (HVLayout != null)
            {
                // place along primary axis; set full width/height on orthogonal axis
                if (vertical)
                {
                    var anchored = rt.anchoredPosition;
                    anchored.x = 0;
                    anchored.y = -index * FullItemPrimary;
                    rt.anchorMin = new Vector2(0, 1);
                    rt.anchorMax = new Vector2(1, 1);
                    rt.pivot = new Vector2(0, 1);
                    rt.sizeDelta = new Vector2(rt.sizeDelta.x == 0 ? container.rect.width : rt.sizeDelta.x, itemSize);
                    rt.anchoredPosition = anchored;
                }
                else
                {
                    var anchored = rt.anchoredPosition;
                    anchored.y = 0;
                    anchored.x = index * FullItemPrimary;
                    rt.anchorMin = new Vector2(0, 1);
                    rt.anchorMax = new Vector2(0, 1);
                    rt.pivot = new Vector2(0, 1);
                    rt.sizeDelta = new Vector2(itemSize, rt.sizeDelta.y == 0 ? container.rect.height : rt.sizeDelta.y);
                    rt.anchoredPosition = anchored;
                }
            }
            else
            {
                // fully manual as earlier
                if (vertical)
                {
                    var anchored = rt.anchoredPosition;
                    anchored.x = 0;
                    anchored.y = -index * FullItemPrimary;
                    rt.anchorMin = new Vector2(0, 1);
                    rt.anchorMax = new Vector2(1, 1);
                    rt.pivot = new Vector2(0, 1);
                    rt.sizeDelta = new Vector2(rt.sizeDelta.x == 0 ? container.rect.width : rt.sizeDelta.x, itemSize);
                    rt.anchoredPosition = anchored;
                }
                else
                {
                    var anchored = rt.anchoredPosition;
                    anchored.y = 0;
                    anchored.x = index * FullItemPrimary;
                    rt.anchorMin = new Vector2(0, 1);
                    rt.anchorMax = new Vector2(0, 1);
                    rt.pivot = new Vector2(0, 1);
                    rt.sizeDelta = new Vector2(itemSize, rt.sizeDelta.y == 0 ? container.rect.height : rt.sizeDelta.y);
                    rt.anchoredPosition = anchored;
                }
            }
        }
    }
}