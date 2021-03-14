using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
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
        public List<string> FilesToRemove { get; set; }
        private readonly List<string> _files;
        private List<string> files_to_remove;
        private BitmapImage b1;
        private int current_image;
        Bitmap icon;

        public ShowDeletedAlignmentSource(List<string> files)
        {
            InitializeComponent();
            _files = files;
            files_to_remove = new List<string>();
            Aborted = false;
            current_image = 0;
            FilesToRemove = new List<string>();
            tbRemoveCount.Text = $"Files marked for removal: {files_to_remove.Count}";
            icon = FS_Helper.Properties.Resources.thrash;
        }

        private void Window_ContentRendered(object sender, EventArgs e)
        {
            ShowImage();
        }

        private void ShowImage()
        {
            try
            {
                using (Stream BitmapStream = System.IO.File.Open(_files[current_image], System.IO.FileMode.Open))
                {
                    System.Drawing.Image img = System.Drawing.Image.FromStream(BitmapStream);

                    using (var bmp = new Bitmap(img))
                    {
                        var src = System.Windows.Interop.Imaging.CreateBitmapSourceFromHBitmap(bmp.GetHbitmap(), IntPtr.Zero, System.Windows.Int32Rect.Empty, BitmapSizeOptions.FromEmptyOptions());
                        /*
                        // ... Create a new BitmapImage.
                        b1 = new BitmapImage();
                        b1.BeginInit();
                        b1.CacheOption = BitmapCacheOption.OnLoad;
                        b1.StreamSource = BitmapStream;
                        //b1.UriSource = new Uri(_files[current_image]);
                        b1.EndInit();*/
                        DImage.Source = src;
                    }
                }
                tbFileName.Text = _files[current_image];

                if (files_to_remove.Contains(_files[current_image]))
                    using (MemoryStream memory = new MemoryStream())
                    {
                        icon.Save(memory, ImageFormat.Png);
                        memory.Position = 0;
                        BitmapImage bitmapImage = new BitmapImage();
                        bitmapImage.BeginInit();
                        bitmapImage.StreamSource = memory;
                        bitmapImage.CacheOption = BitmapCacheOption.OnLoad;
                        bitmapImage.EndInit();
                        ThrashImage.Source = bitmapImage;
                    }
                else
                    ThrashImage.Source = null;
            }
            catch { }
        }

        private void CloseDialog()
        {
            //b1.UriSource = null;
            DImage.Source = null;
            this.Close();
        }

        private void BtKeepFile_OnClick(object sender, RoutedEventArgs e)
        {
            if (files_to_remove.Contains(_files[current_image]))
                files_to_remove.Remove(_files[current_image]);
            tbRemoveCount.Text = $"Files marked for removal: {files_to_remove.Count}";
            NextImage();
        }

        private void BtRemoveFile_OnClick(object sender, RoutedEventArgs e)
        {
            if (!files_to_remove.Contains(_files[current_image]))
                files_to_remove.Add(_files[current_image]);
            tbRemoveCount.Text = $"Files marked for removal: {files_to_remove.Count}";
            NextImage();
        }

        private void BtAbort_OnClick(object sender, RoutedEventArgs e)
        {
            Aborted = true;
            files_to_remove = new List<string>();
            CloseDialog();
        }

        private void BtPrev_Click(object sender, RoutedEventArgs e)
        {
            PrevImage();
        }

        private void PrevImage()
        {
            if (current_image != 0) current_image--;
            ShowImage();
        }

        private void BtNext_Click(object sender, RoutedEventArgs e)
        {
            NextImage();
        }

        private void NextImage()
        {
            if (current_image != _files.Count - 1) current_image++;
            ShowImage();
        }

        private void BtConfirm_Click(object sender, RoutedEventArgs e)
        {
            FilesToRemove = files_to_remove;
            CloseDialog();
        }
    }
}
