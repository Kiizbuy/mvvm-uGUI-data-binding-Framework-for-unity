namespace MVVM.Runtime.Binders
{
    public interface IViewModelTemplate
    {
        void InitChildBindings(object viewModel);
    }
}