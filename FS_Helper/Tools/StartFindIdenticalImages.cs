using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace FS_Helper
{
    public  static partial class Tools
    {
        
        public static void StartFindIdenticalImages(MainWindow main)
        {
            DirectoryInfo path1 = null;
            using (var dialog = new System.Windows.Forms.FolderBrowserDialog() { Description = "Select images folder" })
            {
                var result = dialog.ShowDialog();
                if (result == System.Windows.Forms.DialogResult.OK)
                    path1 = new DirectoryInfo(dialog.SelectedPath);
                else return;
            }

            var info1 = path1.GetFiles("*.jp*", SearchOption.TopDirectoryOnly).ToList();
            if (!info1.Any())
            {
                info1 = path1.GetFiles("*.png", SearchOption.TopDirectoryOnly).ToList();
                if (!info1.Any())
                {
                    MessageBox.Show("No jpg or png files are found in the first folder. Aborting.");
                    return;
                }
            }
            
            var files = info1.Select(r => r.FullName).ToList();

            var im = new FindIdentical(files) { Owner = main };
            im.ShowDialog();
            if (im.Aborted)
            {
                MessageBox.Show("Aborted.", "User aborted", MessageBoxButton.OK, MessageBoxImage.Exclamation);
                return;
            }

            int rcount = 0;
            foreach (var f in im.IdenticalReviewed)
            {
                File.Delete(f);
                rcount++;
            }
            
            MessageBox.Show($"Identical {rcount} files deleted.", "Results", MessageBoxButton.OK, MessageBoxImage.Information);
            im = null;
        }

        public static void StartFindIdenticalBySizeAndShortName(MainWindow main)
        {
            var selectedPath = "";
            if (!string.IsNullOrEmpty(CompareFolder1))
                selectedPath = CompareFolder1;
            var path0 = "";
            using (var dialog = new System.Windows.Forms.FolderBrowserDialog() { Description = "Select first folder", SelectedPath = selectedPath })
            {
                var result = dialog.ShowDialog();
                if (result == System.Windows.Forms.DialogResult.OK)
                    path0 = dialog.SelectedPath;
                else return;

            }
            selectedPath = path0;
            if (!string.IsNullOrEmpty(CompareFolder2))
                selectedPath = CompareFolder2;

            var path1 = "";
            using (var dialog = new System.Windows.Forms.FolderBrowserDialog() { Description = "Select second folder", SelectedPath = selectedPath })
            {
                var result = dialog.ShowDialog();
                if (result == System.Windows.Forms.DialogResult.OK)
                    path1 = dialog.SelectedPath;
                else return;
            }

            CompareFolder1 = path0;
            CompareFolder2 = path1;

            var ext = new List<string> { ".jpg", ".gif", ".png" };
            var files1 = new Dictionary<string, long>();
            foreach (var file in Directory
                .EnumerateFiles(CompareFolder1, "*.*", SearchOption.TopDirectoryOnly)
                .Where(s => ext.Contains(Path.GetExtension(s).ToLowerInvariant())))
            {
                var fi = new System.IO.FileInfo(file);
                files1.Add(Path.GetFileNameWithoutExtension(file), fi.Length);
            }
            var copyPath = CompareFolder2 + "\\found\\";
            if (!Directory.Exists(copyPath))
                Directory.CreateDirectory(copyPath);
            
            foreach (var file in Directory
                .EnumerateFiles(CompareFolder2, "*.*", SearchOption.TopDirectoryOnly)
                .Where(s => ext.Contains(Path.GetExtension(s).ToLowerInvariant())))
            {
                var fi = new System.IO.FileInfo(file);
                var name = Path.GetFileName(file);
                foreach (var k in files1)
                {
                    if (k.Value==fi.Length && name.StartsWith(k.Key))
                    {
                        File.Move(file, Path.Combine(copyPath, name));
                    }
                }
            }
        }
    }
}
