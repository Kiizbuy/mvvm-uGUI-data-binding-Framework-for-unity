using MVVM.Runtime.Binders;
using UnityEngine;
using UnityEngine.UI;

namespace Core.MVVM.Runtime.Core.Binders.Implementations
{
    [AddComponentMenu("MVVM/Binders/Slider Binder")]
    [RequireComponent(typeof(Slider))]
    [SupportedBindingTypes(typeof(float))]
    public sealed class SliderBinder : TwoWayBinder<float, float>
    {
        private Slider _slider;

        private void Awake()
        {
            _slider = GetComponent<Slider>();
            base.Awake();
        }

        protected override void ApplyTargetValue(float value)
        {
            if (_slider != null && !_slider.wholeNumbers && Mathf.Abs(_slider.value - value) > 0.001f)
                _slider.value = value;
        }

        protected override void SubscribeToUiEvents()
        {
            if (_slider != null)
                _slider.onValueChanged.AddListener(OnUiValueChanged);
        }

        protected override void UnsubscribeFromUiEvents()
        {
            if (_slider != null)
                _slider.onValueChanged.RemoveListener(OnUiValueChanged);
        }
    }
}