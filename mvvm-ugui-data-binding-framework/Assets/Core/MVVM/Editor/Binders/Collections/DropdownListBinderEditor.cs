using System;
using System.Linq;
using System.Reflection;
using MVVM.Runtime.Binders;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace MVVM.Editor.Binders
{
    [CustomEditor(typeof(DropdownListBinder), true)]
    public sealed class DropdownListBinderEditor : BinderEditorBase
    {
        protected override string ViewModelPropName => nameof(DropdownListBinder.ViewModel);
        protected override string MemberNamePropName => nameof(DropdownListBinder.ViewModelProperty);

        private SerializedProperty _selectedPropertyProp;
        private string[] _candidateSelectedNames = Array.Empty<string>();

        private readonly Type[] _supportedTypes = new Type[]
        {
            typeof(string),
            typeof(int),
            typeof(long),
            typeof(float),
            typeof(double),
            typeof(ulong),
            typeof(short),
            typeof(ushort),
        };

        protected override Type[] GetSupportedSourceTypes()
        {
            return new[]
            {
                typeof(System.Collections.IEnumerable)
            };
        }

        protected override void CacheMembers(MonoBehaviour vmCandidate)
        {
            if (vmCandidate == null)
            {
                MemberNames = Array.Empty<string>();
                return;
            }

            var vmType = (vmCandidate as IViewModelProvider)?.GetViewModel()?.GetType() ?? vmCandidate.GetType();

            MemberNames = GetSupportedBindablePropertyNames(vmType);

            _candidateSelectedNames = GetBindablePropertyNames(vmType, _supportedTypes)
                .OrderBy(n => n).ToArray();
        }

        private string[] GetSupportedBindablePropertyNames(Type vmType)
        {
            return GetBindablePropertyNames(vmType, GetSupportedSourceTypes())
                .Where(name =>
                {
                    var pi = vmType.GetProperty(name,
                        BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                    if (pi == null)
                        return false;
                    
                    var elemType = GetEnumerableElementType(pi.PropertyType);
                    return elemType != null && IsElementTypeSupported(elemType);

                })
                .OrderBy(n => n)
                .ToArray();
        }

        protected override void DrawMemberExtraControls()
        {
            _selectedPropertyProp ??= serializedObject.FindProperty(nameof(DropdownListBinder.SelectedProperty));

            if (_candidateSelectedNames.Length == 0)
            {
                Root.Add(new HelpBox(
                    "There are no bindable properties available on the ViewModel to record the selected value.",
                    HelpBoxMessageType.Info));
                
                if (_selectedPropertyProp != null)
                    Root.Add(new PropertyField(_selectedPropertyProp));
            }
            else
            {
                var current = _selectedPropertyProp != null ? _selectedPropertyProp.stringValue : string.Empty;
                var idx = Array.IndexOf(_candidateSelectedNames, current);
                
                if (idx == -1 && !string.IsNullOrEmpty(current))
                {
                    Debug.LogError(GetInvalidSelectedPropertyMessage(current));
                }

                var popup = new PopupField<string>("Selected property (write)", _candidateSelectedNames.ToList(), Mathf.Max(0, idx),
                    s => s, s => s);
                
                if (_selectedPropertyProp != null)
                    popup.BindProperty(_selectedPropertyProp);

                if (idx == -1 && !string.IsNullOrEmpty(current))
                {
                    var labelElement = popup.Q<Label>();
                    if (labelElement != null)
                    {
                        labelElement.style.color = ERROR_COLOR;
                    }
                }

                popup.RegisterValueChangedCallback(evt =>
                {
                    var newIdx = Array.IndexOf(_candidateSelectedNames, evt.newValue);
                    if (newIdx >= 0)
                    {
                        if (_selectedPropertyProp != null)
                        {
                            _selectedPropertyProp.stringValue = evt.newValue;
                            serializedObject.ApplyModifiedProperties();
                        }
                        
                        // Убираем подсветку при выборе валидного значения
                        var labelElement = popup.Q<Label>();
                        if (labelElement != null)
                        {
                            labelElement.style.color = StyleKeyword.Null;
                        }
                    }
                });
                Root.Add(popup);
            }
        }

        private string GetInvalidSelectedPropertyMessage(string invalidPropertyName)
        {
            return $"Selected property '{invalidPropertyName}' not found in ViewModel. " +
                   $"Available properties: {string.Join(", ", _candidateSelectedNames)}";
        }

        protected override string GetEmptyMembersMessage(MonoBehaviour vmCandidate, string supportedTypesDescription)
        {
            return
                $"ViewModel '{DescribeVmCandidate(vmCandidate)}' doesn't expose an IEnumerable bindable member (ObservableDataList<T>) with element type in [{string.Join(", ", _supportedTypes.Select(t => t.Name))}].";
        }

        private static Type GetEnumerableElementType(Type type)
        {
            if (type == null)
                return null;

            if (type.IsArray)
                return type.GetElementType();

            if (type.IsGenericType &&
                type.GetGenericTypeDefinition() == typeof(System.Collections.Generic.IEnumerable<>))
                return type.GetGenericArguments()[0];

            var ifaces = type.GetInterfaces();
           
            foreach (var iface in ifaces)
            {
                if (iface.IsGenericType &&
                    iface.GetGenericTypeDefinition() == typeof(System.Collections.Generic.IEnumerable<>))
                    return iface.GetGenericArguments()[0];
            }

            return null;
        }

        private bool IsElementTypeSupported(Type elementType)
        {
            if (elementType == null)
                return false;

            if (_supportedTypes.Any(s =>
                    s == elementType || s.IsAssignableFrom(elementType) || elementType.IsAssignableFrom(s)))
                return true;

            if (Nullable.GetUnderlyingType(elementType) is { } underlying)
            {
                if (_supportedTypes.Any(s =>
                        s == underlying || s.IsAssignableFrom(underlying) || underlying.IsAssignableFrom(s)))
                    return true;
            }

            return false;
        }
    }
}