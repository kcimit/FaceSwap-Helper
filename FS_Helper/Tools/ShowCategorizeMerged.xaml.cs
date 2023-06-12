using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using Path = System.IO.Path;
using WPFCustomMessageBox;

namespace FS_Helper
{
    /// <summary>
    /// Interaction logic for SelectDuplicate.xaml
    /// </summary>
    public enum AlignmentAction { Notreviewed, Nothing, RemoveSource, ReextractAlignment, Resegmentalignment};
    public partial class ShowCategorizeMerged : Window
    {
        public bool Aborted { get; set; }
        public Dictionary<string, AlignmentAction> FileActions { get; set; }
        private List<string> _files;
        private int current_image;
        Bitmap iconRemove, iconReExtract, iconReSegment;
        private string _path;
        private string _folder;

        public ShowCategorizeMerged(string path, string folder)
        {
            InitializeComponent();
            _path = path;
            _folder = folder;

            iconRemove = FS_Helper.Properties.Resources.thrash;
            iconReExtract = FS_Helper.Properties.Resources.reExtract;
            iconReSegment = FS_Helper.Properties.Resources.reSegment;
        }

        private void Window_ContentRendered(object sender, EventArgs e)
        {
            var pathSource = System.IO.Path.Combine(_path, _folder, "merged");
            var dirInfo = new DirectoryInfo(pathSource);
            var info = dirInfo.GetFiles("*.*");
            _files = new List<string>();
            foreach (var f in info.Where(r => (new string[] { ".png", ".jpg", ".jpeg" }).Contains(r.Extension)))
            {
                _files.Add(f.FullName);
            }
            if (!_files.Any())
            {
                MessageBox.Show("Merged folder is empty.");
                Aborted = true;
                this.Close();
            }

            Aborted = false;
            current_image = 0;
            FileActions = new Dictionary<string, AlignmentAction>();
            foreach (var f in _files.OrderBy(r => r))
                FileActions.Add(f, AlignmentAction.Notreviewed);

            ShowImage();
        }

        private void ShowImage()
        {
            DImage.Source = null;
            ThrashImage.Source = null;
            try
            {
                using (Stream BitmapStream = System.IO.File.Open(_files[current_image], System.IO.FileMode.Open))
                {
                    using (System.Drawing.Image img = System.Drawing.Image.FromStream(BitmapStream))
                    {
                        using (var bmp = new Bitmap(img))
                        {
                            var src = System.Windows.Interop.Imaging.CreateBitmapSourceFromHBitmap(bmp.GetHbitmap(), IntPtr.Zero, System.Windows.Int32Rect.Empty, BitmapSizeOptions.FromEmptyOptions());
                            DImage.Source = src;
                        }
                    }
                }
                tbFileName.Text = _files[current_image];
                tbStatus.Text = $"({current_image + 1}/{_files.Count}) Files marked for action: {FileActions.Values.Count(r => r != AlignmentAction.Nothing && r!= AlignmentAction.Notreviewed)}";
                if (FileActions.ElementAt(current_image).Value == AlignmentAction.Notreviewed)
                    FileActions[FileActions.ElementAt(current_image).Key] = AlignmentAction.Nothing;

                if (FileActions.ElementAt(current_image).Value == AlignmentAction.Nothing || FileActions.ElementAt(current_image).Value == AlignmentAction.Notreviewed)
                    ThrashImage.Source = null;
                else
                {
                    using (MemoryStream memory = new MemoryStream())
                    {
                        if (FileActions.ElementAt(current_image).Value == AlignmentAction.RemoveSource)
                            iconRemove.Save(memory, ImageFormat.Png);
                        if (FileActions.ElementAt(current_image).Value == AlignmentAction.ReextractAlignment)
                            iconReExtract.Save(memory, ImageFormat.Png);
                        if (FileActions.ElementAt(current_image).Value == AlignmentAction.Resegmentalignment)
                            iconReSegment.Save(memory, ImageFormat.Png);

                        memory.Position = 0;
                        var bitmapImage = new BitmapImage();
                        bitmapImage.BeginInit();
                        bitmapImage.StreamSource = memory;
                        bitmapImage.CacheOption = BitmapCacheOption.OnLoad;
                        bitmapImage.EndInit();
                        ThrashImage.Source = bitmapImage;
                    }
                }
            }
            catch { }
        }

