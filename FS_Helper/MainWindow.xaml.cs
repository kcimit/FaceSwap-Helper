using System;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using Path = System.IO.Path;

namespace FS_Helper
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow
    {
        private string _target;
        public ConnectionViewModel Cvm;

        public MainWindow()
        {
            InitializeComponent();
            _target = "data_dst";
            Cvm = new ConnectionViewModel();
            DataContext = Cvm;
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            // Reading last open file
            try
            {
                using (var fs = new FileStream($"{AppDomain.CurrentDomain.BaseDirectory}\\last.txt", FileMode.Open))
                {
                    using (var reader = new StreamReader(fs))
                    {
                        TbDir.Text = reader.ReadLine() ?? throw new InvalidOperationException();
                    }
                }
            }
            catch
            {
                // ignored
            }
        }

        private void BtOpenDir_OnClick(object sender, RoutedEventArgs e)
        {
            using (var dialog = new System.Windows.Forms.FolderBrowserDialog())
            {
                if (!string.IsNullOrEmpty(TbDir.Text)) dialog.SelectedPath = TbDir.Text;
                var result = dialog.ShowDialog();
                if (result == System.Windows.Forms.DialogResult.OK)
                    TbDir.Text = dialog.SelectedPath;
            }
        }

        private void BtClose_OnClick(object sender, RoutedEventArgs e)
        {
            Close();
        }
        
        private void MainWindow_OnClosing(object sender, CancelEventArgs e)
        {
            // Creating last open file
            if (string.IsNullOrEmpty(TbDir.Text)) return;
            try
            {
                using (var fs = new FileStream($"{AppDomain.CurrentDomain.BaseDirectory}\\last.txt", FileMode.Create))
                {
                    using (var writer = new StreamWriter(fs))
                    {
                        writer.WriteLine(TbDir.Text);
                    }
                }
            }
            catch (Exception exc) { MessageBox.Show($"Error creating 'last opened' history file {exc.Message}"); }
        }

        private void BtArrangeAfterExtract_OnClick(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(TbDir.Text))
            {
                MessageBox.Show("Select workspace directory");
                return;
            }

            var path = Path.Combine(TbDir.Text, _target, "aligned");
            var dirInfo = new DirectoryInfo(path);
            var info = dirInfo.GetFiles("*.*");
            foreach (var f in info)
            {
                var name = Path.GetFileNameWithoutExtension(f.FullName);
                var suffix = name.Substring(name.Length - 2, 2);
                if (!suffix[0].Equals('_')) continue;
                var gen = suffix[1];
                var newPath= Path.Combine(path, gen.ToString());
                if (!Directory.Exists(newPath))
                    Directory.CreateDirectory(newPath);
                var newName = Path.Combine(newPath, name.Substring(0, name.Length - 2)+f.Extension);
                File.Move(f.FullName, newName);
            }

            MessageBox.Show("Done!");
        }

        private void BtMergeAfterExtract_OnClick(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(TbDir.Text))
            {
                MessageBox.Show("Select workspace directory");
                return;
            }

            var path = Path.Combine(TbDir.Text, _target, "aligned");
            var dirs = Directory.GetDirectories(path);

            foreach (var subDir in dirs)
            {
                var dirInfo = new DirectoryInfo(subDir);
                var info = dirInfo.GetFiles("*.*");
                foreach (var f in info)
                {
                    var name = f.FullName;
                    var coreName = Path.Combine(path, f.Name);
                    if (File.Exists(coreName))
                    {
                        var selectImage = new SelectDuplicate(coreName, name) {Owner = this};
                        selectImage.ShowDialog();
                        if (selectImage.Aborted)
                        {
                            MessageBox.Show("Aborted!");
                            return;
                        }
                        var choice = selectImage.SelectedName;
                        selectImage = null;
                        if (string.IsNullOrEmpty(choice)) continue;
                        if (!choice.Equals(name)) continue;
                        File.Delete(coreName);
                        File.Move(name,coreName);
                    }
                    else
                        File.Move(name, coreName);
                }
                var info2 = dirInfo.GetFiles("*.*");
                if (info2.Length==0)
                    Directory.Delete(subDir);
            }

            MessageBox.Show("Done!");
        }

        private void BtReviewDeletedAlignments_OnClick(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(TbDir.Text))
            {
                MessageBox.Show("Select workspace directory");
                return;
            }

            var pathSource = Path.Combine(TbDir.Text, _target);
            var dirInfo = new DirectoryInfo(pathSource);
            var info = dirInfo.GetFiles("*.*");
            //var is_png = info.Count(r => r.Extension.Equals("png")) >
            //             info.Count(r => r.Extension.Equals("jpg"));
            
            foreach (var f in info)
            {
               var name = f.FullName; 
               var alignedName = Path.Combine(pathSource, "aligned", Path.GetFileNameWithoutExtension(f.Name) + ".jpg");
               if (File.Exists(alignedName)) continue;
               var im = new ShowDeletedAlignmentSource(name) {Owner = this};
               im.ShowDialog();
               if (im.Aborted)
               {
                   MessageBox.Show("Aborted!");
                   return;
               }
                var keepFile = im.KeepFile;
               im = null;
               if (keepFile==false)
                   File.Delete(name);
            }

            MessageBox.Show("Done!");
        }

        private void BtArrangeDebugLandmarks_OnClick(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(TbDir.Text))
            {
                MessageBox.Show("Select workspace directory");
                return;
            }

            var path = Path.Combine(TbDir.Text, _target, "aligned");
            var landmarks = Path.Combine(TbDir.Text, _target, "landmarks");
            if (!Directory.Exists(landmarks))
                Directory.CreateDirectory(landmarks);

            var dirInfo = new DirectoryInfo(path);
            var info = dirInfo.GetFiles("*_debug.*");
            foreach (var f in info)
            {
                var name = Path.GetFileNameWithoutExtension(f.FullName);
                name = name.Substring(0, name.Length - 6) + f.Extension;
                var newName = Path.Combine(landmarks, name);
                File.Move(f.FullName, newName);
            }

            MessageBox.Show("Done!");
        }

        private void BtRemoveDisregardedAlignments_OnClick(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(TbDir.Text))
            {
                MessageBox.Show("Select workspace directory");
                return;
            }

            var alignments = Path.Combine(TbDir.Text, _target, "aligned");
            var landmarks = Path.Combine(TbDir.Text, _target, "landmarks");
            if (!Directory.Exists(landmarks))
            {
                MessageBox.Show("Landmarks folder does not exist. Aborting!");
                return;
            }
            var dirLandInfo = new DirectoryInfo(landmarks);
            var landInfo = dirLandInfo.GetFiles("*.*");
            if (landInfo.Length == 0)
            {
                MessageBox.Show("Landmarks folder is empty. Aborting!");
                return;
            }

            var landmarksFiles = landInfo.Select(f => f.Name).ToList();

            var dirInfo = new DirectoryInfo(alignments);
            var info = dirInfo.GetFiles("*.*");
            var count = 0;
            foreach (var f in info)
            {
                if (landmarksFiles.Contains(f.Name)) continue;
                count++;
                File.Delete(f.FullName);
            }

            MessageBox.Show($"Removed {count} files. Done!");
        }

        private void BtRemoveDebugImagesWithNoAlignments_OnClick(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(TbDir.Text))
            {
                MessageBox.Show("Select workspace directory");
                return;
            }

            var alignmentsDebug = Path.Combine(TbDir.Text, _target, "aligned_debug");
            var alignments = Path.Combine(TbDir.Text, _target, "aligned");
            
            var dirAlInfo = new DirectoryInfo(alignments);
            var alInfo = dirAlInfo.GetFiles("*.*");

            var alFiles = alInfo.Select(f => f.Name).ToList();

            var dirInfo = new DirectoryInfo(alignmentsDebug);
            var info = dirInfo.GetFiles("*.*");
            var count = 0;
            foreach (var f in info)
            {
                if (alFiles.Contains(f.Name)) continue;
                count++;
                File.Delete(f.FullName);
            }

            MessageBox.Show($"Removed {count} files. Done!");
        }

        private void BtRenameReExtractedAlignments_OnClick(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(TbDir.Text))
            {
                MessageBox.Show("Select workspace directory");
                return;
            }

            var alignments = Path.Combine(TbDir.Text, _target, "aligned");

            var dirAlInfo = new DirectoryInfo(alignments);
            var alInfo = dirAlInfo.GetFiles("*_0.*");
            var count = 0;
            foreach (var f in alInfo)
            {
                var name = Path.GetFileNameWithoutExtension(f.FullName);
                name = name.Substring(0, name.Length - 2) + f.Extension;
                var newName = Path.Combine(alignments, name);

                if (File.Exists(newName))
                    File.Delete(newName);

                File.Move(f.FullName, newName);
                count++;
            }

            MessageBox.Show($"Renamed {count} files. Done!");
        }

        private void CbTarget_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (CbTarget.SelectedIndex == 0)
                _target = "data_src";
            else if (CbTarget.SelectedIndex == 1) _target = "data_dst";
        }

        private void BtConvertPngToJpg_OnClick(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(TbDir.Text))
            {
                MessageBox.Show("Select workspace directory");
                return;
            }

            var merged = Path.Combine(TbDir.Text, _target, "merged");
            ConvertToJpg.ConvertAll(merged, Cvm);
            MessageBox.Show("Done!");
        }
    }
}
