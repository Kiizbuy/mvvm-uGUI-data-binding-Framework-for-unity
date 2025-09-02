using MVVM.Runtime.Binders;
using UnityEngine;
using UnityEngine.UI;

namespace Core.MVVM.Runtime.Core.Binders.Implementations
{
    [AddComponentMenu("MVVM/Binders/Image Binder")]
    [RequireComponent(typeof(Image))]
    [SupportedBindingTypes(typeof(Sprite))]
    public sealed class ImageBinder : OneWayBinder<Sprite, Sprite>
    {
        [SerializeField]
        private Image _image;

        private void Awake()
        {
            _image ??= GetComponent<Image>();
            base.Awake();
        }

        protected override void ApplyTargetValue(Sprite value)
        {
            _image.sprite = value;
        }
    }
}