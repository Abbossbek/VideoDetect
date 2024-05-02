using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace VideoDetect.ViewModels
{
    public class BindableBase
    {
        public event PropertyChangedEventHandler PropertyChanged;
        // Insert SetProperty below here
        protected virtual T SetProperty<T>(ref T backingVariable, T value, [CallerMemberName] string propertyName = null)
        {
            if (!EqualityComparer<T>.Default.Equals(backingVariable, value))
            {
                backingVariable = value;
                OnPropertyChanged(propertyName);
            }
            return backingVariable;
        }
        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChangedEventHandler handler = this.PropertyChanged;
            if (handler != null)
            {
                var e = new PropertyChangedEventArgs(propertyName);
                handler(this, e);
            }
        }
    }
}