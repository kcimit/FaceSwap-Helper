using Services;
using ShabsImpl;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
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
    public enum AlignmentViewAction { Keep, Trash, None};
    public class AlignmentViewModel : ViewModelBase
    {
        private BitmapSource _image, _imageOverlay;
        private AlignmentViewAction _action;
        Bitmap thrash, checkmark;
        public string File { get; set; }
        public string Target { get;  set; }

        public AlignmentViewAction Action
        {
            get => _action;
            set
            {
                if (_action == value) return;
                _action = value;
                if (_action == AlignmentViewAction.None)
                {
                    ImageOverlay = null;
                }
                else 
                {
                    ImageOverlay = null;
                    using (MemoryStream memory = new MemoryStream())
                    {
                        if (_action == AlignmentViewAction.Trash) 
                            thrash.Save(memory, ImageFormat.Png);
                        if (_action == AlignmentViewAction.Keep)
                            checkmark.Save(memory, ImageFormat.Png);
                        memory.Position = 0;
                        var bitmapImage = new BitmapImage();
                        bitmapImage.BeginInit();
                        bitmapImage.StreamSource = memory;
                        bitmapImage.CacheOption = BitmapCacheOption.OnLoad;
                        bitmapImage.EndInit();
                        ImageOverlay = bitmapImage;
                    }
                }
                OnPropertyChanged(nameof(Action));
            }
        }
        public BitmapSource Image
        {
            get => _image;
            set
            {
                if (_image == value) return;
                _image = value;
                OnPropertyChanged(nameof(Image));
            }
        }

        public BitmapSource ImageOverlay
        {
            get => _imageOverlay;
            set
            {
                if (_imageOverlay == value) return;
                _imageOverlay = value;
                OnPropertyChanged(nameof(ImageOverlay));
            }
        }
        public ICommand LeftClickCommand { get; set; }
        public ICommand RightClickCommand { get; set; }
        public ICommand RemoveCommand { get; set; }
        private AlignmentsView alignmentsView { get; }

        public AlignmentViewModel(AlignmentsView alignmentsView, string target, string path, string file, SynchronizationContext context)
        {
            this.alignmentsView = alignmentsView;
            uiSynchronizationContext = context;
            thrash = FS_Helper.Properties.Resources.thrash;
            checkmark = FS_Helper.Properties.Resources.checkmark;
            this.File = file;
            this.Target = target;
            var b1 = new BitmapImage();
            b1.BeginInit();
            b1.CacheOption = BitmapCacheOption.OnLoad;

            b1.UriSource = new Uri(System.IO.Path.Combine(path,file));
            b1.EndInit();
            Image = b1;

            this.LeftClickCommand = new RelayCommand(this.SelectImage);
            this.RemoveCommand = new RelayCommand(this.Remove);
            this.RightClickCommand = new RelayCommand(this.DiscardImage);
            Action = AlignmentViewAction.None;
        }

        private void Remove(object obj)
        {
            alignmentsView.SetRemoved(Target, File);
        }

        private void DiscardImage(object obj)
        {
            if (Action == AlignmentViewAction.Trash)
                Action = AlignmentViewAction.None;
            else
                Action = AlignmentViewAction.Trash;
            alignmentsView.SetThrashed(Target, File);
        }

        private void SelectImage(object obj)
        {
            Action = AlignmentViewAction.Keep;
            alignmentsView.SetSelected(File);
        }
    }

    public class AlignmentsView : ViewModelBase
    {

        public ICommand NextCommand { get; set; }
        public ICommand TrashAllCommand { get; set; }
        public ICommand ClearAllCommand { get; set; }
        public ICommand ConfirmCommand { get; set; }
        private Dictionary<string, string> alignments_reviewed;
        public Dictionary<string, string> AlignmentsReviewed { get; set; }
        public Dictionary<string, List<string>> Trashed { get; set; }
        private Dictionary<string, List<string>> pairs;
        private string path;
        private int nextIndex;
        private string lastSelectedImage;
        private string lastThrashedImage;
        ReviewAlignmentsBatch main;
        public ObservableCollection<AlignmentViewModel> Items { get; set; }

        private int prevIndex;

        public AlignmentsView(Dictionary<string, List<string>> pairs, string path, int startImage, string image, ReviewAlignmentsBatch main)
        {
            uiSynchronizationContext = SynchronizationContext.Current;
            this.pairs = pairs;
            this.path = path;
            this.main = main;
            lastSelectedImage = image;
            alignments_reviewed = new Dictionary<string, string>();
            AlignmentsReviewed = new Dictionary<string, string>();
            Trashed = new Dictionary<string, List<string>>();
            this.NextCommand = new RelayCommand(this.Next);
            this.TrashAllCommand = new RelayCommand(this.TrashAll);
            this.ClearAllCommand = new RelayCommand(this.ClearAll);
            this.ConfirmCommand = new RelayCommand(this.Confirm);
            prevIndex = startImage;
            DisplayCollection(startImage);
        }

        private void ClearAll(object obj)
        {
            foreach (var i in Items)
            {
                i.Action = AlignmentViewAction.None;
            }
        }

        private void TrashAll(object obj)
        {
            foreach (var i in Items)
            {
                i.Action = AlignmentViewAction.Trash;
            }
        }

        private void Confirm(object obj)
        {
            Save();
            AlignmentsReviewed = alignments_reviewed;
            main.CloseDialog();
        }

        private void Next(object obj)
        {
            if (Items == null)
                return;
            Save();
            if (nextIndex >= pairs.Count - 1)
                return;
            lastThrashedImage = string.Empty;
            DisplayCollection(nextIndex);
        }

        private void Save()
        {
            foreach (var i in Items.Where(r => r.Action != AlignmentViewAction.Trash))
            {
                if (!alignments_reviewed.ContainsKey(i.Target))
                    alignments_reviewed.Add(i.Target, i.File);
            }
            foreach (var i in Items.Where(r => r.Action == AlignmentViewAction.Trash))
            {
                if (!Trashed.ContainsKey(i.Target))
                    Trashed.Add(i.Target, new List<string> { i.File });
                else
                    Trashed[i.Target].Add(i.File);
            }
            if (alignments_reviewed.Any())
                lastSelectedImage = alignments_reviewed.Last().Value;
        }

        private void DisplayCollection(int startImage)
        {
            Items = new ObservableCollection<AlignmentViewModel>();
            prevIndex = startImage;
            nextIndex = Math.Min(pairs.Count - 1, startImage + 128);
            var cmp = new Dictionary<string, double>();
            var cmp2 = new Dictionary<string, double>();
            Parallel.For(prevIndex, nextIndex, (i) =>
            {
                var kvp = pairs.ElementAt(i);
                var vl = kvp.Value;
                if (vl.Count == 0)
                    return;
                foreach (var v in vl)
                {
                    var difference = 100.0 - ImageHashing.Similarity(System.IO.Path.Combine(path, lastSelectedImage), System.IO.Path.Combine(path, v));
                    //(int)(100.0 * ImageTool.GetPercentageDifference(System.IO.Path.Combine(path, lastSelectedImage), System.IO.Path.Combine(path, v)));
                    lock (cmp)
                    {
                        cmp.Add(v, difference);
                    }
                    var difference2 = 100.0;
                    if (!string.IsNullOrEmpty(lastThrashedImage))
                        difference2 = 100.0 - ImageHashing.Similarity(System.IO.Path.Combine(path, lastThrashedImage), System.IO.Path.Combine(path, v));
                    //(int)(100.0 * ImageTool.GetPercentageDifference(System.IO.Path.Combine(path, lastThrashedImage), System.IO.Path.Combine(path, v)));
                    lock (cmp2)
                    {
                        cmp2.Add(v, difference2);
                    }
                }
            });

            for (int i = prevIndex; i < nextIndex; i++)
            {
                var cmp3 = new Dictionary<string, double>();
                var kvp = pairs.ElementAt(i);

                var vl = kvp.Value;
                if (vl.Count == 0)
                    continue;

                var toRemove = new List<string>();
                foreach (var v in vl)
                {
                    var difference = cmp[v];
                    var difference2 = cmp2[v];
                    if (difference2 < 20 )
                        toRemove.Add(v);
                    else
                        cmp3.Add(v, difference);
                }
                foreach (var r in toRemove)
                    vl.Remove(r);

                if (cmp3.Count < 2)
                    continue;

                var best = cmp3.OrderBy(r => r.Value).First();

                Items.Add(new AlignmentViewModel(this, kvp.Key, path, best.Key, uiSynchronizationContext));
            }
            OnPropertyChanged(nameof(Items));
        }

        internal void SetThrashed(string target, string file)
        {
            lastThrashedImage = file;
           // DisplayCollection(prevIndex);
        }

        internal void SetSelected(string file)
        {
            lastSelectedImage = file;
            DisplayCollection(prevIndex);
        }

        internal void SetRemoved(string target, string file)
        {
            lastThrashedImage = file;
            if (!Trashed.ContainsKey(target))
                Trashed.Add(target, new List<string> { file });
            else
                Trashed[target].Add(file);
            DisplayCollection(prevIndex);
        }
    }

    /// <summary>
    /// Interaction logic for SelectDuplicate.xaml
    /// </summary>
    public partial class ReviewAlignmentsBatch : Window
    {
        public bool Aborted { get; set; }
        public Dictionary<string, string> AlignmentsReviewed { get; set; }
        public Dictionary<string, List<string>> Trashed { get; set; }
        AlignmentsView vm;

        public ReviewAlignmentsBatch(Dictionary<string, List<string>> pairs, string path, int startImage, string image)
        {
            InitializeComponent();

            Trashed = new Dictionary<string, List<string>>();

            Aborted = false;
            AlignmentsReviewed = new Dictionary<string, string>();
            vm = new AlignmentsView(pairs, path, startImage, image, this);
            this.DataContext = vm;
        }

        private void Window_ContentRendered(object sender, EventArgs e)
        {
            
        }
        public void CloseDialog()
        {
            AlignmentsReviewed = vm.AlignmentsReviewed;
            Trashed = vm.Trashed;
            this.Close();
        }
        private void BtAbort_OnClick(object sender, RoutedEventArgs e)
        {
            Aborted = true;
            AlignmentsReviewed = new Dictionary<string, string>();
            Close();
        }
    }
}
