using UnityEngine;

namespace MVVM.Runtime.Converters
{
    [CreateAssetMenu(menuName = "MVVM/Converters/String To Int")]
    public sealed class StringToIntConverterT : ValueConverterT<string, int>
    {
        public override int Convert(string source) => int.TryParse(source, out var i) ? i : 0;
        public override string ConvertBack(int target) => target.ToString();
    }
}