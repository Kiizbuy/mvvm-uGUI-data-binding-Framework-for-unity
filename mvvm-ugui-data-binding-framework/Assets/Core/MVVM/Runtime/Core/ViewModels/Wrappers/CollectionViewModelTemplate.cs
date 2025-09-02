using UnityEngine;
using UnityEngine.Assertions;
using System;
using System.Linq;

namespace MVVM.Runtime.Binders
{
    public sealed class CollectionViewModelTemplate : MonoBehaviour, IViewModelTemplate, IViewModelProvider
    {
        [SerializeField]
        private string viewModelTypeName = string.Empty;

        private object viewModel;

        public string ViewModelTypeName
        {
            get => viewModelTypeName;
            set => viewModelTypeName = value;
        }

        public object GetViewModel()
        {
            if (viewModel != null || string.IsNullOrEmpty(viewModelTypeName))
                return viewModel;

            var type = Type.GetType(viewModelTypeName);
            if (type == null)
            {
                Debug.LogError($"TemplateVM: not found type '{viewModelTypeName}'", gameObject);
                return null;
            }

            try
            {
                viewModel = CreateInstanceWithDefaults(type);
            }
            catch (Exception ex)
            {
                Debug.LogError($"TemplateVM: instantiate error '{viewModelTypeName}'");
                Debug.LogException(ex);
                return null;
            }

            return viewModel;
        }

        private static object CreateInstanceWithDefaults(Type type)
        {
            var ctor = type.GetConstructor(Type.EmptyTypes);
            if (ctor != null)
                return Activator.CreateInstance(type);

            ctor = type.GetConstructors()
                .OrderBy(c => c.GetParameters().Length)
                .FirstOrDefault();

            if (ctor == null)
                throw new MissingMethodException($"Тип '{type.FullName}' не имеет публичных конструкторов.");

            var parameters = ctor.GetParameters()
                .Select(p => p.HasDefaultValue ? p.DefaultValue : GetDefaultValue(p.ParameterType))
                .ToArray();

            return ctor.Invoke(parameters);
        }

        private static object GetDefaultValue(Type t) =>
            t.IsValueType ? Activator.CreateInstance(t) : null;

        public void InitChildBindings(object viewModel)
        {
            Assert.IsNotNull(viewModel, "Cannot initialise child bindings with null view model.");
            this.viewModel = viewModel;

            foreach (var binding in GetComponentsInChildren<BinderBase>(true))
            {
                binding.ViewModel = this;
                binding.Initialize();
            }
        }
    }
}