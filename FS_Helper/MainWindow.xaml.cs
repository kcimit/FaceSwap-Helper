using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
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
        ConnectionViewModel Cvm;
        CancellationTokenSource cs;
        CancellationToken cancellationToken;
        public MainWindow()
        {
            InitializeComponent();
            _target = "data_dst";
            Cvm = new ConnectionViewModel();
            DataContext = Cvm;
            cs= new CancellationTokenSource();
            cancellationToken = cs.Token;
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            if (Directory.Exists($@"{System.AppDomain.CurrentDomain.BaseDirectory}workspace"))
                TbDir.Text = $@"{System.AppDomain.CurrentDomain.BaseDirectory}workspace";
            else
            {
                // Reading last open file
                try
                {
                    using (var fs = new FileStream($"{AppDomain.CurrentDomain.BaseDirectory}\\last.txt", FileMode.Open))
                    {
                        using (var reader = new StreamReader(fs))
                        {
                            TbDir.Text = reader.ReadLine() ?? "";
                            Tools.CompareFolder1=reader.ReadLine() ?? "";
                            Tools.CompareFolder2 = reader.ReadLine() ?? "";
                        }
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine(ex.Message);
                    // ignored
                }
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
            cs.Cancel();
            // Creating last open file
            if (string.IsNullOrEmpty(TbDir.Text)) return;
            try
            {
                using (var fs = new FileStream($"{AppDomain.CurrentDomain.BaseDirectory}\\last.txt", FileMode.Create))
                {
                    using (var writer = new StreamWriter(fs))
                    {
                        writer.WriteLine(TbDir.Text);
                        writer.WriteLine(Tools.CompareFolder1);
                        writer.WriteLine(Tools.CompareFolder2);
                    }
                }
            }
            catch (Exception exc) { MessageBox.Show($"Error creating 'last opened' history file {exc.Message}", "Problem", MessageBoxButton.OK, MessageBoxImage.Exclamation); }
        }

        private void BtArrangeAfterExtract_OnClick(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(TbDir.Text))
            {
                MessageBox.Show("Select workspace directory first.", "Problem", MessageBoxButton.OK, MessageBoxImage.Exclamation);
                return;
            }

            var path = Path.Combine(TbDir.Text, _target, "aligned");
            var dirInfo = new DirectoryInfo(path);
            var info = dirInfo.GetFiles("*.*");
            foreach (var f in info)
            {
                var name = Path.GetFileNameWithoutExtension(f.FullName);
                var suffix = name.Substring(name.Length - 2, 2);
                if (!suffix[0].Equals('_'))
                    continue;

                var gen = suffix[1];
                var newPath = Path.Combine(path, gen.ToString());
                if (!Directory.Exists(newPath))
                    Directory.CreateDirectory(newPath);

                //var newName = Path.Combine(newPath, name.Substring(0, name.Length - 2) + f.Extension);
                var newName = Path.Combine(newPath, name + f.Extension);
                File.Move(f.FullName, newName);
            }
            Cvm.Status = "Arranging is complete.";
        }

        private void BtMergeAfterExtract_OnClick(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(TbDir.Text))
            {
                MessageBox.Show("Select workspace directory first.", "Problem", MessageBoxButton.OK, MessageBoxImage.Exclamation);
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
                        var selectImage = new SelectDuplicate(coreName, name) { Owner = this };
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
                        File.Move(name, coreName);
                    }
                    else
                        File.Move(name, coreName);
                }
                var info2 = dirInfo.GetFiles("*.*");
                if (info2.Length == 0)
                    Directory.Delete(subDir);
            }
            Cvm.Status = "Merging is complete.";
        }

        private void BtReviewDeletedAlignments_OnClick(object sender, RoutedEventArgs e)
        {
            var im = new ShowDeletedAlignmentSource(TbDir.Text, _target) { Owner = this };
            im.ShowDialog();
        }

        private void BtArrangeDebugLandmarks_OnClick(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(TbDir.Text))
            {
                MessageBox.Show("Select workspace directory first.", "Problem", MessageBoxButton.OK, MessageBoxImage.Exclamation);
                return;
            }

            var path = Path.Combine(TbDir.Text, _target, "aligned");
            var landmarks = Path.Combine(TbDir.Text, _target, "landmarks");
            if (!Directory.Exists(landmarks))
                Directory.CreateDirectory(landmarks);

            var dirInfo = new DirectoryInfo(landmarks);
            var info = dirInfo.GetFiles("*.*");
            if (info.Any())
            {
                var res = MessageBox.Show("There are files in landmarks folder. Do you want to remove them? Select 'No' to keep them.", "Question", MessageBoxButton.YesNoCancel, MessageBoxImage.Question);
                if (res == MessageBoxResult.Cancel)
                    return;

                if (res == MessageBoxResult.Yes) 
                    foreach (var f in info)
                        File.Delete(f.FullName);
            }

            dirInfo = new DirectoryInfo(path);
            info = dirInfo.GetFiles("*_debug.*");
            foreach (var f in info)
            {
                var name = Path.GetFileNameWithoutExtension(f.FullName);
                name = name.Substring(0, name.Length - 6) + f.Extension;
                var newName = Path.Combine(landmarks, name);
                File.Move(f.FullName, newName);
            }
            
            MessageBox.Show($"Done!", "Results", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void BtRemoveDisregardedAlignments_OnClick(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(TbDir.Text))
            {
                MessageBox.Show("Select workspace directory first.", "Problem", MessageBoxButton.OK, MessageBoxImage.Exclamation);
                return;
            }

            var alignments = Path.Combine(TbDir.Text, _target, "aligned");
            var landmarks = Path.Combine(TbDir.Text, _target, "landmarks");
            if (!Directory.Exists(landmarks))
            {
                MessageBox.Show("Landmarks folder does not exist. Aborting!", "Problem", MessageBoxButton.OK, MessageBoxImage.Exclamation);
                return;
            }
            var dirLandInfo = new DirectoryInfo(landmarks);
            var landInfo = dirLandInfo.GetFiles("*.*");
            if (landInfo.Length == 0)
            {
                MessageBox.Show("Landmarks folder is empty. Aborting!", "Problem", MessageBoxButton.OK, MessageBoxImage.Exclamation);
                return;
            }

            var landmarksFiles = landInfo.Select(f => f.Name).ToList();

            var dirInfo = new DirectoryInfo(alignments);
            var info = dirInfo.GetFiles("*.*");
            var files_to_delete = new List<string>();

            foreach (var f in info)
            {
                if (landmarksFiles.Contains(f.Name)) continue;
                files_to_delete.Add(f.FullName);
            }
            DeleteFiles(files_to_delete);
        }

        private void BtRemoveDebugImagesWithNoAlignments_OnClick(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(TbDir.Text))
            {
                MessageBox.Show("Select workspace directory first.", "Problem", MessageBoxButton.OK, MessageBoxImage.Exclamation);
                return;
            }

            var alignmentsDebug = Path.Combine(TbDir.Text, _target, "aligned_debug");
            var alignments = Path.Combine(TbDir.Text, _target, "aligned");

            var dirAlInfo = new DirectoryInfo(alignments);

            var check = dirAlInfo.GetFiles("*_?.*");
            if (check.Any())
            {
                var res = MessageBox.Show("Warning! Alignments with _ are found. It could be not yet renamed alignments after extract. Do you really want to remove them?", "Question", MessageBoxButton.YesNoCancel, MessageBoxImage.Question);
                if (res == MessageBoxResult.No || res == MessageBoxResult.Cancel) return;
            }
            var alInfo = dirAlInfo.GetFiles("*.*");

            var alFiles = alInfo.Select(f => f.Name).ToList();

            var dirInfo = new DirectoryInfo(alignmentsDebug);
            var info = dirInfo.GetFiles("*.*");
            var files_to_delete = new List<string>();
            foreach (var f in info)
            {
                if (alFiles.Contains(f.Name)) continue;
                files_to_delete.Add(f.FullName);
            }
            DeleteFiles(files_to_delete);
        }

        private static void DeleteFiles(List<string> files_to_delete)
        {
            if (files_to_delete.Count > 0)
            {
                var res = MessageBox.Show($"You are about to delete {files_to_delete.Count} files. Please confirm deletetion", "Confirmation", MessageBoxButton.YesNo, MessageBoxImage.Question);
                if (res == MessageBoxResult.Yes)
                {
                    foreach (var f in files_to_delete)
                        File.Delete(f);

                    MessageBox.Show($"Removed {files_to_delete.Count} files. Done!", "Results", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
        }

        private void BtRenameReExtractedAlignments_OnClick(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(TbDir.Text))
            {
                MessageBox.Show("Select workspace directory first.", "Problem", MessageBoxButton.OK, MessageBoxImage.Exclamation);
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
            MessageBox.Show($"Renamed {count} files. Done!", "Results", MessageBoxButton.OK, MessageBoxImage.Information);
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
                MessageBox.Show("Select workspace directory first.", "Problem", MessageBoxButton.OK, MessageBoxImage.Exclamation);
                return;
            }

            var merged = Path.Combine(TbDir.Text, _target, "merged");
            ConvertToJpg.ConvertAll(merged, Cvm, cancellationToken);
        }

        private void BtConvertPngToJpgDst_OnClick(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(TbDir.Text))
            {
                MessageBox.Show("Select workspace directory", "Problem", MessageBoxButton.OK, MessageBoxImage.Exclamation);
                return;
            }

            var original = Path.Combine(TbDir.Text, _target);
            ConvertToJpg.ConvertAll(original, Cvm, cancellationToken);
        }

        private void BtBackupOld_Click(object sender, RoutedEventArgs e)
        {
            Tools.BackupOld(TbDir.Text, _target);
        }

        private void BtMergeBackOld_Click(object sender, RoutedEventArgs e)
        {
            Tools.MergeBackOld(TbDir.Text, _target);
        }

        private void BtReviewMultipleFacesAlignments_Click(object sender, RoutedEventArgs e)
        {
            var im = new ReviewAlignments(TbDir.Text, _target) { Owner = this };
            im.ShowDialog();
        }

        private void BtArrangeImagePack_OnClick(object sender, RoutedEventArgs e)
        {
            Tools.ArrangeImagePack(Cvm);
        }

        private void BtFindBrokenAlignments_OnClick(object sender, RoutedEventArgs e)
        {
            Tools.StartReviewBrokenAlignments(TbDir.Text, _target, this);
        }

        private void BtDissimilar_Click(object sender, RoutedEventArgs e)
        {
            Tools.StartDissimilar(TbDir.Text, _target, Cvm);
        }

        private void BtCompareImages_Click(object sender, RoutedEventArgs e)
        {
            Tools.StartCompareImages(this);
        }

        private void BtGroupFilesBackToSourceFolder_Click(object sender, RoutedEventArgs e)
        {
            var res = WPFCustomMessageBox.CustomMessageBox.ShowYesNoCancel("Do you want to recreate from source image pack (Yes) or arrange similar names images into different folders (No)?", "Question", "Recreate", "Arrange similar","Cancel", MessageBoxImage.Question );
            if (res ==  MessageBoxResult.Yes)
                Tools.ReCreateImagePack(Cvm);
            if (res == MessageBoxResult.No)
                Tools.ArrangeIntoFolders(Cvm);
        }

        private void BtGroupFilesBackToSourceFolder2_Click(object sender, RoutedEventArgs e)
        {
            Tools.ReNameFavorites(Cvm);
        }

        private void BtFindIdentical_Click(object sender, RoutedEventArgs e)
        {
            Tools.StartFindIdenticalImages(this);
        }

        private void BtFindIdenticalBySizeAndShortName_Click(object sender, RoutedEventArgs e)
        {
            Tools.StartFindIdenticalBySizeAndShortName(this);
        }

        private void BtCategorizeMerged_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(TbDir.Text))
            {
                MessageBox.Show("Select workspace directory first.", "Problem", MessageBoxButton.OK, MessageBoxImage.Exclamation);
                return;
            }
                        
            var im = new ShowCategorizeMerged(TbDir.Text, _target) { Owner = this };
            im.ShowDialog();
            im = null;
        }

        private void BtRenameFolder_Click(object sender, RoutedEventArgs e)
        {
            Tools.RenameFolderTest(this);
        }
    }
}
