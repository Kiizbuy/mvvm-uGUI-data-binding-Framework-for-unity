using MVVM.Runtime.Binders;
using UnityEngine;
using UnityEngine.UI;

namespace Core.MVVM.Runtime.Core.Binders.Implementations
{
    [AddComponentMenu("MVVM/Binders/Toggle Binder")]
    [RequireComponent(typeof(Toggle))]
    public class ToggleBinder : TwoWayBinder<bool, bool>
    {
        [SerializeField] private Toggle _toggle;

        private void Awake()
        {
            _toggle ??= GetComponent<Toggle>();
            base.Awake();
        }

        protected override void ApplyTargetValue(bool value)
        {
            _toggle.isOn = value;
        }

        protected override void SubscribeToUiEvents()
        {
            _toggle.onValueChanged.RemoveListener(OnUiValueChanged);
            _toggle.onValueChanged.AddListener(OnUiValueChanged);
        }

        protected override void UnsubscribeFromUiEvents()
        {
            _toggle.onValueChanged.RemoveListener(OnUiValueChanged);
        }
    }
}