using UnityEngine;

namespace MVVM.Runtime.Binders
{
    public sealed class ObservableViewModelListBinder : ObservableViewModelListBinderBase
    {
        protected override GameObject CreateItem(object item)
        {
            var go = Instantiate(itemPrefab, container);

            var templateVm = go.GetComponentInChildren<CollectionViewModelTemplate>(true);
            if (templateVm != null)
            {
                templateVm.InitChildBindings(item);
            }

            return go.gameObject;
        }


        protected override void ClearSpawned()
        {
            foreach (var go in _spawnedItems)
            {
                if (go != null)
                {
                    Destroy(go);
                }
            }

            _spawnedItems.Clear();
        }
    }
}