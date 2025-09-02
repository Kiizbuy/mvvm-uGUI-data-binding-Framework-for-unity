using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEngine.UI;
using MVVM.Runtime.Core;
using UnityEngine.Scripting;

namespace MVVM.Runtime.Binders
{
    [AddComponentMenu("MVVM/Binders/Command Binder")]
    [RequireComponent(typeof(Button))]
    public sealed class CommandBinder : MonoBehaviour
    {
        [Preserve]
        private static readonly Dictionary<(Type, string), MethodInfo> _methodCache = new();
        
        public MonoBehaviour ViewModel;
        public string MethodName;

        private Button _button;
        private Action _cachedAction;

        private void Awake()
        {
            CacheAction();
            _button = GetComponent<Button>();
            if (_button == null)
            {
                Debug.LogError($"{name}: Button component not found.", this);
                enabled = false;
                return;
            }

            _button.onClick.AddListener(OnClicked);
        }

        private void OnDestroy()
        {
            if (_button != null)
                _button.onClick.RemoveListener(OnClicked);
        }

        private void OnClicked()
        {
            _cachedAction?.Invoke();
        }

        private void CacheAction()
        {
            var vmInstance = ResolveActualViewModelInstance();
            if (vmInstance == null)
            {
                Debug.LogWarning($"{name}: ViewModel instance is null — cannot bind '{MethodName}'.", this);
                _cachedAction = null;
                return;
            }
            
            _cachedAction = CreateAction(vmInstance, MethodName);
            if (_cachedAction == null)
            {
                Debug.LogError(
                    $"{name}: No parameterless [Bindable] method '{MethodName}' found on {vmInstance.GetType()}", this);
            }
        }

        private object ResolveActualViewModelInstance()
        {
            if (ViewModel == null)
                return null;

            if (ViewModel is IViewModelProvider provider)
                return provider.GetViewModel();

            return ViewModel;
        }

        private static Action CreateAction(object target, string methodName)
        {
            if (string.IsNullOrEmpty(methodName) || target == null)
            {
                Debug.LogError($"Create action - target is null {target is null} | method name is empty {string.IsNullOrEmpty(methodName)}");                
                return null;
            }

            var targetType = target.GetType();
            var key = (targetType, methodName);

            var method = GetOrCreateBindableMethodInfo(methodName, key, targetType);

            if (method == null)
                return null;
            try
            {
                return (Action)Delegate.CreateDelegate(typeof(Action), target, method);
            }
            catch (Exception ex)
            {
                Debug.LogError($"Failed to create delegate for {methodName} on {target}: {ex}");
                return null;
            }
        }

        private static MethodInfo GetOrCreateBindableMethodInfo(string methodName, (Type targetType, string methodName) key, Type targetType)
        {
            if (_methodCache.TryGetValue(key, out var method))
                return method;

            var cur = targetType;
            while (cur != null)
            {
                var candidate = cur.GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly)
                    .FirstOrDefault(m => string.Equals(m.Name, methodName, StringComparison.Ordinal)
                                         && m.ReturnType == typeof(void)
                                         && m.GetParameters().Length == 0
                                         && m.GetCustomAttribute<BindableAttribute>() != null);
                if (candidate != null)
                {
                    method = candidate;
                    break;
                }
                cur = cur.BaseType;
            }

            if (method != null)
            {
                _methodCache[key] = method;
            }
            else
            {
                Debug.Log($"Not found method with key {methodName} on type {targetType}. Searched type hierarchy.");
            }

            return method;
        }

    }
}