using UnityEngine;
using MVVM.Runtime.Core;

namespace MVVM.Runtime.Converters
{
    public abstract class ValueConverterT<TSource, TTarget> : ValueConverter
    {
        public abstract TTarget Convert(TSource source);
        public abstract TSource ConvertBack(TTarget target);

        public override object Convert(object source) => Convert((TSource)source);
        public override object ConvertBack(object target) => ConvertBack((TTarget)target);
    }
}