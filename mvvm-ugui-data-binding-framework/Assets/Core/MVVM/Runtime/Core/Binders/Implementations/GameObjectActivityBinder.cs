using MVVM.Runtime.Binders;
using UnityEngine;

namespace Core.MVVM.Runtime.Core.Binders.Implementations
{
    [AddComponentMenu("MVVM/Binders/GameObjectActivity Binder")]
    public class GameObjectActivityBinder : OneWayBinder<bool, bool>
    {
        [SerializeField] private GameObject _gameObject;

        private void Awake()
        {
            base.Awake();
        }

        protected override void ApplyTargetValue(bool value)
        {
            _gameObject.SetActive(value);
        }
    }
}