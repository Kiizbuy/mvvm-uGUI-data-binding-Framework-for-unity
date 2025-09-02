using MVVM.Runtime.Binders;
using UnityEngine;

namespace Core.MVVM.Runtime.Core.Binders.Implementations
{
    [AddComponentMenu("MVVM/Binders/RectTransformPosition Binder (Vector3)")]
    public class RectTransformPositionVector3Binder :  OneWayBinder<Vector3, Vector3>
    {
        [SerializeField] private RectTransform _rectTransform;
        
        protected override void ApplyTargetValue(Vector3 value)
        {
            _rectTransform.position = value;
        }
    }
}