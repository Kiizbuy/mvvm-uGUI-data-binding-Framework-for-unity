using System;
using System.Collections.Generic;
using System.Linq;
using MVVM.Runtime.Binders;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace MVVM.Editor.Binders
{
    [CustomEditor(typeof(SubViewModelTemplate))]
    public class SubViewModelTemplateEditor : ViewModelEditorBase
    {
        private SerializedProperty _viewModelPropertyNameProp;
        private SerializedProperty _rootProviderMonoProp;

        private List<MonoBehaviour> _rootCandidates = new();

        private string[] _memberNames = Array.Empty<string>();
        private Dictionary<string, Type> _memberTypes = new();
        private int _selectedMemberIdx = -1;

        private Label _memberTypeLabel;
        private PopupField<string> _memberPopup;
        private VisualElement _popupContainer;

        public override VisualElement CreateInspectorGUI()
        {
            if (target == null)
                return new Label("Invalid object (null target).");

            var root = new VisualElement();
            _viewModelPropertyNameProp = serializedObject.FindProperty("viewModelPropertyName");
            _rootProviderMonoProp = serializedObject.FindProperty("rootProviderMono");

            CollectRootCandidates(out _rootCandidates, out _);
            UpdateMemberList();

            if (_memberNames.Length == 0)
            {
                root.Add(new HelpBox(
                    "No [Bindable] properties/fields were found on the root ViewModel that inherit from ViewModelBase.",
                    HelpBoxMessageType.Info));
            }
            else
            {
                _popupContainer = new VisualElement();
                root.Add(_popupContainer);
                CreateMemberPopup();

                _memberTypeLabel = new Label();
                UpdateMemberTypeLabel();
                root.Add(_memberTypeLabel);
            }

            return root;
        }

        private void CreateMemberPopup()
        {
            if (_popupContainer == null) return;

            _popupContainer.Clear();

            var currentName = _viewModelPropertyNameProp.stringValue;
            _selectedMemberIdx = Math.Max(0, Array.IndexOf(_memberNames, currentName));

            if (_selectedMemberIdx == -1 && !string.IsNullOrEmpty(currentName))
            {
                Debug.LogError(GetInvalidMemberMessage(GetCurrentProvider(), currentName, _memberNames));
            }

            _memberPopup = new PopupField<string>(
                "Sub ViewModel member",
                _memberNames.ToList(),
                _selectedMemberIdx,
                s => s,
                s => s
            );

            if (_selectedMemberIdx == -1 && !string.IsNullOrEmpty(currentName))
            {
                var labelElement = _memberPopup.Q<Label>();
                if (labelElement != null)
                {
                    labelElement.style.color = ErrorColor;
                }
            }

            _memberPopup.RegisterValueChangedCallback(evt =>
            {
                var newIdx = Array.IndexOf(_memberNames, evt.newValue);
                if (newIdx < 0) 
                    return;
                
                _selectedMemberIdx = newIdx;
                _viewModelPropertyNameProp.stringValue = _memberNames[newIdx];
                serializedObject.ApplyModifiedProperties();
                
                var labelElement = _memberPopup.Q<Label>();
                if (labelElement != null)
                {
                    labelElement.style.color = StyleKeyword.Null;
                }
                
                UpdateMemberTypeLabel();
            });
            _memberPopup.BindProperty(_viewModelPropertyNameProp);

            _popupContainer.Add(_memberPopup);
        }

        private MonoBehaviour GetCurrentProvider()
        {
            var providerObj = _rootProviderMonoProp.objectReferenceValue as MonoBehaviour;
            if (providerObj == null && _rootCandidates.Count == 1)
                providerObj = _rootCandidates[0];
            return providerObj;
        }

        private void UpdateMemberList()
        {
            _memberNames = Array.Empty<string>();
            _memberTypes.Clear();

            var providerObj = GetCurrentProvider();
            
            if (providerObj == null) 
                return;

            GetBindableMembersFromProvider(providerObj, out _memberNames, out _memberTypes);

            if (_memberNames.Length == 1)
            {
                _selectedMemberIdx = 0;
                _viewModelPropertyNameProp.stringValue = _memberNames[0];
                serializedObject.ApplyModifiedProperties();
            }
            
            var currentName = _viewModelPropertyNameProp.stringValue;
            var currentIdx = Array.IndexOf(_memberNames, currentName);
            
            if (currentIdx == -1 && !string.IsNullOrEmpty(currentName))
            {
                Debug.LogError(GetInvalidMemberMessage(providerObj, currentName, _memberNames));
            }

            CreateMemberPopup();
        }

        private void UpdateMemberTypeLabel()
        {
            if (_memberTypeLabel == null) 
                return;
            
            if (_selectedMemberIdx < 0 || _selectedMemberIdx >= _memberNames.Length)
            {
                _memberTypeLabel.text = "(Unknown)";
                return;
            }

            var memberName = _memberNames[_selectedMemberIdx];
            _memberTypeLabel.text = _memberTypes.TryGetValue(memberName, out var t) ? $"Type: {t.Name}" : "(Unknown)";
        }
    }
}