using UnityEngine.Scripting;

namespace MVVM.Runtime.Binders
{
    [Preserve]
    public abstract class OneWayBinder<TSource, TTarget> : BinderBase
    {
        protected override void OnViewModelValueChanged()
        {
            // 1) Get Raw data from VM
            var raw = Getter();
            // 2) Do Cast
            var source = CastValue<TSource>(raw);
            // 3) Convert data for type with ui handle
            var target = ConvertValue<TSource, TTarget>(source);
            // 4) Apply to UI
            ApplyTargetValue(target);
        }

        protected abstract void ApplyTargetValue(TTarget value);
    }
}