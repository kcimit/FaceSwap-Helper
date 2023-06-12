using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media.Imaging;
using XnaFan.ImageComparison;

namespace FS_Helper
{
    /// <summary>
    /// Interaction logic for SelectDuplicate.xaml
    /// </summary>
    public partial class FindIdentical : Window
    {
        public bool Aborted { get; set; }
        public List<string> IdenticalReviewed { get; set; }
        public bool UpdateChoice { get; private set; }

        private BitmapImage b1;
        private int current_image_index;
        private string current_image;
        List<System.Windows.Controls.Image> identical;
        private List<string> files;
        private ConnectionViewModel cvm;
        List<List<string>> listIdentical;
        bool analysis_complete;
        public FindIdentical(List<string> files)
        {
            InitializeComponent();
            cvm = new ConnectionViewModel();
            listIdentical = new List<List<string>>();
            DataContext = cvm;
            this.files = files;
            Aborted = false;
            current_image_index = 0;
            analysis_complete = false;

            IdenticalReviewed = new List<string>();

            identical = new List<System.Windows.Controls.Image>()
            {
                DImage_0, DImage_1, DImage_2, DImage_3, DImage_4, DImage_5
            };
        }

        private void Window_ContentRendered(object sender, EventArgs e)
        {
            AnalyzeAll();
            BuildList();
        }

        private void BuildList()
        {
            if (!analysis_complete) return;
            
            current_image_index = 0;
            ShowImage();
        }

        private void AnalyzeAll()
        {
            var tsk =
        Task.Factory.StartNew(() =>
        {
            try
            {
                listIdentical = ImageTool.GetDuplicateImages(files, cvm); 
            }

            catch (Exception e)
            {
                MessageBox.Show($"Problem doing analysis {e.Message}", "Problem", MessageBoxButton.OK, MessageBoxImage.Exclamation);
            }
            finally
            {
                analysis_complete = true;
            }
            cvm.Status = $"Ready. Found {listIdentical.Count} duplicate images";
            return;
        });
        }
        
        private void ShowImage()
        {
            if (!analysis_complete)
            {
                cvm.Status = "Wait for analysis to be completed.";
                return;
            }

            if (!listIdentical.Any())
            {
                cvm.Status = "No identical images found.";
                return;
            }
            
            current_image = listIdentical[current_image_index].First();
            cvm.Status = $"{current_image} ({current_image_index+1}/{listIdentical.Count})";

            var index = 0;
            foreach (var image in identical)
            {
                if (index>= listIdentical[current_image_index].Count)
                {
                    image.Source = null;
                }
                else
                { 
                    // ... Create a new BitmapImage.
                    b1 = new BitmapImage();
                    b1.BeginInit();
                    b1.CacheOption = BitmapCacheOption.OnLoad;
                    b1.UriSource = new Uri(listIdentical[current_image_index][index]);
                    b1.EndInit();
                    image.Source = b1;
                }
                index++;
            }
        }

        private void CloseDialog()
        {
            this.Close();
        }

        private void BtAbort_OnClick(object sender, RoutedEventArgs e)
        {
            Aborted = true;
            CloseDialog();
        }

        private void BtPrev_Click(object sender, RoutedEventArgs e)
        {
            PrevImage();
        }

        private void PrevImage()
        {
            if (current_image_index == 0) return;
            current_image_index--;
            ShowImage();
        }

        private void BtNext_Click(object sender, RoutedEventArgs e)
        {
            NextImage();
        }

        private void NextImage()
        {
            if (current_image_index == listIdentical.Count - 1) return;
            current_image_index++;
            ShowImage();
        }

        private void BtConfirm_Click(object sender, RoutedEventArgs e)
        {
            Aborted = false;
            IdenticalReviewed = new List<string>();
            foreach (var files in listIdentical)
            {
                long size = -1;
                string fileToKeep = "";
                bool preferXSeg = false;
                foreach (var file in files)
                {
                    string folder = new DirectoryInfo(System.IO.Path.GetDirectoryName(file)).FullName;
                    if (File.Exists(Path.Combine(folder, "aligned_xseg", Path.GetFileName(file))))
                    {
                        fileToKeep = file;
                        preferXSeg = true;
                    }
                    if (preferXSeg) continue;
                    var fi = new FileInfo(file);
                    if (fi.Length>size)
                    {
                        size = fi.Length;
                        fileToKeep = file;
                    }
                }
                if (!string.IsNullOrEmpty(fileToKeep))
                {
                    foreach (var file in files)
                    {
                        if (!fileToKeep.Equals(file))
                            IdenticalReviewed.Add(file);
                    }
                }
            }
            CloseDialog();
        }
    }
}