        private void CloseDialog()
        {
            //b1.UriSource = null;
            DImage.Source = null;
            this.Close();
        }

        private void BtReextractFile_OnClick(object sender, RoutedEventArgs e)
        {
            ActionReextract();
        }

        private void ActionReextract()
        {
            FileActions[FileActions.ElementAt(current_image).Key] = AlignmentAction.ReextractAlignment;
            
            NextImage();
        }

        private void BtResegmentFile_OnClick(object sender, RoutedEventArgs e)
        {
            ActionResegment();
        }

        private void ActionResegment()
        {
            FileActions[FileActions.ElementAt(current_image).Key] = AlignmentAction.Resegmentalignment;
            NextImage();
        }

        private void BtRemoveFile_OnClick(object sender, RoutedEventArgs e)
        {
            ActionRemove();
        }

        private void ActionRemove()
        {
            FileActions[FileActions.ElementAt(current_image).Key] = AlignmentAction.RemoveSource;
            NextImage();
        }

        private void BtDoNothing_Click(object sender, RoutedEventArgs e)
        {
            ActionNothing();
        }

        private void ActionNothing()
        {
            FileActions[FileActions.ElementAt(current_image).Key] = AlignmentAction.Nothing;
            NextImage();
        }

        private void BtAbort_OnClick(object sender, RoutedEventArgs e)
        {
            Aborted = true;
            FileActions = new Dictionary<string, AlignmentAction>();
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
            CloseDialog();
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (Aborted)
                return;

            if (FileActions.Values.Any(r => r == AlignmentAction.RemoveSource))
                Directory.CreateDirectory(Path.Combine(_path, _folder, "merged", "DeletedSource"));
            if (FileActions.Values.Any(r => r == AlignmentAction.Resegmentalignment))
                Directory.CreateDirectory(Path.Combine(_path, _folder, "merged", "ToResegment"));
            if (FileActions.Values.Any(r => r == AlignmentAction.Nothing))
                Directory.CreateDirectory(Path.Combine(_path, _folder, "merged", "Reviewed"));

            var dcount = 0;
            var mcount = 0;

            foreach (var f in FileActions)
                try
                {
                    if (f.Value == AlignmentAction.ReextractAlignment)
                    {
                        Util.SafeDelete(f.Key);
                        Util.SafeDelete(Path.Combine(_path, _folder, "aligned", Path.GetFileName(f.Key)));
                        Util.SafeDelete(Path.Combine(_path, _folder, "debug", Path.GetFileName(f.Key)));
                        dcount++;
                    }
                    else if (f.Value == AlignmentAction.Resegmentalignment)
                    {
                        Util.SafeDelete(f.Key);
                        File.Copy(Path.Combine(_path, _folder, "aligned", Path.GetFileName(f.Key)), Path.Combine(_path, _folder, "merged", "ToResegment", Path.GetFileName(f.Key)));
                        mcount++;
                    }
                    else if (f.Value == AlignmentAction.RemoveSource)
                    {
                        Util.SafeDelete(f.Key);
                        File.Move(Path.Combine(_path, _folder, Path.GetFileName(f.Key)), Path.Combine(_path, _folder, "merged", "DeletedSource", Path.GetFileName(f.Key)));
                        dcount++;
                    }
                    else if (f.Value != AlignmentAction.Notreviewed)
                    {
                        File.Move(f.Key, Path.Combine(_path, _folder, "merged", "Reviewed", Path.GetFileName(f.Key)));
                        mcount++;
                    }
                }
                catch { }

            MessageBox.Show($"{dcount} files deleted and {mcount} files moved.", "Results", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void Window_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Handled) return;
            
            if (e.Key == Key.Left)
                PrevImage();
            if (e.Key == Key.Right)
                NextImage();
            if (e.Key == Key.A)
                ActionReextract();
            if (e.Key == Key.S)
                ActionResegment();
            if (e.Key == Key.D)
                ActionRemove();
            if (e.Key == Key.Space)
                ActionNothing();
            e.Handled = true;
        }

        private void Window_MouseWheel(object sender, MouseWheelEventArgs e)
        {
            if (e.Delta > 0)
                PrevImage();

            else if (e.Delta < 0)
                NextImage();
        }
    }
}
