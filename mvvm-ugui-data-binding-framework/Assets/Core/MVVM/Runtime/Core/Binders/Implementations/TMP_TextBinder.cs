using MVVM.Runtime.Binders;
using TMPro;
using UnityEngine;

namespace Core.MVVM.Runtime.Core.Binders.Implementations
{
    [AddComponentMenu("MVVM/Binders/TMP_Text Binder")]
    [RequireComponent(typeof(TextMeshProUGUI))]
    [SupportedBindingTypes(typeof(string),
        typeof(int), 
        typeof(float),
        typeof(double), 
        typeof(long), 
        typeof(ulong), 
        typeof(short), 
        typeof(ushort), 
        typeof(byte), 
        typeof(sbyte))]
    public sealed class TMP_TextBinder : OneWayBinder<string, string>
    {
        [SerializeField]
        private TextMeshProUGUI _text;

        private void Awake()
        {
            _text ??= GetComponent<TextMeshProUGUI>();
            base.Awake();
        }

        protected override void ApplyTargetValue(string value)
        {
            _text.text = value;
        }
    }
}