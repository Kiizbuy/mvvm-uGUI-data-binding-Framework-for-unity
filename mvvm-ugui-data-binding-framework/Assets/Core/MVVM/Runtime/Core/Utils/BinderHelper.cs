using System;
using System.Linq;
using System.Reflection;
using MVVM.Runtime.Binders;

namespace MVVM.Editor.Binders
{
    public static class BinderHelper
    {
        public static Type[] GetSupportedSourceTypes(BinderBase binder)
        {
            var attr = binder.GetType()
                            .GetCustomAttribute<SupportedBindingTypesAttribute>(true);
            if (attr != null && attr.Types.Length > 0)
                return attr.Types;

            var type = binder.GetType();
            while (type != null)
            {
                if (type.IsGenericType)
                {
                    var genDef = type.GetGenericTypeDefinition();
                    if (genDef == typeof(TwoWayBinder<,>) ||
                        genDef == typeof(OneWayBinder<,>))
                    {
                        var sourceType = type.GetGenericArguments()[0];
                        return new[] { sourceType };
                    }
                }
                type = type.BaseType;
            }

            return Array.Empty<Type>();
        }

        public static bool IsCompatible(Type propertyType, Type[] supported)
        {
            if (supported == null || supported.Length == 0) 
                return true;

            return supported.Any(s => s.IsAssignableFrom(propertyType));
        }
    }
}