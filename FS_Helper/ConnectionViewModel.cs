using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace FS_Helper
{
    public class ConnectionViewModel : INotifyPropertyChanged
    {
        private string _status;

        static SynchronizationContext _uiSynchronizationContext;

        public ConnectionViewModel()
        {
            _uiSynchronizationContext = SynchronizationContext.Current;
        }

        public string Status
        {
            get => _status;
            set
            {
                if (_status == value) return;
                _status = value;
                OnPropertyChanged("Status");
            }
        }

        protected virtual void OnPropertyChanged(string propertyName)
        {
            _uiSynchronizationContext.Post(
                 o => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName))
                , null
              );
        }
        public event PropertyChangedEventHandler PropertyChanged;
    }
}
