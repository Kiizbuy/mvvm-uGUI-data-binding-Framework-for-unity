using MVVM.Runtime.Binders;
using UnityEngine;

namespace Core.MVVM.Runtime.Core.Binders.Implementations
{
    [AddComponentMenu("MVVM/Binders/TransformRotation Binder")]
    public class TransformRotationBinder : OneWayBinder<Quaternion, Quaternion>
    {
        [SerializeField] private Transform _transform;
      
        protected override void ApplyTargetValue(Quaternion value)
        {
            _transform.rotation = value;
        }
    }
}