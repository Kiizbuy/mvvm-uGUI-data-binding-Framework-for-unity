using System;
using System.Collections.Generic;
using UnityEngine.Scripting;

namespace MVVM.Runtime.Collections
{
    [Preserve]
    public static class ObservableDataListExtensions
    {
        [Preserve]
        public static ObservableDataList<T> ToObservableDataList<T>(this IEnumerable<T> source, bool clone = true)
        {
            if (source == null)
            {
                throw new ArgumentNullException(nameof(source));
            }

            if (!clone && source is ObservableDataList<T> existing)
            {
                return existing;
            }

            var result = new ObservableDataList<T>();
            foreach (var item in source)
            {
                result.Add(item);
            }

            return result;
        }

        [Preserve]
        public static ObservableDataList<T> CopyToObservableDataList<T>(this IEnumerable<T> source, ObservableDataList<T> target)
        {
            if (target == null)
            {
                throw new ArgumentNullException(nameof(target));
            }

            if (source == null)
            {
                target.Clear();
                return target;
            }

            target.Clear();
            foreach (var item in source)
            {
                target.Add(item);
            }

            return target;
        }
    }
}
