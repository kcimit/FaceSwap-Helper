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
using XnaFan.ImageComparison;

namespace FS_Helper
{
    /// <summary>
    /// Interaction logic for SelectDuplicate.xaml
    /// </summary>
    public partial class ReviewAlignments : Window
    {
        Dictionary<string, List<string>> trashed { get; set; }
        public bool UpdateChoice { get; private set; }

        private Dictionary<string, string> alignments_reviewed;
        private Dictionary<string, string> elements_displayed;
        private BitmapImage b1;
        private int current_image_index;
        private string current_image;
        List<System.Windows.Controls.Image> alignments;
        List<System.Windows.Controls.Image> alignments_overlay;
        private string last_image;
        Bitmap icon, checkmark, stop;
        private bool showSingle;
        private Dictionary<string, List<string>> pairs;
        private string dir;
        private string target;
        private string path;
        private int updateCount;

        public ReviewAlignments(string _dir, string _target)
        {
            InitializeComponent();
            
            pairs= new Dictionary<string, List<string>>();
            dir = _dir;
            target = _target;

            alignments_reviewed = new Dictionary<string, string>();
            trashed = new Dictionary<string, List<string>>();
            last_image = null;
            current_image_index = 0;
            elements_displayed = new Dictionary<string, string>();
            tbRemoveCount.Text = $"Alignments reviewed: {alignments_reviewed.Count}";
            icon = Properties.Resources.thrash;
            checkmark = Properties.Resources.checkmark;
            stop = Properties.Resources.stop;
            updateCount = 0;
            alignments = new List<System.Windows.Controls.Image>()
            {
                DImage_0, DImage_1, DImage_2, DImage_3, DImage_4, DImage_5, DImage_6, DImage_7
            };
            alignments_overlay = new List<System.Windows.Controls.Image>()
            {
                DImage_0_Overlay, DImage_1_Overlay, DImage_2_Overlay, DImage_3_Overlay, DImage_4_Overlay, DImage_5_Overlay, DImage_6_Overlay, DImage_7_Overlay
            };
        }

        void UpdatePairs()
        {
            var dirInfo = new DirectoryInfo(path);
            var info = dirInfo.GetFiles("*.*");
            foreach (var f in info)
            {
                var name = System.IO.Path.GetFileNameWithoutExtension(f.FullName);
                var suffix = name.Substring(name.Length - 2, 2);
                if (!suffix[0].Equals('_')) continue;

                var fname = name.Substring(0, name.Length - 2) + f.Extension;

                if (!pairs.ContainsKey(fname))
                    pairs.Add(fname, new List<string>());
                var exiName = name + f.Extension;
                if (!pairs[fname].Contains(exiName))
                    pairs[fname].Add(exiName);
            }

            if (!showSingle)
                foreach (var item in pairs.Where(kvp => kvp.Value.Count < 2).ToList())
                    pairs.Remove(item.Key);
        }

        private void Window_ContentRendered(object sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(dir))
            {
                MessageBox.Show("Select workspace directory first.", "Problem", MessageBoxButton.OK, MessageBoxImage.Exclamation);
                Close();
            }

            path = System.IO.Path.Combine(dir, target, "aligned");

            if (!Directory.Exists(path))
            {
                MessageBox.Show("Aligned directory does not exist.", "Problem", MessageBoxButton.OK, MessageBoxImage.Exclamation);
                return;
            }
            
            showSingle = MessageBox.Show("Do you want also show single face results?", "Question", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes;

            pairs = new Dictionary<string, List<string>>();
            UpdatePairs();

            if (pairs.Count == 0)
            {
                MessageBox.Show("No alignments with multiple faces found.", "Problem", MessageBoxButton.OK, MessageBoxImage.Exclamation);
                Close();
            }
            else
                ShowImage();
        }

        private void ShowImage()
        {
            var vl = pairs.ElementAt(current_image_index).Value;
            current_image= pairs.ElementAt(current_image_index).Key;
            tbFileName.Text = current_image;
            elements_displayed = new Dictionary<string, string>();

            // setting previously chosen image to be the 1st one
            if (alignments_reviewed.ContainsKey(current_image) && alignments_reviewed[current_image]!="stop")
            {
                var vl_new = new List<string>() { alignments_reviewed[current_image] };
                foreach (var v in vl.Where(r => !r.Equals(alignments_reviewed[current_image])))
                    vl_new.Add(v);
                vl = vl_new;
            }
            else // sort by doing histogram comparison
            if (!string.IsNullOrEmpty(last_image))
            {
                var cmp = new Dictionary<string, int>();
                foreach (var v in vl)
                {
                    int difference = (int)(100.0*ImageTool.GetPercentageDifference(last_image, System.IO.Path.Combine(path, v)));
                    cmp.Add(v, difference);
                }
                var vl_new = new List<string>();
                foreach (var kvp in cmp.OrderBy(r => r.Value))
                    vl_new.Add(kvp.Key);
                vl = vl_new;
            }

            var index = 0;
            foreach (var image in alignments)
            {
                if (index == 0)
                {
                    using (MemoryStream memory = new MemoryStream())
                    {
                        stop.Save(memory, ImageFormat.Png);
                        memory.Position = 0;
                        BitmapImage bitmapImage = new BitmapImage();
                        bitmapImage.BeginInit();
                        bitmapImage.StreamSource = memory;
                        bitmapImage.CacheOption = BitmapCacheOption.OnLoad;
                        bitmapImage.EndInit();
                        image.Source = bitmapImage;
                    }
                    elements_displayed.Add(image.Name, "stop");
                }
                else if (index > vl.Count)
                    image.Source = null;
                else
                {
                    // ... Create a new BitmapImage.
                    b1 = new BitmapImage();
                    b1.BeginInit();
                    b1.CacheOption = BitmapCacheOption.OnLoad;

                    b1.UriSource = new Uri(System.IO.Path.Combine(path, vl[index-1]));
                    b1.EndInit();
                    image.Source = b1;
                    elements_displayed.Add(image.Name, vl[index-1]);
                }
                index++;
            }
            DisplayOverlay();
        }

