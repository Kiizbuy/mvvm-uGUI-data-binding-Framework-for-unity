using System;
using MVVM.Runtime.Binders;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;

namespace MVVM.Editor.Binders
{
    [CustomEditor(typeof(BinderBase), true)]
    public sealed class BinderBaseEditor : BinderEditorBase
    {
        protected override string ViewModelPropName => nameof(BinderBase.ViewModel);
        protected override string MemberNamePropName => nameof(BinderBase.ViewModelProperty);

        protected override Type[] GetSupportedSourceTypes() =>
            BinderHelper.GetSupportedSourceTypes((BinderBase) target);

        protected override void CacheMembers(MonoBehaviour vmCandidate)
        {
            var vmType = (vmCandidate as IViewModelProvider)?.GetViewModel()?.GetType() ?? vmCandidate.GetType();
            MemberNames = GetBindablePropertyNames(vmType, SupportedSourceTypes);
            SelectedMemberIdx = Array.IndexOf(MemberNames, MemberNameProp?.stringValue);
        }

        protected override string GetEmptyMembersMessage(MonoBehaviour vmCandidate, string supportedTypesDescription) =>
            $"ViewModel '{DescribeVmCandidate(vmCandidate)}' not contains public [Bindable]-properties, not compatible with type binder ({supportedTypesDescription}).";

        protected override string GetSingleMemberLabel(string memberName) => $"Property: {memberName}";
        protected override string GetMemberPopupLabel() => "Property";

        protected override void DrawMemberExtraControls()
        {
            var conv = serializedObject.FindProperty(nameof(BinderBase.Converter));
            if (conv != null) Root.Add(new PropertyField(conv));
        }
    }
}