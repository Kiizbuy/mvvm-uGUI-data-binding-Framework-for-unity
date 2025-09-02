using UnityEngine;

namespace MVVM.Runtime.Core
{
    [Bindable]
    public abstract class ScreenViewModelMonoBehaviour : ViewModelMonoBehaviour
    {
        [Bindable]
        public bool IsShown
        {
            get => _isShown;
            set
            {
                SetProperty(ref _isShown, value);
                switch (value)
                {
                    case true:
                        OnShown();
                        break;
                    default:
                        OnHided();
                        break;
                }
            }
        }
        [SerializeField] private bool _isShown;

        public void Show()
        {
            IsShown = true;
        }

        public void Hide()
        {
            IsShown = false;
        }

        protected abstract void OnShown();
        protected abstract void OnHided();

    }
}