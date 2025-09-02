using System;
using System.ComponentModel;
using System.Linq.Expressions;
using System.Reflection;
using TMPro;
using UnityEngine;

namespace MVVM.Runtime.Binders
{
    [AddComponentMenu("MVVM/Binders/Dropdown List Binder")]
    [RequireComponent(typeof(TMP_Dropdown))]
    public sealed class DropdownListBinder : CollectionBinderBase
    {
        [SerializeField] public TMP_Dropdown TargetDropdown;

        [Header("Property to write selected item into (on actual ViewModel)")]
        public string SelectedProperty;

        private Func<object> _selectedGetter;
        private Action<object> _selectedSetter;
        private INotifyPropertyChanged _subscribedNpcForSelectedProp;
        private bool _isUpdatingFromDropdown;
        private bool _isUpdatingFromViewModel;

        protected override void Awake()
        {
            base.Awake();
            SetupSelectedPropertyDelegates();
            AttachDropdownListener();
        }

        public override void Initialize()
        {
            base.Initialize();
            SetupSelectedPropertyDelegates();
            AttachDropdownListener();
            UpdateDropdownSelectionFromVm();
        }

        protected override void OnDestroy()
        {
            DetachDropdownListener();
            UnsubscribeSelectedPropertyNpc();
            base.OnDestroy();
        }

        private void AttachDropdownListener()
        {
            if (TargetDropdown == null)
                return;
            
            TargetDropdown.onValueChanged.RemoveListener(OnDropdownValueChanged);
            TargetDropdown.onValueChanged.AddListener(OnDropdownValueChanged);
        }

        private void DetachDropdownListener()
        {
            if (TargetDropdown == null) 
                return;
            
            TargetDropdown.onValueChanged.RemoveListener(OnDropdownValueChanged);
        }
        
        protected override void OnUiUpdateBegin()
        {
            _isUpdatingFromViewModel = true;
        }

        protected override void OnUiUpdateEnd()
        {
            _isUpdatingFromViewModel = false;
        }

        protected override void OnUiClear()
        {
            if (TargetDropdown == null) return;
            TargetDropdown.options.Clear();
            TargetDropdown.value = 0;
            TargetDropdown.RefreshShownValue();
        }

        protected override void OnUiInsert(int index, object element)
        {
            if (TargetDropdown == null)
                return;

            var text = ElementToString?.Invoke(element) ?? element?.ToString() ?? string.Empty;
            var opt = new TMP_Dropdown.OptionData(text);
            var opts = TargetDropdown.options;
            var insertIndex = Mathf.Clamp(index, 0, opts.Count);

            opts.Insert(insertIndex, opt);
        }

        protected override void OnUiRemove(int index)
        {
            if (TargetDropdown == null)
                return;
            var opts = TargetDropdown.options;

            if (index >= 0 && index < opts.Count)
                opts.RemoveAt(index);
        }

        protected override void OnUiReplace(int index, object element)
        {
            if (TargetDropdown == null)
                return;
            var opts = TargetDropdown.options;
            var text = ElementToString?.Invoke(element) ?? element?.ToString() ?? string.Empty;
            var opt = new TMP_Dropdown.OptionData(text);

            switch (index)
            {
                case >= 0 when index < opts.Count:
                    opts[index] = opt;
                    break;
                default:
                    opts.Add(opt);
                    break;
            }
        }

        protected override void OnUiMove(int oldIndex, int newIndex, int count)
        {
            if (TargetDropdown == null)
                return;
            var opts = TargetDropdown.options;

            oldIndex = Mathf.Clamp(oldIndex, 0, opts.Count);
            newIndex = Mathf.Clamp(newIndex, 0, opts.Count);
            
            if (count <= 0 || oldIndex == newIndex)
                return;

            var actualCount = Math.Min(count, Math.Max(0, opts.Count - oldIndex));
            
            if (actualCount <= 0)
                return;

            var moved = opts.GetRange(oldIndex, actualCount);
            opts.RemoveRange(oldIndex, actualCount);

            var insertIndex = newIndex;
            if (newIndex > oldIndex)
                insertIndex = newIndex - actualCount;

            insertIndex = Mathf.Clamp(insertIndex, 0, opts.Count);
            opts.InsertRange(insertIndex, moved);

            AdjustSelectionOnMove(oldIndex, actualCount, insertIndex);
        }

        protected override void OnUiRefresh()
        {
            TargetDropdown.RefreshShownValue();
        }

