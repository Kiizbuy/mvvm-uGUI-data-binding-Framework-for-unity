using UnityEngine.Scripting;

namespace MVVM.Runtime.Binders
{
    [Preserve]
    public abstract class TwoWayBinder<TSource, TTarget> : OneWayBinder<TSource, TTarget>
    {
        private bool _isUpdatingFromVm; // recursion guard

        protected override void OnViewModelValueChanged()
        {
            _isUpdatingFromVm = true;
            base.OnViewModelValueChanged();
            _isUpdatingFromVm = false;
        }

        protected void OnUiValueChanged(TTarget newTargetValue)
        {
            if (_isUpdatingFromVm)
                return;

            var source = ConvertBackValue<TSource, TTarget>(newTargetValue);
            Setter?.Invoke(source);
        }
        
        protected abstract void SubscribeToUiEvents();
        protected abstract void UnsubscribeFromUiEvents();

        protected virtual void OnEnable()
        {
            SubscribeToUiEvents();
        }

        protected virtual void OnDisable()
        {
            UnsubscribeFromUiEvents();
        }
    }
}