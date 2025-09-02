using MVVM.Runtime.Binders;
using UnityEngine;

namespace Core.MVVM.Runtime.Core.Binders.Implementations
{
    [AddComponentMenu("MVVM/Binders/RectTransformPosition Binder (Vector2)")]
    public class RectTransformPositionVector2Binder :  OneWayBinder<Vector2, Vector2>
    {
        [SerializeField] private RectTransform _rectTransform;

        protected override void ApplyTargetValue(Vector2 value)
        {
            _rectTransform.position = value;
        }
    }
}