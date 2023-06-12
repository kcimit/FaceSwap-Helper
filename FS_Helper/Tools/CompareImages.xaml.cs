using Services;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace FS_Helper
{
    
    public class CompareViewModel: ViewModelBase
    {
        private string _fileName, _path0, _path1;
        BitmapSource _image0, _image1;
        private int current_image_index;
        private Queue<Action> actionQueue;
        private Dictionary<string, List<string>> pairs;
        private TranslateTransform image0tt, image1tt;
        private ScaleTransform image0st, image1st;

        public ScaleTransform Image0ScaleTransform
        {
            get => image0st;
            set
            {
                if (image0st == value) return;
                image0st = value;
                if (image1st != value)
                    Image1ScaleTransform = value;
                OnPropertyChanged(nameof(Image0ScaleTransform));
            }
        }

        public ScaleTransform Image1ScaleTransform
        {
            get => image1st;
            set
            {
                if (image1st == value) return;
                image1st = value;
                if (image0st != value)
                    Image0ScaleTransform = value;
                OnPropertyChanged(nameof(Image1ScaleTransform));
            }
        }

        public TranslateTransform Image0TranslateTransform
        {
            get => image0tt;
            set
            {
                if (image0tt == value) return;
                image0tt = value;
                if (image1tt != value)
                    Image1TranslateTransform = value;
                OnPropertyChanged(nameof(Image0TranslateTransform));
            }
        }

        public TranslateTransform Image1TranslateTransform
        {
            get => image1tt;
            set
            {
                if (image1tt == value) return;
                image1tt = value;
                if (image0tt!=value) 
                    Image0TranslateTransform = value;
                OnPropertyChanged(nameof(Image1TranslateTransform));
            }
        }

        public BitmapSource Image0
        {
            get => _image0;
            set
            {
                if (_image0 == value) return;
                _image0 = value;
                OnPropertyChanged(nameof(Image0));
            }
        }
        public BitmapSource Image1
        {
            get => _image1;
            set
            {
                if (_image1 == value) return;
                _image1 = value;
                OnPropertyChanged(nameof(Image1));
            }
        }
        public string FileName
        {
            get => _fileName;
            set
            {
                if (_fileName == value) return;
                _fileName = value;
                OnPropertyChanged(nameof(FileName));
            }
        }

        public string Path0
        {
            get => _path0;
            set
            {
                if (_path0 == value) return;
                _path0 = value;
                OnPropertyChanged(nameof(Path0));
            }
        }

        public string Path1
        {
            get => _path1;
            set
            {
                if (_path1 == value) return;
                _path1 = value;
                OnPropertyChanged(nameof(Path1));
            }
        }

        public CompareViewModel(string path0, string path1)
        {
            uiSynchronizationContext = SynchronizationContext.Current;
            _path0 = path0;
            _path1 = path1;

            var p0 = new DirectoryInfo(path0);
            var p1 = new DirectoryInfo(path1);
            var info0 = p0.GetFiles("*.jpg", SearchOption.TopDirectoryOnly);
            if (!info0.Any())
            {
                info0 = p0.GetFiles("*.png", SearchOption.TopDirectoryOnly);
                if (!info0.Any())
                {
                    MessageBox.Show("No jpg or png files are found in the first folder. Aborting.");
                    return;
                }
            }
            var info1 = p1.GetFiles("*.jpg", SearchOption.TopDirectoryOnly);
            if (!info1.Any())
            {
                info1 = p1.GetFiles("*.png", SearchOption.TopDirectoryOnly);
                if (!info1.Any())
                {
                    MessageBox.Show("No jpg or png files are found in the second folder. Aborting.");
                    return;
                }
            }

            pairs = new Dictionary<string, List<string>>();
            var path1FileNames = info1.Select(r => Path.GetFileName(r.FullName)).ToList();
            foreach (var f in info0)
            {
                var fn = Path.GetFileName(f.FullName);
                if (path1FileNames.Contains(fn)) pairs.Add(fn, new List<string>() { f.FullName });
            }
            foreach (var f in info1)
            {
                var fn = Path.GetFileName(f.FullName);
                if (pairs.ContainsKey(fn))
                    pairs[fn].Add(f.FullName);
            }
            current_image_index = 0;
            actionQueue = new Queue<Action>();
            this.NextImageCommand = new RelayCommand(this.NextImage);
            this.PrevImageCommand = new RelayCommand(this.PrevImage);
            AssignImages();
        }

        public ICommand NextImageCommand { get; set; }
        private void NextImage(object parameter)
        {
            int step = 1;
            if (parameter != null) step = (int)parameter;
            if (current_image_index == pairs.Count - 1) return;
            if (current_image_index + step >= pairs.Count) current_image_index = pairs.Count - 1;
            else
                current_image_index += step;
            lock (actionQueue)
            {
                actionQueue.Enqueue(() =>
                {
                    AssignImages();
                });
            }
            lock (actionQueue)
            {
                while (actionQueue.Count > 0)
                {
                    actionQueue.Dequeue().Invoke();
                }
            }
        }

        public ICommand PrevImageCommand { get; set; }
        private void PrevImage(object parameter)
        {
            int step = 1;
            if (parameter != null) step = (int)parameter;

            if (current_image_index == 0) return;

            if (current_image_index - step < 0) current_image_index = 0;
            else
                current_image_index -= step;
            lock (actionQueue)
            {
                actionQueue.Enqueue(() =>
                {
                    AssignImages();
                });
            }
            lock (actionQueue)
            {
                while (actionQueue.Count > 0)
                {
                    actionQueue.Dequeue().Invoke();
                }
            }
        }

        private void AssignImages()
        {
            var vl = pairs.ElementAt(current_image_index).Value;
            FileName = pairs.ElementAt(current_image_index).Key;
            // ... Create a new BitmapImage.
            var b0 = new BitmapImage();
            b0.BeginInit();
            b0.CacheOption = BitmapCacheOption.OnLoad;

            b0.UriSource = new Uri(vl[0]);
            b0.EndInit();
            Image0 = b0;
            // ... Create a new BitmapImage.
            var b1 = new BitmapImage();
            b1.BeginInit();
            b1.CacheOption = BitmapCacheOption.OnLoad;

            b1.UriSource = new Uri(vl[1]);
            b1.EndInit();
            Image1 = b1;
        }

        internal void HandleKeyDown(KeyEventArgs e)
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

    /// <summary>
    /// Interaction logic for SelectDuplicate.xaml
    /// </summary>
    public partial class CompareImages : Window
    {
        CompareViewModel cvm;
        public CompareImages(string path0, string path1)
        {
            InitializeComponent();
            cvm = new CompareViewModel(path0, path1);
            DataContext = cvm;
        }

        private void Window_ContentRendered(object sender, EventArgs e)
        {
            
        }
        private void BtClose_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void Window_KeyDown(object sender, KeyEventArgs e)
        {
            if (cvm == null) return;
            cvm.HandleKeyDown(e);
        }
    }
}
