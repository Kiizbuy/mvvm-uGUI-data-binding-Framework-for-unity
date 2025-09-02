using MVVM.Runtime.Binders;
using UnityEngine;
using UnityEngine.UI;

namespace Core.MVVM.Runtime.Core.Binders.Implementations
{
    [AddComponentMenu("MVVM/Binders/Text Binder")]
    [RequireComponent(typeof(Text))]
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
    public class TextBinder : OneWayBinder<string, string>
    {
        [SerializeField] 
        private Text _text;
        
        private void Awake()
        {
            _text ??= GetComponent<Text>();
            base.Awake();
        }

        protected override void ApplyTargetValue(string value)
        {
            _text.text = value;
        }
    }
}