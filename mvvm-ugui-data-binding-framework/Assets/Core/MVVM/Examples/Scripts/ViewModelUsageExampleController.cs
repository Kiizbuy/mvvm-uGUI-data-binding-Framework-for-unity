using UnityEngine;
using UnityEngine.InputSystem;

namespace Core.MVVM.Examples.Scripts
{
    public class ViewModelUsageExampleController : MonoBehaviour
    {
        [SerializeField] private Sprite[] _randomIcons;
        [SerializeField] private ExampleViewModelMonoBehaviour _viewModel;
        
        private void Update()
        {
            if (Keyboard.current.tKey.wasPressedThisFrame)
            {
                _viewModel.SomeCounter++;
                _viewModel.SomeTest.Add(new ExampleViewModel("First" + UnityEngine.Random.Range(1, 100)));
                _viewModel.Avatar = _randomIcons[Random.Range(0, _randomIcons.Length)];
            }

            if (Keyboard.current.sKey.wasPressedThisFrame)
            {
                _viewModel.SubViewModel.Name = "Changed";
                _viewModel.ExtendedSubViewModel.Exp = "mmm";
                if (_viewModel.SomeTest.Count > 0)
                {
                    _viewModel.SomeTest.RemoveAt(_viewModel.SomeTest.Count - 1);
                }

                _viewModel.Active = !_viewModel.Active;
                (_viewModel.DropdownOptions[0], _viewModel.DropdownOptions[^1]) = (_viewModel.DropdownOptions[^1], _viewModel.DropdownOptions[0]);
            }
        }
    }
}