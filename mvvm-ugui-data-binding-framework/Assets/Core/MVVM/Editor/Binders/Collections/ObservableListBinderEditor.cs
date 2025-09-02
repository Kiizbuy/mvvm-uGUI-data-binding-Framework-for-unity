using MVVM.Runtime.Binders;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace MVVM.Editor.Binders
{
    [CustomEditor(typeof(ObservableViewModelListBinderBase), true)]
    public sealed class ObservableListBinderEditor : ListBinderEditorBase
    {
        protected override string ViewModelPropName => nameof(ObservableViewModelListBinderBase.ViewModel);
        protected override string MemberNamePropName => nameof(ObservableViewModelListBinderBase.ViewModelProperty);

        protected override string[] SerializedFieldsToSkip() => new[] {"container", "itemPrefab"};

        protected override void DrawMemberExtraControls()
        {
            // draw container (we skip it from default fields so draw it here in desired order)
            var containerProp = serializedObject.FindProperty("container");
            if (containerProp != null)
                Root.Add(new PropertyField(containerProp));

            var prefabProp = serializedObject.FindProperty("itemPrefab");
            var prefabField = new ObjectField("Item Prefab")
                {objectType = typeof(GameObject), allowSceneObjects = true};
            prefabField.BindProperty(prefabProp);

            prefabField.RegisterValueChangedCallback(evt =>
            {
                ValidatePrefab(evt.newValue as GameObject, prefabProp);
                serializedObject.ApplyModifiedProperties();
                prefabField.SetValueWithoutNotify(prefabProp.objectReferenceValue as GameObject);
                DrawItemTypeLabels(prefabProp);
            });

            Root.Add(prefabField);

            DrawItemTypeLabels(prefabProp);
        }
    }
}