using MVVM.Runtime.Binders;
using TMPro;
using UnityEngine;

namespace Core.MVVM.Runtime.Core.Binders.Implementations
{
    [AddComponentMenu("MVVM/Binders/TMP_InputField Binder")]
    [RequireComponent(typeof(TMP_InputField))]
    public class TMP_InputFieldBinder: TwoWayBinder<string, string>
    {
        [SerializeField]
        private TMP_InputField _input;

        private void Awake()
        {
            _input ??= GetComponent<TMP_InputField>();
            base.Awake();
        }

        protected override void ApplyTargetValue(string value)
        {
            _input.text = value;
        }

        protected override void SubscribeToUiEvents()
        {
            _input.onEndEdit.RemoveListener(OnUiValueChanged);
            _input.onEndEdit.AddListener(OnUiValueChanged);
        }

        protected override void UnsubscribeFromUiEvents()
        {
            _input.onEndEdit.RemoveListener(OnUiValueChanged);
        }
    }
}