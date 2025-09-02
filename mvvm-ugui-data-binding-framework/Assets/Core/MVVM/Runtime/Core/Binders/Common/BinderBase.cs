using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq.Expressions;
using System.Reflection;
using MVVM.Runtime.Converters;
using UnityEngine;
using UnityEngine.Profiling;
using UnityEngine.Scripting;
using Debug = UnityEngine.Debug;

namespace MVVM.Runtime.Binders
{
    public abstract class BinderBase : MonoBehaviour
    {
        public MonoBehaviour ViewModel;
        public string ViewModelProperty;
        public ValueConverter Converter;

        protected Func<object> Getter;
        protected Action<object> Setter;

        [Preserve]
        private static readonly Dictionary<(Type, string), PropertyInfo> _propertyCache = new();

        private INotifyPropertyChanged _subscribedNpc;

        protected virtual void Awake()
        {
            TryInitialize();
        }

        private void TryInitialize()
        {
            if (ViewModel != null && ViewModel is not CollectionViewModelTemplate)
                Initialize();
        }

        public virtual void Initialize()
        {
            if (string.IsNullOrEmpty(ViewModelProperty))
            {
                Debug.LogWarning($"{name}: ViewModelProperty not set.", this);
            }

            if (ViewModel == null)
            {
                Debug.LogError($"{name}: ViewModel not set.", this);
                enabled = false;
                return;
            }

            var actualViewModel = GetActualViewModel();
            if (actualViewModel == null)
            {
                Debug.LogError($"{name}: Actual ViewModel instance is null.", this);
                enabled = false;
                return;
            }

            var vmType = actualViewModel.GetType();
            var key = (vmType, ViewModelProperty);

            if (!TryGetPropertyData(key, vmType, out var propInfo)) 
                return;

            CompileDelegates(propInfo, vmType);
            VerifyTypeCompatibility(propInfo);
            Connect();
        }

        private bool TryGetPropertyData((Type vmType, string ViewModelProperty) key, Type vmType, out PropertyInfo propInfo)
        {
            if (_propertyCache.TryGetValue(key, out propInfo))
                return true;
            
            propInfo = vmType.GetProperty(ViewModelProperty,
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (propInfo == null)
            {
                Debug.LogError($"{name}: Property '{ViewModelProperty}' not found in {vmType}.", this);
                enabled = false;
                return false;
            }

            _propertyCache[key] = propInfo;
            return true;

        }

        public void Connect()
        {
            var actualViewModel = GetActualViewModel();
            Disconnect();

            if (actualViewModel is INotifyPropertyChanged notifier)
            {
                notifier.PropertyChanged -= OnViewModelPropertyChanged;
                notifier.PropertyChanged += OnViewModelPropertyChanged;
                _subscribedNpc = notifier;

                OnViewModelValueChanged();
            }
        }

        public void Disconnect()
        {
            if (_subscribedNpc != null)
            {
                _subscribedNpc.PropertyChanged -= OnViewModelPropertyChanged;
                _subscribedNpc = null;
            }
        }

        private void CompileDelegates(PropertyInfo propInfo, Type vmType)
        {
            Profiler.BeginSample("Compile delegates");

            // basic validations
            if (propInfo == null) 
                throw new ArgumentNullException(nameof(propInfo));
            // indexers are not supported by this binder
            
            if (propInfo.GetIndexParameters().Length > 0)
            {
                Debug.LogError($"{name}: Property '{propInfo.Name}' is an indexer — not supported.", this);
                Getter = () => null;
                Setter = null;
                Profiler.EndSample();
                return;
            }

            var getActualVmMethod = typeof(BinderBase).GetMethod(nameof(GetActualViewModel),
                BindingFlags.Instance | BindingFlags.NonPublic);
            var thisConst = Expression.Constant(this);
            var callGetActual = Expression.Call(thisConst, getActualVmMethod); // returns object

            // convert(object) -> vmType
            var instanceExpr = Expression.Convert(callGetActual, vmType);

            // property access on (vmType) instance
            var propertyExpr = Expression.Property(instanceExpr, propInfo);

            // GETTER
            if (propInfo.CanRead && propInfo.GetGetMethod(true) != null)
            {
                try
                {
                    // guard: vm == null ? null : (object)prop
                    var nullObj = Expression.Constant(null, typeof(object));
                    var guarded = Expression.Condition(
                        Expression.Equal(callGetActual, Expression.Constant(null, typeof(object))),
                        nullObj,
                        Expression.Convert(propertyExpr, typeof(object))
                    );

                    var getterLambda = Expression.Lambda<Func<object>>(guarded);
                    try
                    {
                        Getter = getterLambda.Compile();
                    }
                    catch (Exception compileEx) when (
                        compileEx is PlatformNotSupportedException or NotSupportedException || 
                        compileEx.GetType().Name.Contains("ExecutionEngineException") ||
                        compileEx is InvalidOperationException)
                    {
                        Debug.LogWarning(
                            $"{name}: Expression.Compile() for getter failed -> using reflection fallback. ({compileEx.GetType().Name})",
                            this);
                        Getter = () =>
                        {
                            var vm = GetActualViewModel();
                            return vm == null ? null : propInfo.GetValue(vm);
                        };
                    }
                }
                catch (Exception ex)
                {
                    Debug.LogWarning(
                        $"{name}: Failed to build getter expression; using reflection fallback. ({ex.GetType().Name})",
                        this);
                    Getter = () =>
                    {
                        var vm = GetActualViewModel();
                        return vm == null ? null : propInfo.GetValue(vm);
                    };
                }
            }
            else
            {
                // no getter available
                Debug.LogWarning($"no getter available for {propInfo.Name}", gameObject);
                Getter = () => null;
            }

            // SETTER
            if (propInfo.CanWrite && propInfo.GetSetMethod(true) != null)
            {
                try
                {
                    var valueParam = Expression.Parameter(typeof(object), "value");
                    var convertedValue = Expression.Convert(valueParam, propInfo.PropertyType);
                    var assignExpr = Expression.Assign(propertyExpr, convertedValue);

                    // guard: vm == null -> no-op
                    // but since propertyExpr already refers to instanceExpr which uses callGetActual,
                    // the assignment will NRE if vm == null. We create a block with a guard.
                    var setNull = Expression.Empty();
                    var setBody = Expression.IfThen(
                        Expression.NotEqual(callGetActual, Expression.Constant(null, typeof(object))),
                        assignExpr
                    );

                    var setterLambda = Expression.Lambda<Action<object>>(setBody, valueParam);

                    try
                    {
                        Setter = setterLambda.Compile();
                    }
                    catch (Exception compileEx) when (
                        compileEx is PlatformNotSupportedException 
                            or NotSupportedException 
                        || compileEx.GetType().Name.Contains("ExecutionEngineException") 
                        || compileEx is InvalidOperationException)
                    {
                        Debug.LogWarning(
                            $"{name}: Expression.Compile() for setter failed -> using reflection fallback. ({compileEx.GetType().Name})",
                            this);
                        Setter = (obj) =>
                        {
                            var vm = GetActualViewModel();
                            if (vm == null) return;
                            propInfo.SetValue(vm, obj);
                        };
                    }
                }
                catch (Exception ex)
                {
                    Debug.LogWarning(
                        $"{name}: Failed to build setter expression; using reflection fallback. ({ex.GetType().Name})",
                        this);
                    Setter = (obj) =>
                    {
                        var vm = GetActualViewModel();
                        if (vm == null)
                            return;
                        propInfo.SetValue(vm, obj);
                    };
                }
            }
            else
            {
                Setter = null;
            }

            Profiler.EndSample();
        }


        private object GetActualViewModel()
        {
            if (ViewModel == null)
                return null;

            if (ViewModel is IViewModelProvider provider)
                return provider.GetViewModel();

            return ViewModel;
        }

        [Conditional("UNITY_EDITOR")]
        private void VerifyTypeCompatibility(PropertyInfo propInfo)
        {
            var baseType = GetType().BaseType;
            if (baseType is not {IsGenericType: true})
                return;

            var genericArgs = baseType.GetGenericArguments(); // [TSource, TTarget]
            var expectedSource = genericArgs[0]; // Type, which binder expected from VM

            if (expectedSource.IsAssignableFrom(propInfo.PropertyType)) return;

            if (Converter != null)
                return;

            var canConvert = false;
            try
            {
                var dummy = GetDefault(propInfo.PropertyType);
                Convert.ChangeType(dummy, expectedSource);
                canConvert = true;
            }
            catch
            {
                // ignored
            }

            if (!canConvert)
            {
                Debug.LogError($"{name}: Property '{propInfo.Name}' has type {propInfo.PropertyType}, " +
                               $"but the binder expects {expectedSource}. " +
                               $"Assign a suitable ValueConverter in the Converter field.", this);
            }
        }

        private static object GetDefault(Type t) => t.IsValueType ? Activator.CreateInstance(t) : null;

        private void OnViewModelPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == ViewModelProperty || e.PropertyName == string.Empty)
                OnViewModelValueChanged();
        }

