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
        public static void StartReviewBrokenAlignments(string dir, string _target, MainWindow main)
        {
            if (string.IsNullOrEmpty(dir))
            {
                MessageBox.Show("Select workspace directory first.", "Problem", MessageBoxButton.OK, MessageBoxImage.Exclamation);
                return;
            }

            var path = Path.Combine(dir, _target, "aligned");
            var dirInfo = new DirectoryInfo(path);
            if (!Directory.Exists(path))
            {
                MessageBox.Show("Aligned directory does not exist.", "Problem", MessageBoxButton.OK, MessageBoxImage.Exclamation);
                return;
            }
            var target_path = Path.Combine(dir, _target, "aligned_broken");
            if (!Directory.Exists(target_path)) Directory.CreateDirectory(target_path);

            var info = dirInfo.GetFiles("*.*");
            var alignments = new List<string>();
            foreach (var f in info)
            {
                if (!f.Extension.Equals(".png") && !f.Extension.Equals(".jpg")) continue;
                alignments.Add(f.Name);
            }
            if (!alignments.Any())
            {
                MessageBox.Show("No alignments found.", "Problem", MessageBoxButton.OK, MessageBoxImage.Exclamation);
                return;
            }

            var im = new ReviewBrokenAlignments(alignments, path) { Owner = main };
            im.ShowDialog();
            if (im.Aborted)
            {
                MessageBox.Show("Aborted.", "User aborted", MessageBoxButton.OK, MessageBoxImage.Exclamation);
                return;
            }

            int rcount = 0;
            foreach (var f in im.BrokenReviewed)
            {
                File.Move(Path.Combine(path, f), Path.Combine(target_path, f));
                rcount++;
            }
            
            MessageBox.Show($"Broken {rcount} files moved to aligned_broken folder.", "Results", MessageBoxButton.OK, MessageBoxImage.Information);
            im = null;
        }
    }
}
