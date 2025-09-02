using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEditor.UIElements;
using MVVM.Runtime.Binders;
using MVVM.Runtime.Core;

namespace MVVM.Editor.Binders
{
    public abstract class BinderEditorBase : UnityEditor.Editor
    {
        protected SerializedProperty ViewModelProp;
        protected SerializedProperty MemberNameProp;

        protected MonoBehaviour CachedVm;
        protected string[] MemberNames = Array.Empty<string>();
        protected int SelectedMemberIdx = -1;

        protected Type[] SupportedSourceTypes = Array.Empty<Type>();
        protected VisualElement Root;

        protected static readonly Color ERROR_COLOR = new Color(1f, 0.4f, 0.4f);

        public override VisualElement CreateInspectorGUI()
        {
            Root = new VisualElement();

            ViewModelProp = serializedObject.FindProperty(ViewModelPropName);
            MemberNameProp = serializedObject.FindProperty(MemberNamePropName);

            SupportedSourceTypes = GetSupportedSourceTypes();

            UpdateUI();
            return Root;
        }

        private void UpdateUI()
        {
            serializedObject.Update();
            Root.Clear();

            if (target == null)
                return;

            var vmCandidates = ((Component) target).GetComponentsInParent<MonoBehaviour>(true)
                .Where(m => m is IViewModelProvider)
                .ToArray();

            var selectedVm = ViewModelProp?.objectReferenceValue as MonoBehaviour;

            if (vmCandidates.Length == 0)
            {
                Root.Add(new HelpBox(
                    $"There is no component in the hierarchy that inherits {nameof(IViewModelProvider)}. Set the ViewModel manually.",
                    HelpBoxMessageType.Warning));
                if (ViewModelProp != null)
                    Root.Add(new PropertyField(ViewModelProp));
            }
            else if (vmCandidates.Length == 1)
            {
                if (ViewModelProp != null && selectedVm != vmCandidates[0])
                {
                    Undo.RecordObject(target, "Auto-select ViewModel");
                    ViewModelProp.objectReferenceValue = vmCandidates[0];
                    serializedObject.ApplyModifiedProperties();
                    selectedVm = vmCandidates[0];
                }

                Root.Add(new Label($"ViewModel: {DescribeVmCandidate(vmCandidates[0])}"));
            }
            else
            {
                var options = vmCandidates.Select(DescribeVmCandidate).ToList();
                var currentIdx = Mathf.Max(0, Array.IndexOf(vmCandidates, selectedVm));

                var popup = new PopupField<string>("ViewModel", options, currentIdx, s => s, s => s);
                if (ViewModelProp != null) popup.BindProperty(ViewModelProp);
                popup.RegisterValueChangedCallback(evt =>
                {
                    var newIdx = options.IndexOf(evt.newValue);
                    if (newIdx >= 0 && newIdx < vmCandidates.Length)
                    {
                        Undo.RecordObject(target, "Select ViewModel");
                        if (ViewModelProp != null)
                            ViewModelProp.objectReferenceValue = vmCandidates[newIdx];
                        serializedObject.ApplyModifiedProperties();
                        UpdateUI();
                    }
                });
                Root.Add(popup);
                selectedVm = vmCandidates[Mathf.Clamp(currentIdx, 0, vmCandidates.Length - 1)];
            }

            if (selectedVm != null)
            {
                if (!ReferenceEquals(selectedVm, CachedVm))
                {
                    CacheMembers(selectedVm);
                    CachedVm = selectedVm;
                }

                if (MemberNames.Length == 0)
                {
                    var supported = SupportedSourceTypes.Length == 0
                        ? "Any type"
                        : string.Join(", ", SupportedSourceTypes.Select(t => t.Name));
                    Root.Add(new HelpBox(GetEmptyMembersMessage(selectedVm, supported), HelpBoxMessageType.Warning));
                }
                else if (MemberNames.Length == 1)
                {
                    if (MemberNameProp != null && MemberNameProp.stringValue != MemberNames[0])
                    {
                        MemberNameProp.stringValue = MemberNames[0];
                        serializedObject.ApplyModifiedProperties();
                        SelectedMemberIdx = 0;
                    }

                    Root.Add(new Label(GetSingleMemberLabel(MemberNames[0])));
                }
                else
                {
                    var currentMemberName = MemberNameProp?.stringValue;
                    var idx = Array.IndexOf(MemberNames, currentMemberName);
                    
                    if (idx == -1 && !string.IsNullOrEmpty(currentMemberName))
                    {
                        Debug.LogError(GetInvalidMemberMessage(selectedVm, currentMemberName));
                    }

                    var memberPopup = new PopupField<string>(
                        GetMemberPopupLabel(), 
                        MemberNames.ToList(), 
                        Mathf.Max(0, idx));
                    
                    if (MemberNameProp != null) 
                        memberPopup.BindProperty(MemberNameProp);

                    if (idx == -1 && !string.IsNullOrEmpty(currentMemberName))
                    {
                        var label = memberPopup.Q<Label>();
                        if (label != null)
                        {
                            label.style.color = ERROR_COLOR;
                        }
                    }

                    memberPopup.RegisterValueChangedCallback(evt =>
                    {
                        var newIdx = Array.IndexOf(MemberNames, evt.newValue);
                        if (newIdx >= 0)
                        {
                            SelectedMemberIdx = newIdx;
                            if (MemberNameProp != null)
                                MemberNameProp.stringValue = evt.newValue;
                            serializedObject.ApplyModifiedProperties();
                            
                            var label = memberPopup.Q<Label>();
                            if (label != null)
                            {
                                label.style.color = StyleKeyword.Null;
                            }
                        }
                    });
                    Root.Add(memberPopup);
                }

                DrawMemberExtraControls();
            }

            DrawBinderSerializedFields();
            serializedObject.ApplyModifiedProperties();
        }

