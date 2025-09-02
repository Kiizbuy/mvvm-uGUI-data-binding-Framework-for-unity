using MVVM.Runtime.Converters;
using UnityEngine;

namespace Core.MVVM.Runtime.Core.Converters
{
    [CreateAssetMenu(menuName = "MVVM/Converters/Float To Formatted String")]
    public sealed class FloatToFormattedStringConverterT : ValueConverterT<float, string>
    {
        public string Format = "0.##";

        public override string Convert(float source) => source.ToString(Format);
        public override float ConvertBack(string target) => float.TryParse(target, out var f) ? f : 0f;
    }
}