        protected abstract void OnViewModelValueChanged();

        protected virtual void OnDestroy()
        {
            Disconnect();
        }

        protected static T CastValue<T>(object raw)
        {
            switch (raw)
            {
                case null:
                    return default;
                case T t:
                    return t;
                case IConvertible:
                    return (T) Convert.ChangeType(raw, typeof(T));
            }

            var tc = TypeDescriptor.GetConverter(typeof(T));
            if (tc.CanConvertFrom(raw.GetType()))
                return (T) tc.ConvertFrom(raw);

            throw new InvalidCastException($"Cannot cast value of type {raw.GetType()} to {typeof(T)}");
        }

        protected static TSource ConvertBackValue<TSource, TTarget>(TTarget target)
        {
            switch (target)
            {
                case TSource s:
                    return s;
                case IConvertible:
                    return (TSource) Convert.ChangeType(target, typeof(TSource));
            }

            var tc = TypeDescriptor.GetConverter(typeof(TSource));
            if (tc.CanConvertFrom(target.GetType()))
                return (TSource) tc.ConvertFrom(target);

            throw new InvalidCastException($"Cannot convert back from {typeof(TTarget)} to {typeof(TSource)}");
        }

        protected TTarget ConvertValue<TSource, TTarget>(TSource source)
        {
            if (source == null)
            {
                Debug.LogWarning($"Source {ViewModelProperty} is null {gameObject.name}", gameObject);
                return default;
            }

            switch (source)
            {
                case TTarget t:
                    return t;
                case IConvertible:
                    return (TTarget) Convert.ChangeType(source, typeof(TTarget));
            }

            var tc = TypeDescriptor.GetConverter(typeof(TTarget));
            if (tc.CanConvertFrom(source.GetType()))
                return (TTarget) tc.ConvertFrom(source);

            throw new InvalidCastException($"Cannot convert {typeof(TSource)} to {typeof(TTarget)}");
        }
    }
}