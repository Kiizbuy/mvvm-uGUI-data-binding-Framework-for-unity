using System;
using MVVM.Runtime.Binders;
using UnityEditor;
using UnityEngine;

namespace MVVM.Editor.Binders
{
    [CustomEditor(typeof(CommandBinder), true)]
    public sealed class CommandBinderEditor : BinderEditorBase
    {
        protected override string ViewModelPropName => nameof(CommandBinder.ViewModel);
        protected override string MemberNamePropName => nameof(CommandBinder.MethodName);

        protected override void CacheMembers(MonoBehaviour vmCandidate)
        {
            if (vmCandidate is IViewModelProvider provider && provider.GetViewModel() != null)
            {
                MemberNames = GetBindableMethods(provider.GetViewModel().GetType());
                SelectedMemberIdx = Array.IndexOf(MemberNames, MemberNameProp?.stringValue);
            }
            else
            {
                MemberNames = Array.Empty<string>();
                SelectedMemberIdx = -1;
            }
        }

        protected override string GetEmptyMembersMessage(MonoBehaviour vmCandidate, string supportedTypesDescription) =>
            $"ViewModel '{DescribeVmCandidate(vmCandidate)}' not contains [Bindable]-methods without arguments.";

        protected override string GetSingleMemberLabel(string memberName) => $"Method: {memberName}";
        protected override string GetMemberPopupLabel() => "Method";
    }
}