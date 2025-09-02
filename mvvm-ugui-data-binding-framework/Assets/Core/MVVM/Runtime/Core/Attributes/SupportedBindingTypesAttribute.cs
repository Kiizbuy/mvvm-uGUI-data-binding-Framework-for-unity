using System;

namespace MVVM.Runtime.Binders
{
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = true)]
    public sealed class SupportedBindingTypesAttribute : Attribute
    {
        public readonly Type[] Types;

        public SupportedBindingTypesAttribute(params Type[] types) =>
            Types = types ?? Array.Empty<Type>();
    }
}