using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace FS_Helper
{
    /// <summary>
    /// Interaction logic for SelectDuplicate.xaml
    /// </summary>
    public partial class ShowDeletedAlignmentSource : Window
    {
        private List<string> files;
        private string dir;
        private string target;
        private List<string> files_to_remove;
        private List<string> files_to_keep;
        private int current_image;
        Bitmap icon;

        public ShowDeletedAlignmentSource(string _dir, string _target)
        {
            InitializeComponent();
            dir = _dir;
            target=_target; 
            files_to_remove = new List<string>();
            files_to_keep=new List<string>();
            current_image = 0;
            
            icon = FS_Helper.Properties.Resources.thrash;
        }

        private void Window_ContentRendered(object sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(dir))
            {
                MessageBox.Show("Select workspace directory first.", "Problem", MessageBoxButton.OK, MessageBoxImage.Exclamation);
                return;
            }

            var pathSource = System.IO.Path.Combine(dir, target);
            var dirInfo = new DirectoryInfo(pathSource);
            var info = dirInfo.GetFiles("*.*");
            //var is_png = info.Count(r => r.Extension.Equals("png")) >
            //             info.Count(r => r.Extension.Equals("jpg"));
            files = new List<string>();
            foreach (var f in info.Where(r => (new string[] { ".png", ".jpg", ".jpeg" }).Contains(r.Extension)))
            {
                var name = f.FullName;
                var alignedName = System.IO.Path.Combine(pathSource, "aligned", $"{System.IO.Path.GetFileNameWithoutExtension(f.Name)}.jpg");
                var alignedName_0 = System.IO.Path.Combine(pathSource, "aligned", $"{System.IO.Path.GetFileNameWithoutExtension(f.Name)}_0.jpg");
                if (File.Exists(alignedName) || File.Exists(alignedName_0)) continue;
                files.Add(name);

            }
            if (!files.Any())
            {
                MessageBox.Show("Nothing to remove.");
                Close();
            }
            tbRemoveCount.Text = $"Files marked for removal: {files_to_remove.Count}";
            ShowImage();
        }

        private void ShowImage()
        {
            try
            {
                using (Stream BitmapStream = System.IO.File.Open(files[current_image], System.IO.FileMode.Open))
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
                tbFileName.Text = files[current_image];

                if (files_to_remove.Contains(files[current_image]))
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
            DImage.Source = null;
            int dcount = 0;

            foreach (var f in files_to_remove)
                try
                {
                    File.Delete(f);
                    dcount++;
                }
                catch { }
            
            if (files_to_remove.Any())
            {
                var target_path = System.IO.Path.Combine(dir, target, "source_to_keep");
                if (!Directory.Exists(target_path)) Directory.CreateDirectory(target_path);
                foreach (var f in files_to_keep)
                    File.Move(f, System.IO.Path.Combine(target_path, System.IO.Path.GetFileName(f)));

            }
            MessageBox.Show($"{dcount} files deleted.", "Results", MessageBoxButton.OK, MessageBoxImage.Information);
            this.Close();
        }

        private void BtKeepFile_OnClick(object sender, RoutedEventArgs e)
        {
            if (files_to_remove.Contains(files[current_image]))
                files_to_remove.Remove(files[current_image]);
            files_to_keep.Add(files[current_image]);
            tbRemoveCount.Text = $"Files marked for removal: {files_to_remove.Count}";
            NextImage();
        }

        private void BtRemoveFile_OnClick(object sender, RoutedEventArgs e)
        {
            if (!files_to_remove.Contains(files[current_image]))
                files_to_remove.Add(files[current_image]);
            if (files_to_keep.Contains(files[current_image]))
                files_to_keep.Remove(files[current_image]);
            tbRemoveCount.Text = $"Files marked for removal: {files_to_remove.Count}";
            NextImage();
        }

        private void BtAbort_OnClick(object sender, RoutedEventArgs e)
        {
            this.Close();
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
            if (current_image != files.Count - 1) current_image++;
            ShowImage();
        }

        private void BtConfirm_Click(object sender, RoutedEventArgs e)
        {
            CloseDialog();
        }

        private void BtRemoveAllFiles_Click(object sender, RoutedEventArgs e)
        {
            var res = MessageBox.Show("Are you sure you want to remove all files", "Question", MessageBoxButton.YesNo, MessageBoxImage.Question);
            files_to_remove = files;
            CloseDialog();
        }
    }
}
