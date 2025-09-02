using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using UnityEngine.Scripting;

namespace MVVM.Runtime.Core
{
    [Preserve]
    public abstract class ViewModel : IViewModel
    {
        public event PropertyChangedEventHandler PropertyChanged;

        [Preserve]
        protected void SetProperty<T>(ref T field, T value, [CallerMemberName] string propertyName = null)
        {
            if (EqualityComparer<T>.Default.Equals(field, value)) 
                return;
            
            field = value;
            OnPropertyChanged(propertyName);
        }

        [Preserve]
        private void OnPropertyChanged(string propertyName)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        
        [Preserve]
        public void NotifyAllPropertiesChanged()
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(string.Empty));
    }
}