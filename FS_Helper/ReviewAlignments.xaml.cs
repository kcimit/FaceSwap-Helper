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
        public bool Aborted { get; set; }
        public Dictionary<string, string> AlignmentsReviewed { get; set; }
        private Dictionary<string, string> alignments_reviewed;
        private Dictionary<string, string> elements_displayed;
        private BitmapImage b1;
        private int current_image_index;
        private string current_image;
        List<System.Windows.Controls.Image> alignments;
        List<System.Windows.Controls.Image> alignments_overlay;
        private string last_image;
        Bitmap icon, checkmark, stop;
        private Dictionary<string, List<string>> pairs;
        private string path;


        public ReviewAlignments(Dictionary<string, List<string>> pairs, string path)
        {
            InitializeComponent();

            this.pairs = pairs;
            this.path = path;
            alignments_reviewed = new Dictionary<string, string>();
            Aborted = false;
            last_image = null;
            current_image_index = 0;
            AlignmentsReviewed = new Dictionary<string, string>();
            elements_displayed = new Dictionary<string, string>();
            tbRemoveCount.Text = $"Alignments reviewed: {alignments_reviewed.Count}";
            icon = FS_Helper.Properties.Resources.thrash;
            checkmark = FS_Helper.Properties.Resources.checkmark;
            stop = FS_Helper.Properties.Resources.stop;

            alignments = new List<System.Windows.Controls.Image>()
            {
                DImage_0, DImage_1, DImage_2, DImage_3, DImage_4, DImage_5, DImage_6, DImage_7
            };
            alignments_overlay = new List<System.Windows.Controls.Image>()
            {
                DImage_0_Overlay, DImage_1_Overlay, DImage_2_Overlay, DImage_3_Overlay, DImage_4_Overlay, DImage_5_Overlay, DImage_6_Overlay, DImage_7_Overlay
            };

        }

        private void Window_ContentRendered(object sender, EventArgs e)
        {
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
                if (index == vl.Count)
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
                else
                    if (index > vl.Count)
                    image.Source = null;
                else
                {
                    // ... Create a new BitmapImage.
                    b1 = new BitmapImage();
                    b1.BeginInit();
                    b1.CacheOption = BitmapCacheOption.OnLoad;

                    b1.UriSource = new Uri(System.IO.Path.Combine(path, vl[index]));
                    b1.EndInit();
                    image.Source = b1;
                    elements_displayed.Add(image.Name, vl[index]);
                }
                index++;
            }
            DisplayOverlay();
        }

        private void CloseDialog()
        {
            this.Close();
        }

        private void BtAbort_OnClick(object sender, RoutedEventArgs e)
        {
            Aborted = true;
            alignments_reviewed = new Dictionary<string, string>();
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
            if (current_image_index == pairs.Count - 1) return;
            current_image_index++;
            ShowImage();
        }

        private void BtConfirm_Click(object sender, RoutedEventArgs e)
        {
            AlignmentsReviewed = alignments_reviewed;
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

        private void SetChoiseOn(string elementName)
        {
            if (alignments_reviewed.ContainsKey(current_image))
                alignments_reviewed[current_image] = elements_displayed[elementName];
            else
            {
                alignments_reviewed.Add(current_image, elements_displayed[elementName]);
                tbRemoveCount.Text = $"Alignments reviewed: {alignments_reviewed.Count}";
            }
            if (elements_displayed[elementName]!="stop")
                last_image = System.IO.Path.Combine(path, elements_displayed[elementName]);
            DisplayOverlay();
            NextImage();
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
                        BitmapImage bitmapImage = new BitmapImage();
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
