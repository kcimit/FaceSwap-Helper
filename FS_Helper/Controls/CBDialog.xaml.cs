using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace FS_Helper
{
    /// <summary>
    /// Interaction logic for CBDialog.xaml
    /// </summary>
    public partial class CBDialog : Window
    {
        CBViewModel vm;
        public bool Selected { get; set; }
        public string SelectedItem
        {
            get
            {
                if (vm == null) return null;
                return vm.SelectedItem;
            }
        }
        public CBDialog(string title, string message, List<string> items)
        {
            InitializeComponent();
            Selected = false;

            vm = new CBViewModel(title, message, items);
            this.DataContext = vm;
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            Selected = false;
            this.Close();
        }

        private void Ok_Click(object sender, RoutedEventArgs e)
        {
            Selected = true;
            this.Close();
        }
    }

    public class CBViewModel : INotifyPropertyChanged
    {
        static SynchronizationContext uiSynchronizationContext;
        List<string> _items;
        string _title, _message;
        public string SelectedItem { get; set; }

        public List<string> Items
        {
            get
            {
                return _items;
            }
            set
            {
                _items = value;
                OnPropertyChanged(nameof(Items));
            }
        }

        public string Title
        {
            get { return _title; }
            set
            {
                if (_title == value) return;
                _title = value;
                OnPropertyChanged(nameof(Title));
            }
        }

        public string Message
        {
            get { return _message; }
            set
            {
                if (_message == value) return;
                _message = value;
                OnPropertyChanged(nameof(Message));
            }
        }

        public CBViewModel(string title, string message, List<string> items)
        {
            uiSynchronizationContext = SynchronizationContext.Current;
            Title = title;
            Message = message;
            Items = items;
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged(string propertyName)
        {
            uiSynchronizationContext.Post(
                 o => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName))
                , null
              );
        }
    }


}