        private void SetupSelectedPropertyDelegates()
        {
            _selectedGetter = null;
            _selectedSetter = null;

            UnsubscribeSelectedPropertyNpc();

            if (string.IsNullOrEmpty(SelectedProperty))
                return;

            var getActualVmMethod = typeof(BinderBase).GetMethod("GetActualViewModel",
                BindingFlags.Instance | BindingFlags.NonPublic);
            if (getActualVmMethod == null)
            {
                Debug.LogError($"{name}: Cannot find BinderBase.GetActualViewModel method.", this);
                return;
            }

            object vmInstance = null;
            try
            {
                vmInstance = getActualVmMethod.Invoke(this, null);
            }
            catch
            {
                vmInstance = null;
            }

            if (vmInstance == null) return;

            var vmType = vmInstance.GetType();
            var pi = vmType.GetProperty(SelectedProperty,
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            var fi = pi == null
                ? vmType.GetField(SelectedProperty,
                    BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                : null;

            if (pi == null && fi == null)
            {
                Debug.LogError($"{name}: SelectedProperty '{SelectedProperty}' not found on {vmType}.", this);
                return;
            }

            // Expression helpers
            var thisConst = Expression.Constant(this);
            var callGetActual = Expression.Call(thisConst, getActualVmMethod); // returns object
            var instanceExpr = Expression.Convert(callGetActual, vmType); // cast to vmType

            // GETTER
            try
            {
                if (pi != null)
                {
                    var propExpr = Expression.Property(instanceExpr, pi);
                    var guarded = Expression.Condition(
                        Expression.Equal(callGetActual, Expression.Constant(null, typeof(object))),
                        Expression.Constant(null, typeof(object)),
                        Expression.Convert(propExpr, typeof(object))
                    );

                    var getterLambda = Expression.Lambda<Func<object>>(guarded);
                    _selectedGetter = getterLambda.Compile();
                }
                else
                {
                    var fieldExpr = Expression.Field(instanceExpr, fi);
                    var guarded = Expression.Condition(
                        Expression.Equal(callGetActual, Expression.Constant(null, typeof(object))),
                        Expression.Constant(null, typeof(object)),
                        Expression.Convert(fieldExpr, typeof(object))
                    );

                    var getterLambda = Expression.Lambda<Func<object>>(guarded);
                    _selectedGetter = getterLambda.Compile();
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning(
                    $"{name}: Failed to build selected getter expression; using reflection fallback. ({ex.GetType().Name})",
                    this);
                _selectedGetter = () =>
                {
                    var actual = getActualVmMethod.Invoke(this, null);
                    if (actual == null) return null;
                    return pi != null ? pi.GetValue(actual) : fi.GetValue(actual);
                };
            }

            // SETTER (with ConvertTo)
            try
            {
                var convertMethod = typeof(DropdownListBinder).GetMethod(nameof(ConvertTo),
                    BindingFlags.Static | BindingFlags.NonPublic);
                if (convertMethod == null)
                {
                    Debug.LogError($"{name}: Could not find ConvertTo method.", this);
                }

                if (pi != null && pi.CanWrite)
                {
                    var valueParam = Expression.Parameter(typeof(object), "value");
                    var callConvert = Expression.Call(convertMethod, valueParam,
                        Expression.Constant(pi.PropertyType, typeof(Type)));
                    var convertedValue = Expression.Convert(callConvert, pi.PropertyType);
                    var assignExpr = Expression.Assign(Expression.Property(instanceExpr, pi), convertedValue);

                    var setBody = Expression.IfThen(
                        Expression.NotEqual(callGetActual, Expression.Constant(null, typeof(object))),
                        assignExpr
                    );

                    var setterLambda = Expression.Lambda<Action<object>>(setBody, valueParam);
                    _selectedSetter = setterLambda.Compile();
                }
                else if (fi != null && !fi.IsInitOnly)
                {
                    var valueParam = Expression.Parameter(typeof(object), "value");
                    var callConvert = Expression.Call(convertMethod, valueParam,
                        Expression.Constant(fi.FieldType, typeof(Type)));
                    var convertedValue = Expression.Convert(callConvert, fi.FieldType);
                    var assignExpr = Expression.Assign(Expression.Field(instanceExpr, fi), convertedValue);

                    var setBody = Expression.IfThen(
                        Expression.NotEqual(callGetActual, Expression.Constant(null, typeof(object))),
                        assignExpr
                    );

                    var setterLambda = Expression.Lambda<Action<object>>(setBody, valueParam);
                    _selectedSetter = setterLambda.Compile();
                }
                else
                {
                    _selectedSetter = null;
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"{name}: Failed to compile setter for '{SelectedProperty}': {ex.Message}", this);
                _selectedSetter = null;
            }

            try
            {
                if (getActualVmMethod.Invoke(this, null) is INotifyPropertyChanged notifier)
                {
                    notifier.PropertyChanged -= OnAnyViewModelPropertyChanged;
                    notifier.PropertyChanged += OnAnyViewModelPropertyChanged;
                    _subscribedNpcForSelectedProp = notifier;
                }
            }
            catch
            {
                // ignore subscription errors
            }
        }

        private void UnsubscribeSelectedPropertyNpc()
        {
            if (_subscribedNpcForSelectedProp == null)
                return;

            _subscribedNpcForSelectedProp.PropertyChanged -= OnAnyViewModelPropertyChanged;
            _subscribedNpcForSelectedProp = null;
        }

        private static object ConvertTo(object value, Type targetType)
        {
            if (targetType == null)
                throw new ArgumentNullException(nameof(targetType));

            if (value == null)
            {
                var underlying = Nullable.GetUnderlyingType(targetType);
                if (underlying != null)
                    return null;

                return targetType.IsValueType ? Activator.CreateInstance(targetType) : null;
            }

            var effective = Nullable.GetUnderlyingType(targetType) ?? targetType;
            if (effective.IsInstanceOfType(value)) return value;

            if (effective.IsEnum)
            {
                if (value is string s)
                    return Enum.Parse(effective, s);
                try
                {
                    return Enum.ToObject(effective, value);
                }
                catch
                {
                    /* ignored */
                }
            }

            try
            {
                return Convert.ChangeType(value, effective);
            }
            catch
            {
                var tc = TypeDescriptor.GetConverter(effective);
                if (tc.CanConvertFrom(value.GetType()))
                    return tc.ConvertFrom(value);
                throw new InvalidCastException($"Cannot convert value of type {value.GetType()} to {targetType}");
            }
        }

        private void OnAnyViewModelPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (string.IsNullOrEmpty(SelectedProperty))
                return;

            if (e.PropertyName == SelectedProperty || e.PropertyName == string.Empty)
                UpdateDropdownSelectionFromVm();
        }

        private void UpdateDropdownSelectionFromVm()
        {
            if (TargetDropdown == null || _selectedGetter == null)
                return;
            if (_isUpdatingFromDropdown)
                return;

            try
            {
                _isUpdatingFromViewModel = true;

                var vmVal = _selectedGetter();
                var idx = -1;

                if (Snapshot != null && Snapshot.Count > 0)
                {
                    idx = Snapshot.FindIndex(s => ReferenceEquals(s, vmVal) || (s != null && s.Equals(vmVal)));
                }

                if (idx < 0)
                {
                    var targetText = vmVal != null
                        ? (ElementToString != null ? ElementToString(vmVal) : vmVal.ToString())
                        : string.Empty;
                    idx = TargetDropdown.options.FindIndex(o => o.text == targetText);
                }

                if (idx < 0)
                {
                    if (TargetDropdown.options.Count == 0)
                        return;
                    TargetDropdown.value = 0;
                }
                else if (TargetDropdown.value != idx)
                {
                    TargetDropdown.value = idx;
                }
            }
            finally
            {
                _isUpdatingFromViewModel = false;
            }
        }

        private void OnDropdownValueChanged(int newValue)
        {
            if (TargetDropdown == null)
                return;
            if (_isUpdatingFromViewModel)
                return;

            try
            {
                _isUpdatingFromDropdown = true;

                if (_selectedSetter == null)
                {
                    Debug.LogWarning($"{name}: SelectedProperty setter not available or not configured.", this);
                    return;
                }

                object elementToSet = null;
                if (Snapshot != null && newValue >= 0 && newValue < Snapshot.Count)
                {
                    elementToSet = Snapshot[newValue];
                }
                else
                {
                    // fallback: pass option text
                    if (newValue >= 0 && newValue < TargetDropdown.options.Count)
                        elementToSet = TargetDropdown.options[newValue].text;
                }

                _selectedSetter(elementToSet);
            }
            finally
            {
                _isUpdatingFromDropdown = false;
            }
        }

        private void AdjustSelectionOnMove(int oldStart, int count, int newStart)
        {
            if (TargetDropdown == null)
                return;

            var cur = TargetDropdown.value;
            if (cur < 0)
                return;

            var oldEnd = oldStart + count - 1;

            if (cur >= oldStart && cur <= oldEnd)
            {
                var offset = cur - oldStart;
                TargetDropdown.value = newStart + offset;
            }
            else if (oldStart < newStart)
            {
                if (cur > oldEnd && cur <= newStart)
                    TargetDropdown.value = Math.Max(0, cur - count);
            }
            else if (newStart < oldStart)
            {
                if (cur >= newStart && cur < oldStart)
                    TargetDropdown.value = Math.Min(TargetDropdown.options.Count - 1, cur + count);
            }
        }
    }
}