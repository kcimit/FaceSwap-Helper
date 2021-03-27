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
    public partial class SelectDuplicate : Window
    {
        public string SelectedName { get; set; }
        public bool Aborted { get; set; }
        private readonly string _firstFile;
        private readonly string _secondFile;
        private BitmapImage _b1, _b2;

        public SelectDuplicate(string firstFile, string secondFile)
        {
            InitializeComponent();
            _firstFile = firstFile;
            _secondFile = secondFile;
            Aborted = false;
        }

        private void CloseDialog()
        {
            _b1.UriSource = null;
            _b2.UriSource = null;
            FirstImage.Source = null;
            SecondImage.Source = null;
            this.Close();
        }

        private void BtSelectFirst_OnClick(object sender, RoutedEventArgs e)
        {
            SelectedName = _firstFile;
            CloseDialog();
        }

        private void BtSelectSecond_OnClick(object sender, RoutedEventArgs e)
        {
            SelectedName = _secondFile;
            CloseDialog();
        }

        private void FirstImage_OnLoaded(object sender, RoutedEventArgs e)
        {
            // ... Create a new BitmapImage.
            _b1 = new BitmapImage();
            _b1.BeginInit();
            _b1.CacheOption = BitmapCacheOption.OnLoad;
            _b1.UriSource = new Uri(_firstFile);
            _b1.EndInit();

            // ... Get Image reference from sender.
            // ... Assign Source.
            if (sender is Image image) image.Source = _b1;
        }

        private void SecondImage_OnLoaded(object sender, RoutedEventArgs e)
        {
            // ... Create a new BitmapImage.
            _b2 = new BitmapImage();
            _b2.BeginInit();
            _b2.CacheOption = BitmapCacheOption.OnLoad;
            _b2.UriSource = new Uri(_secondFile);
            _b2.EndInit();

            // ... Get Image reference from sender.
            // ... Assign Source.
            if (sender is Image image) image.Source = _b2;
        }

        private void BtAbort_OnClick(object sender, RoutedEventArgs e)
        {
            Aborted = true;
            CloseDialog();
        }
    }
}