        protected virtual string GetInvalidMemberMessage(MonoBehaviour vmCandidate, string invalidMemberName)
        {
            var supported = SupportedSourceTypes.Length == 0
                ? "Any type"
                : string.Join(", ", SupportedSourceTypes.Select(t => t.Name));
                
            return $"Member '{invalidMemberName}' not found in ViewModel '{DescribeVmCandidate(vmCandidate)}'. " +
                   $"Supported types: {supported}. Available members: {string.Join(", ", MemberNames)}";
        }

        protected abstract string ViewModelPropName { get; }
        protected abstract string MemberNamePropName { get; }
        protected virtual Type[] GetSupportedSourceTypes() => Array.Empty<Type>();
        protected abstract void CacheMembers(MonoBehaviour vmCandidate);

        protected virtual string GetEmptyMembersMessage(MonoBehaviour vmCandidate, string supportedTypesDescription) =>
            $"ViewModel '{DescribeVmCandidate(vmCandidate)}' doesn't have compatible members ({supportedTypesDescription}).";

        protected virtual string GetSingleMemberLabel(string memberName) => $"Member: {memberName}";
        protected virtual string GetMemberPopupLabel() => "Member";

        protected virtual void DrawMemberExtraControls()
        {
        }

        protected virtual string[] SerializedFieldsToSkip() => Array.Empty<string>();

        protected virtual string DescribeVmCandidate(MonoBehaviour candidate)
        {
            switch (candidate)
            {
                case CollectionViewModelTemplate template:
                    var vm = template.GetViewModel();
                    return vm != null
                        ? $"Template → {vm.GetType().Name} ({candidate.gameObject.name})"
                        : $"Template (null) ({candidate.gameObject.name})";
                case SubViewModelTemplate subTemplate:
                    var vm2 = subTemplate.GetViewModel();
                    var typeName = vm2 != null ? vm2.GetType().Name : "null";
                    return
                        $"SubTemplate → {subTemplate.ViewModelPropertyName} {typeName} ({candidate.gameObject.name})";
                default:
                    return $"{candidate.GetType().Name} → ({candidate.gameObject.name})";
            }
        }

        private void DrawBinderSerializedFields()
        {
            var skip = new HashSet<string>(SerializedFieldsToSkip());

            var targetType = target.GetType();
            var fields = targetType.GetFields(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
          
            foreach (var field in fields)
            {
                if (skip.Contains(field.Name))
                    continue;

                if (field.GetCustomAttribute<SerializeField>() == null) 
                    continue;
                
                var property = serializedObject.FindProperty(field.Name);
                
                if (property != null) 
                    Root.Add(new PropertyField(property));
            }
        }

        protected static string[] GetBindablePropertyNames(Type vmType, Type[] supportedSourceTypes)
        {
            var props = vmType.GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                .Where(p => p.GetCustomAttribute<BindableAttribute>() != null)
                .Where(p => BinderHelper.IsCompatible(p.PropertyType, supportedSourceTypes))
                .Select(p => p.Name);

            return props.ToArray();
        }

        protected static string[] GetBindableMethods(Type vmType)
        {
            return vmType.GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                .Where(m => m.GetCustomAttribute<BindableAttribute>() != null && m.GetParameters().Length == 0)
                .Select(m => m.Name)
                .OrderBy(n => n)
                .ToArray();
        }
    }
}