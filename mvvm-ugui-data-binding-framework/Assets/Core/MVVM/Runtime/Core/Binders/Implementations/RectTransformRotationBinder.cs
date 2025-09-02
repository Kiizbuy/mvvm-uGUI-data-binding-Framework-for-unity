using MVVM.Runtime.Binders;
using UnityEngine;

namespace Core.MVVM.Runtime.Core.Binders.Implementations
{
    [AddComponentMenu("MVVM/Binders/RectTransformRotation Binder")]
    public class RectTransformRotationBinder : OneWayBinder<Quaternion, Quaternion>
    {
        [SerializeField] private RectTransform _transform;

        protected override void ApplyTargetValue(Quaternion value)
        {
            _transform.rotation = value;
        }
    }
}