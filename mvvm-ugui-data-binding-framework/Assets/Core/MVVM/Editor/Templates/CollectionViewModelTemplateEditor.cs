using MVVM.Runtime.Binders;
using UnityEditor;
using UnityEngine.UIElements;

namespace MVVM.Editor.Binders
{
    [CustomEditor(typeof(CollectionViewModelTemplate))]
    public class CollectionViewModelTemplateEditor : ViewModelEditorBase
    {
        private SerializedProperty _viewModelTypeNameProp;
        private PopupField<string> _popupField;

        public override VisualElement CreateInspectorGUI()
        {
            var root = new VisualElement();

            _viewModelTypeNameProp = serializedObject.FindProperty("viewModelTypeName");

            var popup = CreateViewModelTypePopup(_viewModelTypeNameProp, "ViewModel Type");
            root.Add(popup);

            return root;
        }
    }
}