using Microsoft.Win32;
using System;
using System.ComponentModel;
using System.IO;
using System.Threading;
using System.Windows.Input;

namespace Services
{
    //https://zamjad.wordpress.com/2014/01/25/file-dialog-viewmodel/
    public class FileDialogViewModel : ViewModelBase
    {
        public FileDialogViewModel()
        {
            this.SaveCommand = new RelayCommand(this.SaveFile);
            this.OpenCommand = new RelayCommand(this.OpenFile);
            this.ReadCommand = new RelayCommand(this.ReadFile);
        }

        #region Properties

        public Stream Stream
        {
            get;
            set;
        }

        public string Extension
        {
            get;
            set;
        }

        public string Filter
        {
            get;
            set;
        }

        public string FileName
        {
            get;
            set;
        }

        public string Title
        {
            get;
            set;
        }

        public bool Multiselect
        {
            get;
            set;
        }
        public ICommand ReadCommand
        {
            get;
            set;
        }

        public ICommand OpenCommand
        {
            get;
            set;
        }

        public ICommand SaveCommand
        {
            get;
            set;
        }

        #endregion

        private void OpenFile(object parameter)
        {
            FileService fileServices = new FileService();
            this.FileName = fileServices.OpenFile(this.Extension, this.Filter, this.Title, this.Multiselect);
        }

        private void ReadFile(object parameter)
        {
            FileService fileServices = new FileService();
            this.Stream = fileServices.ReadFile(this.Extension, this.Filter, this.Title, this.Multiselect);
        }

        private void SaveFile(object parameter)
        {
            FileService fileServices = new FileService();
            this.Stream = fileServices.SaveFile(this.Extension, this.Filter);
        }
    }

    public sealed class FileService
    {
        public Stream ReadFile(string defaultExtension, string filter, string title, bool multiselect)
        {
            var fd = new OpenFileDialog
            {
                DefaultExt = defaultExtension,
                Filter = filter,
                Title = title,
                Multiselect = multiselect
            };

            bool? result = fd.ShowDialog();

            return result.Value ? fd.OpenFile() : null;
        }

        public string OpenFile(string defaultExtension, string filter, string title, bool multiselect)
        {
           var fd = new OpenFileDialog
            {
                DefaultExt = defaultExtension,
                Filter = filter,
                Title = title,
                Multiselect = multiselect
            };

            bool? result = fd.ShowDialog();
            return (result == true) ? fd.FileName : null;
        }

        public Stream SaveFile(string defaultExtension, string filter)
        {
            SaveFileDialog fd = new SaveFileDialog();
            fd.DefaultExt = defaultExtension;
            fd.Filter = filter;

            bool? result = fd.ShowDialog();

            return result.Value ? fd.OpenFile() : null;
        }
    }

    public abstract class ViewModelBase : INotifyPropertyChanged
    {
        public SynchronizationContext uiSynchronizationContext;
        private string _status;

        private bool controlsEnabled;
        public Boolean ControlsEnabled
        {
            get { return controlsEnabled; }
            set
            {
                controlsEnabled = value;
                OnPropertyChanged(nameof(ControlsEnabled));
            }
        }
        public void OnPropertyChanged(string propertyName)
        {
            uiSynchronizationContext.Post(
                 o => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName))
                , null
              );
        }
        public string Status
        {
            get => _status;
            set
            {
                if (_status == value) return;
                _status = value;
                OnPropertyChanged(nameof(Status));
            }
        }

        #region NotifyPropertyChanged Methods

        public event PropertyChangedEventHandler PropertyChanged;

        #endregion
    }

    public sealed class RelayCommand : ICommand
    {
        private Action<object> function;

        public RelayCommand(Action<object> function)
        {
            this.function = function;
        }

        public bool CanExecute(object parameter)
        {
            return this.function != null;
        }

        public void Execute(object parameter)
        {
            if (this.function != null)
            {
                this.function(parameter);
            }
        }

        public event EventHandler CanExecuteChanged
        {
            add { CommandManager.RequerySuggested += value; }
            remove { CommandManager.RequerySuggested -= value; }
        }
    }
}
