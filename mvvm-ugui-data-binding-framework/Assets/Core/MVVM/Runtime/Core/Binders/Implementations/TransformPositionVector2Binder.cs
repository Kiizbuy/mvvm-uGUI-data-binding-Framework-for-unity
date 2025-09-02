using MVVM.Runtime.Binders;
using UnityEngine;

namespace Core.MVVM.Runtime.Core.Binders.Implementations
{
    [AddComponentMenu("MVVM/Binders/TransformPosition Binder (Vector2)")]
    public class TransformPositionVector2Binder :  OneWayBinder<Vector2, Vector2>
    {
        [SerializeField] private Transform _transform;
       
        protected override void ApplyTargetValue(Vector2 value)
        {
            _transform.position = value;
        }
    }
}