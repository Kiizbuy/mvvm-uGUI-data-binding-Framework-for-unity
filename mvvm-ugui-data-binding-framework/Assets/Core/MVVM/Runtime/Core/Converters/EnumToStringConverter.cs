using System;
using MVVM.Runtime.Converters;
using UnityEngine;

namespace MVVM.Runtime.Core
{
    [CreateAssetMenu(menuName = "MVVM/Converters/Enum To String")]
    public sealed class EnumToStringConverterT<TEnum> : ValueConverterT<TEnum, string> where TEnum : Enum
    {
        public override string Convert(TEnum source) => source.ToString();
        public override TEnum ConvertBack(string target) => (TEnum)Enum.Parse(typeof(TEnum), target);
    }
}