        private void CloseDialog()
        {
            int dcount = 0;
            int rcount = 0;
            foreach (var del in trashed)
            {
                foreach (var f in del.Value)
                {
                    try
                    {
                        File.Delete(System.IO.Path.Combine(path, f));
                        dcount++;
                    }
                    catch { }
                }
            }
            
            var target_path = System.IO.Path.Combine(dir, target, "aligned_reviewed");
            if (!Directory.Exists(target_path)) Directory.CreateDirectory(target_path);

            foreach (var pair in pairs)
            {
                if (!alignments_reviewed.ContainsKey(pair.Key)) continue;
                if (alignments_reviewed[pair.Key].Equals("stop"))
                {
                    foreach (var f in pair.Value)
                        try
                        {
                            File.Delete(System.IO.Path.Combine(path, f));
                            dcount++;
                        }
                        catch { }
                }
                else
                {
                    foreach (var f in pair.Value)
                    {
                        if (f.Equals(alignments_reviewed[pair.Key]))
                        {
                            File.Move(System.IO.Path.Combine(path, f), System.IO.Path.Combine(target_path, pair.Key));
                            rcount++;
                        }
                        else
                            try
                            {
                                File.Delete(System.IO.Path.Combine(path, f));
                                dcount++;
                            }
                            catch { }
                    }
                }
            }
            MessageBox.Show($"{dcount} files deleted and {rcount} alignments reviewed in aligned_reviewed folder.", "Results", MessageBoxButton.OK, MessageBoxImage.Information);

            this.Close();
        }

        private void BtAbort_OnClick(object sender, RoutedEventArgs e)
        {
            Close();
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
            if (++updateCount % 10 == 0)
                UpdatePairs();

            if (current_image_index == pairs.Count - 1)
            {
                UpdatePairs();
                if (current_image_index == pairs.Count - 1)
                    return;
            }

            current_image_index++;
            ShowImage();
        }
        private void BtConfirm_Click(object sender, RoutedEventArgs e)
        {
            CloseDialog();
        }

        private void DImage_MouseDown(object sender, MouseButtonEventArgs e)
        {
            var mouseWasDownOn = e.Source as FrameworkElement;
            string elementName = "";
            if (mouseWasDownOn != null)
                elementName = mouseWasDownOn.Name;
            if (!elements_displayed.ContainsKey(elementName)) return;
            SetChoiseOn(elementName);
        }

        private void Window_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Space)
            {
                if (!UpdateChoice)
                {
                    e.Handled = true;
                    return;
                }
                if (elements_displayed == null || elements_displayed.Count == 0) return;
                SetChoiseOn(elements_displayed.ElementAt(1).Key);
                e.Handled = true;
            }
        }

        private void BtBatch_Click(object sender, RoutedEventArgs e)
        {
            var batch = new ReviewAlignmentsBatch(pairs, path, current_image_index, last_image);
            batch.ShowDialog();
            if (!batch.Aborted)
            {
                alignments_reviewed = batch.AlignmentsReviewed;
                trashed = batch.Trashed;
                CloseDialog();
            }
        }

        private void SetChoiseOn(string elementName)
        {
            UpdateChoice = false;
            if (alignments_reviewed.ContainsKey(current_image))
                alignments_reviewed[current_image] = elements_displayed[elementName];
            else
            {
                alignments_reviewed.Add(current_image, elements_displayed[elementName]);
                tbRemoveCount.Text = $"Alignments reviewed: {alignments_reviewed.Count}/{pairs.Count}";
            }
            if (elements_displayed[elementName]!="stop")
                last_image = System.IO.Path.Combine(path, elements_displayed[elementName]);
            DisplayOverlay();
            NextImage();
            UpdateChoice = true;
        }

        private void DisplayOverlay()
        {
            foreach (var image in alignments_overlay)
            {
                if (!alignments_reviewed.ContainsKey(current_image) || !elements_displayed.ContainsKey(image.Name.Replace("_Overlay", "")) || elements_displayed[image.Name.Replace("_Overlay","")] != alignments_reviewed[current_image])
                    image.Source = null;
                else
                    using (MemoryStream memory = new MemoryStream())
                    {
                        checkmark.Save(memory, ImageFormat.Png);
                        memory.Position = 0;
                        var bitmapImage = new BitmapImage();
                        bitmapImage.BeginInit();
                        bitmapImage.StreamSource = memory;
                        bitmapImage.CacheOption = BitmapCacheOption.OnLoad;
                        bitmapImage.EndInit();
                        image.Source = bitmapImage;
                    }
            }
        }
    }
}
