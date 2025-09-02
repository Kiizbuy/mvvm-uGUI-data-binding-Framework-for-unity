using System.ComponentModel;

namespace MVVM.Runtime.Core
{
    public interface IViewModel : INotifyPropertyChanged
    {
        void NotifyAllPropertiesChanged();
    }

}