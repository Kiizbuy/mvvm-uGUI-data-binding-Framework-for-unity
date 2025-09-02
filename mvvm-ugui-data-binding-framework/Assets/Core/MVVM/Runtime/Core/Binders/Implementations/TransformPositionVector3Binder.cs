using MVVM.Runtime.Binders;
using UnityEngine;

namespace Core.MVVM.Runtime.Core.Binders.Implementations
{
    [AddComponentMenu("MVVM/Binders/TransformPosition Binder (Vector3)")]
    public class TransformPositionVector3Binder :  OneWayBinder<Vector3, Vector3>
    {
        [SerializeField] private Transform _transform;
        protected override void ApplyTargetValue(Vector3 value)
        {
            _transform.position = value;
        }
    }
}