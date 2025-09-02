using System;
using System.Linq;
using System.Reflection;
using MVVM.Runtime.Binders;
using MVVM.Runtime.Collections;
using MVVM.Runtime.Core;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace MVVM.Editor.Binders
{
    public abstract class ListBinderEditorBase : BinderEditorBase
    {
        protected Type ItemType;

        protected override void CacheMembers(MonoBehaviour vmCandidate)
        {
            var vmType = vmCandidate.GetType();
            var properties = vmType.GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                .Where(p => p.PropertyType.IsGenericType &&
                            p.PropertyType.GetGenericTypeDefinition() == typeof(ObservableDataList<>))
                .Where(p => 
                {
                    var genericArgument = p.PropertyType.GetGenericArguments()[0];
                    return typeof(ViewModel).IsAssignableFrom(genericArgument);
                })
                .ToArray();

            MemberNames = properties.Select(p => p.Name).ToArray();
            SelectedMemberIdx = Array.IndexOf(MemberNames, MemberNameProp?.stringValue);

            if (MemberNames.Length == 1)
            {
                if (MemberNameProp != null) MemberNameProp.stringValue = MemberNames[0];
                ItemType = GetItemType(vmType, MemberNames[0]);
            }
            else if (SelectedMemberIdx >= 0)
            {
                ItemType = GetItemType(vmType, MemberNames[SelectedMemberIdx]);
            }
            else
            {
                ItemType = null;
            }
        }

        protected override string GetEmptyMembersMessage(MonoBehaviour vmCandidate, string supportedTypesDescription) =>
            "Not properties with type ObservableList<T> in selected ViewModel.";

        protected override string GetSingleMemberLabel(string memberName) => $"Property: {memberName}";
        protected override string GetMemberPopupLabel() => "Property";

        private static Type GetItemType(Type vmType, string propertyName)
        {
            var prop = vmType.GetProperty(propertyName,
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (prop != null && prop.PropertyType.IsGenericType &&
                prop.PropertyType.GetGenericTypeDefinition() == typeof(ObservableDataList<>))
                return prop.PropertyType.GetGenericArguments()[0];
            
            return null;
        }

        protected void ValidatePrefab(GameObject prefab, SerializedProperty prefabProp)
        {
            if (prefab != null && ItemType != null)
            {
                var template = prefab.GetComponent<CollectionViewModelTemplate>();
                if (template == null)
                {
                    ShowPrefabError($"Prefab must have {nameof(CollectionViewModelTemplate)} Component.");
                    prefabProp.objectReferenceValue = null;
                    return;
                }

                if (template.ViewModelTypeName != ItemType.FullName)
                {
                    ShowPrefabError(
                        $"The ViewModelTypeName in {nameof(CollectionViewModelTemplate)} must be equal '{ItemType.FullName}', but now - '{template.ViewModelTypeName}'.");
                    prefabProp.objectReferenceValue = null;
                }
            }
        }

        protected void DrawItemTypeLabels(SerializedProperty prefabProp)
        {
            Root.Q<Label>("ItemTypeLabel")?.RemoveFromHierarchy();
            Root.Q<Label>("TemplateVMTypeLabel")?.RemoveFromHierarchy();

            if (ItemType != null)
            {
                Root.Add(new Label($"Item Type: {ItemType.Name}") {name = "ItemTypeLabel"});
                if (prefabProp != null && prefabProp.objectReferenceValue is GameObject prefab)
                {
                    var template = prefab.GetComponent<CollectionViewModelTemplate>();
                    if (template != null)
                        Root.Add(new Label($"TemplateVM ViewModelTypeName: {template.ViewModelTypeName}")
                            {name = "TemplateVMTypeLabel"});
                }
            }
        }

        protected static void ShowPrefabError(string message)
        {
            EditorUtility.DisplayDialog("Error in Item Prefab", message, "Ок");
        }
    }
}