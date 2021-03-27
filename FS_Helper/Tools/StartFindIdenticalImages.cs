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

            var info1 = path1.GetFiles("*.jpg");
            if (!info1.Any())
            {
                info1 = path1.GetFiles("*.png");
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
    }
}
