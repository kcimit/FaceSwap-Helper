using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;

namespace FS_Helper
{
    public  static partial class Tools
    {
        public static void StartCompareImages(MainWindow main)
        {
            DirectoryInfo path1 = null;
            using (var dialog = new System.Windows.Forms.FolderBrowserDialog() {  Description= "Select first folder" })
            {
                var result = dialog.ShowDialog();
                if (result == System.Windows.Forms.DialogResult.OK)
                    path1 = new DirectoryInfo(dialog.SelectedPath);
                else return;
            }

            DirectoryInfo path2 = null;
            using (var dialog = new System.Windows.Forms.FolderBrowserDialog() { Description = "Select second folder", SelectedPath = path1.FullName })
            {
                var result = dialog.ShowDialog();
                if (result == System.Windows.Forms.DialogResult.OK)
                    path2 = new DirectoryInfo(dialog.SelectedPath);
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
            var info2 = path2.GetFiles("*.jpg");
            if (!info2.Any())
            {
                info2 = path2.GetFiles("*.png");
                if (!info2.Any())
                {
                    MessageBox.Show("No jpg or png files are found in the second folder. Aborting.");
                    return;
                }
            }

            var pairs = new Dictionary<string, List<string>>();
            foreach (var f in info1)
            {
                var fn= Path.GetFileName(f.FullName);
                if (!pairs.ContainsKey(fn))
                    pairs.Add(fn, new List<string>() { f.FullName });
                else
                    pairs[fn].Add(f.FullName);
            }
            foreach (var f in info2)
            {
                var fn = Path.GetFileName(f.FullName);
                if (!pairs.ContainsKey(fn))
                    pairs.Add(fn, new List<string>() { f.FullName });
                else
                    pairs[fn].Add(f.FullName);
            }
            var im = new CompareImages(pairs) { Owner = main };
            im.ShowDialog();
        }
    }
}
