using MVVM.Runtime.Binders;
using UnityEngine;
using UnityEngine.UI;

namespace Core.MVVM.Runtime.Core.Binders.Implementations
{
    [AddComponentMenu("MVVM/Binders/InputField Binder")]
    [RequireComponent(typeof(InputField))]
    public sealed class InputFieldBinder : TwoWayBinder<string, string>
    {
        [SerializeField]
        private InputField _input;

        private void Awake()
        {
            _input ??= GetComponent<InputField>();
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