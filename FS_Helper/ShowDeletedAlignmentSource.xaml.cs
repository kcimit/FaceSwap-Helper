using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Text;
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
    /// Interaction logic for SelectDuplicate.xaml
    /// </summary>
    public partial class ShowDeletedAlignmentSource : Window
    {
        public bool Aborted { get; set; }
        private readonly string _file;
        private BitmapImage b1;
        public bool? KeepFile { get; set; }

        public ShowDeletedAlignmentSource(string file)
        {
            InitializeComponent();
            this._file = file;
            KeepFile = null;
            Aborted = false;
        }

        private void CloseDialog()
        {
            b1.UriSource = null;
            DImage.Source = null;
            this.Close();
        }
        private void DImage_OnLoaded(object sender, RoutedEventArgs e)
        {
            // ... Create a new BitmapImage.
            b1 = new BitmapImage();
            b1.BeginInit();
            b1.CacheOption = BitmapCacheOption.OnLoad;
            b1.UriSource = new Uri(_file);
            b1.EndInit();

            // ... Get Image reference from sender.
            // ... Assign Source.
            if (sender is Image image) image.Source = b1;
        }

        private void BtKeepFile_OnClick(object sender, RoutedEventArgs e)
        {
            KeepFile = true;
            CloseDialog();
        }

        private void BtRemoveFile_OnClick(object sender, RoutedEventArgs e)
        {
            KeepFile = false;
            CloseDialog();
        }

        private void BtAbort_OnClick(object sender, RoutedEventArgs e)
        {
            Aborted = true;
            CloseDialog();
        }
    }
}
