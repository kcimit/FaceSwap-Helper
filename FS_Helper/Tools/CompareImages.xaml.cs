using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media.Imaging;

namespace FS_Helper
{
    /// <summary>
    /// Interaction logic for SelectDuplicate.xaml
    /// </summary>
    public partial class CompareImages : Window
    {
        private BitmapImage b1;
        private int current_image_index;
        private string current_image;
        List<System.Windows.Controls.Image> alignments;
        private Dictionary<string, List<string>> pairs;
        Queue<Action> actionQueue;
        public CompareImages(Dictionary<string, List<string>> pairs)
        {
            InitializeComponent();
            this.pairs = pairs;
            current_image_index = 0;
            alignments = new List<System.Windows.Controls.Image>()
            {
                DImage_0, DImage_1, DImage_2
            };
            actionQueue = new Queue<Action>();
        }

        private void Window_ContentRendered(object sender, EventArgs e)
        {
            ShowImage();
        }

        private void ShowImage()
        {
            var vl = pairs.ElementAt(current_image_index).Value;
            current_image = pairs.ElementAt(current_image_index).Key;
            tbFileName.Text = current_image;
            var index = 0;
            foreach (var image in alignments)
            {
                if (index >= vl.Count)
                    image.Source = null;
                else
                {
                    // ... Create a new BitmapImage.
                    b1 = new BitmapImage();
                    b1.BeginInit();
                    b1.CacheOption = BitmapCacheOption.OnLoad;

                    b1.UriSource = new Uri(vl[index]);
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

        private void BtPrev_Click(object sender, RoutedEventArgs e)
        {
            PrevImage();
        }

        private void PrevImage(int step=1)
        {
            if (current_image_index == 0) return;
            
            lock (actionQueue)
            {
                while (actionQueue.Count > 0)
                {
                    actionQueue.Dequeue().Invoke();
                }
            }
            if (current_image_index - step < 0) current_image_index = 0;
            else
                current_image_index -=step;
            lock (actionQueue)
            {
                actionQueue.Enqueue(() =>
                {
                    ShowImage();
                });
            }
        }

        private void BtNext_Click(object sender, RoutedEventArgs e)
        {
            NextImage();
        }

        private void NextImage(int step=1)
        {
            if (current_image_index == pairs.Count - 1) return;
            lock (actionQueue)
            {
                while (actionQueue.Count > 0)
                {
                    actionQueue.Dequeue().Invoke();
                }
            }
            if (current_image_index + step >= pairs.Count) current_image_index = pairs.Count - 1;
            else 
                current_image_index += step;
            lock (actionQueue)
            {
                actionQueue.Enqueue(() =>
                {
                    ShowImage();
                });
            }
        }

        private void BtClose_Click(object sender, RoutedEventArgs e)
        {
            CloseDialog();
        }

        private void Window_KeyDown(object sender, KeyEventArgs e)
        {
            bool IsShiftKey = Keyboard.Modifiers == ModifierKeys.Shift ? true : false;
            if (e.Key == Key.Left)
            {
                PrevImage(IsShiftKey ? 10 : 1);
            }
            if (e.Key == Key.Right)
            {
                NextImage(IsShiftKey ? 10 : 1);
            }
        }
    }
}
