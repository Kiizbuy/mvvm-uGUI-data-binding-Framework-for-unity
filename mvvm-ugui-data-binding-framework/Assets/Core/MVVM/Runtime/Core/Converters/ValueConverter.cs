using UnityEngine;
using MVVM.Runtime.Core;

namespace MVVM.Runtime.Converters
{
    public abstract class ValueConverter : ScriptableObject, IValueConverter
    {
        public abstract object Convert(object source);

        public abstract object ConvertBack(object target);  
    }
}