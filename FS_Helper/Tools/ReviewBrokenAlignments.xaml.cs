using Services;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
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
using XnaFan.ImageComparison;

namespace FS_Helper
{
    /// <summary>
    /// Interaction logic for SelectDuplicate.xaml
    /// </summary>
    public partial class ReviewBrokenAlignments : Window
    {
        public bool Aborted { get; set; }
        public List<string> BrokenReviewed { get; set; }
        public bool UpdateChoice { get; private set; }

        private BitmapImage b1;
        private int current_image_index;
        private string current_image;
        List<System.Windows.Controls.Image> alignments;
        List<System.Windows.Controls.Image> alignments_overlay;
        private string last_image;
        Bitmap thrash;
        private List<string> aligns;
        private string path;
        private AlignmentsViewModel cvm;
        Dictionary<int, List<int>> Diffs;
        List<int> broken;
        List<int> to_remove;
        bool analysis_complete;
        public ReviewBrokenAlignments(List<string> aligns, string path)
        {
            InitializeComponent();
            cvm = new AlignmentsViewModel(this);
            broken = new List<int>();
            DataContext = cvm;
            Diffs = new Dictionary<int, List<int>>();
            to_remove = new List<int>();
            this.aligns = aligns;
            this.path = path;
            Aborted = false;
            last_image = null;
            current_image_index = 0;
            analysis_complete = false;
            thrash = FS_Helper.Properties.Resources.thrash;

            BrokenReviewed = new List<string>();

            alignments = new List<System.Windows.Controls.Image>()
            {
                DImage_0, DImage_1, DImage_2, DImage_3, DImage_4, DImage_5, DImage_6
            };
            alignments_overlay = new List<System.Windows.Controls.Image>()
            {
                DImage_0_Overlay, DImage_1_Overlay, DImage_2_Overlay, DImage_3_Overlay, DImage_4_Overlay, DImage_5_Overlay, DImage_6_Overlay
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
            broken = new List<int>();
            int last_broken_index = 0;
            foreach (var d in Diffs)
            {
                if (d.Key == 0) continue;
                if (d.Value.First() > cvm.Threshold)
                {
                    // Last broken image was previous one so need to double check with one ahead
                    if (last_broken_index > 1 && d.Key == last_broken_index + 1)
                    {
                        if (d.Value[1] < cvm.Threshold) continue;
                    }
                    broken.Add(d.Key);
                    last_broken_index = d.Key;
                }
            }
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
                for (int cnt = 0; cnt < aligns.Count; cnt++)
                {
                    if (cnt == 0) continue;
                    cvm.Status = $"Analyzing file {cnt}/{aligns.Count}";
                    Diffs.Add(cnt, new List<int>());
                    for (int i = 1; i <= Math.Min(3, cnt + 1); i++)
                    {
                        Diffs[cnt].Add((int)(100.0 * ImageTool.GetPercentageDifference(System.IO.Path.Combine(path, aligns[cnt - 1]), System.IO.Path.Combine(path, aligns[cnt]))));
                    }
                }
            }

            catch (Exception e)
            {
                MessageBox.Show($"Problem doing analysis {e.Message}", "Problem", MessageBoxButton.OK, MessageBoxImage.Exclamation);
            }
            finally
            {
                analysis_complete = true;
            }
            return;
        });
        }
        
        private void ShowImage()
        {
            if (!analysis_complete)
            {
                cvm.CurrentAlignment = "Wait for analysis to be completed.";
                return;
            }


            if (!broken.Any())
            {
                cvm.CurrentAlignment = "Decrease threshold to get more results";
                return;
            }
            var vl = broken[current_image_index];
            current_image= aligns[vl];
            cvm.CurrentAlignment = $"{current_image} ({current_image_index+1}/{broken.Count})";

            var index = 0;
            foreach (var image in alignments)
            {
                var ind = vl + index - 3;
                if (ind<0 || ind==aligns.Count-1)
                {
                    image.Source = null;
                }
                else
                { 
                    // ... Create a new BitmapImage.
                    b1 = new BitmapImage();
                    b1.BeginInit();
                    b1.CacheOption = BitmapCacheOption.OnLoad;

                    b1.UriSource = new Uri(System.IO.Path.Combine(path, aligns[ind]));
                    b1.EndInit();
                    image.Source = b1;
                }
                index++;
            }
            index = 0;
            foreach (var image in alignments_overlay)
            {
                var ind = vl + index - 3;
                if (ind < 0 || ind == aligns.Count - 1 || !to_remove.Contains(ind))
                {
                    image.Source = null;
                }
                else
                {
                    using (MemoryStream memory = new MemoryStream())
                    {
                        thrash.Save(memory, ImageFormat.Png);
                        memory.Position = 0;
                        var bitmapImage = new BitmapImage();
                        bitmapImage.BeginInit();
                        bitmapImage.StreamSource = memory;
                        bitmapImage.CacheOption = BitmapCacheOption.OnLoad;
                        bitmapImage.EndInit();
                        image.Source = bitmapImage;
                    }
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
            to_remove = new List<int>();
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
            if (current_image_index == broken.Count - 1) return;
            current_image_index++;
            ShowImage();
        }

        private void BtConfirm_Click(object sender, RoutedEventArgs e)
        {
            Aborted = false;
            BrokenReviewed = new List<string>();
            foreach (var ind in to_remove)
                BrokenReviewed.Add(aligns[ind]);
            CloseDialog();
        }

        private void DImage_MouseDown(object sender, MouseButtonEventArgs e)
        {
            var mouseWasDownOn = e.Source as FrameworkElement;
            if (mouseWasDownOn != null)
            {
                if (mouseWasDownOn.Name.Equals("DImage_3"))
                {
                    SwitchChoice();
                    NextImage();
                }
            }
        }

        private void SwitchChoice()
        {
            if (!broken.Any() || current_image_index>=broken.Count) return;
            var vl = broken[current_image_index];
            if (to_remove.Contains(vl)) to_remove.Remove(vl);
            else to_remove.Add(vl);
        }
        private void BtRemove_Click(object sender, RoutedEventArgs e)
        {
            if (!broken.Any() || current_image_index >= broken.Count) return;
            var vl = broken[current_image_index];
            if (!to_remove.Contains(vl)) to_remove.Add(vl);
            
        }

        private void BtKeep_Click(object sender, RoutedEventArgs e)
        {
            if (!broken.Any() || current_image_index >= broken.Count) return;
            var vl = broken[current_image_index];
            if (to_remove.Contains(vl)) to_remove.Remove(vl);
        }
        public void UpdateThreshold()
        {
            
            BuildList();
        }
    }


    public class AlignmentsViewModel : ViewModelBase
    {
        private string _current_alignment;
        private int _threshold;
        ReviewBrokenAlignments _main;

        public AlignmentsViewModel (ReviewBrokenAlignments main)
        {
            _threshold = 80;
            uiSynchronizationContext = SynchronizationContext.Current;
            _main = main;
        }

        public string CurrentAlignment
        {
            get => _current_alignment;
            set
            {
                if (_current_alignment == value) return;
                _current_alignment = value;
                OnPropertyChanged(nameof(CurrentAlignment));
            }
        }
        public int Threshold
        {
            get => _threshold;
            set
            {
                if (_threshold == value) return;
                _threshold = value;
                OnPropertyChanged(nameof(Threshold));
                _main.UpdateThreshold();
            }
        }
    }
}
