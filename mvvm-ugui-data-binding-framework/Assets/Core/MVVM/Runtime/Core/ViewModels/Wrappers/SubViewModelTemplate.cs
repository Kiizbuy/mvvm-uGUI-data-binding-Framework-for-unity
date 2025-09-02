using System;
using System.Linq.Expressions;
using System.Reflection;
using UnityEngine;
using MVVM.Runtime.Core;
using MVVM.Runtime.Binders;

namespace MVVM.Runtime.Binders
{
    [AddComponentMenu("MVVM/Binders/SubViewModelTemplate")]
    public sealed class SubViewModelTemplate : MonoBehaviour, IViewModelProvider, IViewModelTemplate
    {
        [Tooltip("Имя свойства или поля в корневом ViewModel, которое содержит вложенный ViewModel.'")]
        [SerializeField] private string viewModelPropertyName = string.Empty;

        [Tooltip("Опционально: ссылка на явный провайдер корневого VM. Если не задано — будет найден ближайший IViewModelProvider или ViewModelMonoBehaviour в предках.")]
        public MonoBehaviour rootProviderMono;

        // кеш для быстрого доступа к вложенной VM
        private Func<object, object> _cachedGetter;
        private Type _cachedRootType;
        private object _lastRootInstance; // для детекции смены root

        public string ViewModelPropertyName
        {
            get => viewModelPropertyName;
            set
            {
                if (viewModelPropertyName == value) 
                    return;
                viewModelPropertyName = value;
                InvalidateCache();
            }
        }

        public MonoBehaviour RootProviderMono
        {
            get => rootProviderMono;
            set
            {
                if (rootProviderMono == value) return;
                rootProviderMono = value;
                InvalidateCache();
            }
        }

        public object GetViewModel()
        {
            object root = null;

            if (rootProviderMono is ViewModelMonoBehaviour vmMb)
            {
                root = vmMb;
            }

            if (root == null)
                return null;

            // --- fast path ---
            if (ReferenceEquals(_lastRootInstance, root) && _cachedGetter != null)
            {
                try
                {
                    return _cachedGetter(root);
                }
                catch (Exception ex)
                {
                    HandleError(ex);
                    return null;
                }
            }

            // --- slow path (rebuild cache) ---
            _lastRootInstance = root;
            var rootType = root.GetType();

            if (_cachedGetter == null || _cachedRootType != rootType)
            {
                BuildGetterForRootType(rootType);
            }

            try
            {
                return _cachedGetter?.Invoke(root);
            }
            catch (Exception ex)
            {
                HandleError(ex);
                return null;
            }
        }

        private void HandleError(Exception ex)
        {
            Debug.LogError($"SubViewModelTemplate: cached getter failed for '{viewModelPropertyName}' on {_cachedRootType}");
            Debug.LogException(ex);
        }

        private void BuildGetterForRootType(Type rootType)
        {
            InvalidateCache();

            if (string.IsNullOrEmpty(viewModelPropertyName))
                return;

            var prop = rootType.GetProperty(viewModelPropertyName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (prop != null)
            {
                _cachedRootType = rootType;
                _cachedGetter = CompileGetterForProperty(rootType, prop);
                return;
            }

            var field = rootType.GetField(viewModelPropertyName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (field != null)
            {
                _cachedRootType = rootType;
                _cachedGetter = CompileGetterForField(rootType, field);
                return;
            }

            Debug.LogWarning($"SubViewModelTemplate: member '{viewModelPropertyName}' not found on {rootType}");
        }

        private static Func<object, object> CompileGetterForProperty(Type rootType, PropertyInfo prop)
        {
            var rootParam = Expression.Parameter(typeof(object), "root");
            var casted = Expression.Convert(rootParam, rootType);
            var propAccess = Expression.Property(casted, prop);
            var convertResult = Expression.Convert(propAccess, typeof(object));
            return Expression.Lambda<Func<object, object>>(convertResult, rootParam).Compile();
        }

        private static Func<object, object> CompileGetterForField(Type rootType, FieldInfo field)
        {
            var rootParam = Expression.Parameter(typeof(object), "root");
            var casted = Expression.Convert(rootParam, rootType);
            var fieldAccess = Expression.Field(casted, field);
            var convertResult = Expression.Convert(fieldAccess, typeof(object));
            return Expression.Lambda<Func<object, object>>(convertResult, rootParam).Compile();
        }

        private void InvalidateCache()
        {
            _cachedGetter = null;
            _cachedRootType = null;
            _lastRootInstance = null;
        }

        public void InitChildBindings(object viewModel)
        {
            var binders = GetComponentsInChildren<BinderBase>(true);
            foreach (var b in binders)
            {
                b.ViewModel = this;
                b.Initialize();
            }
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            InvalidateCache();
        }

        private void Reset()
        {
            if (rootProviderMono != null)
                return;
            
            var p = GetComponentInParent<MonoBehaviour>(true);
            if (p is IViewModelProvider or ViewModelMonoBehaviour)
                rootProviderMono = p;
        }
#endif
    }
}
