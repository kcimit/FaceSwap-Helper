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
        public static void StartReviewAlignments(string dir, string _target, MainWindow main)
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
            var target_path = Path.Combine(dir, _target, "aligned_reviewed");
            if (!Directory.Exists(target_path)) Directory.CreateDirectory(target_path);

            var info = dirInfo.GetFiles("*.*");
            var pairs = new Dictionary<string, List<string>>();
            foreach (var f in info)
            {
                var name = Path.GetFileNameWithoutExtension(f.FullName);
                var suffix = name.Substring(name.Length - 2, 2);
                if (!suffix[0].Equals('_')) continue;

                var fname = name.Substring(0, name.Length - 2) + f.Extension;

                if (!pairs.ContainsKey(fname)) pairs.Add(fname, new List<string>());
                pairs[fname].Add(Path.GetFileNameWithoutExtension(f.FullName) + f.Extension);
            }
            foreach (var item in pairs.Where(kvp => kvp.Value.Count < 2).ToList())
            {
                pairs.Remove(item.Key);
            }
            if (pairs.Count == 0)
            {
                MessageBox.Show("No alignments with multiple faces found.", "Problem", MessageBoxButton.OK, MessageBoxImage.Exclamation);
                return;
            }

            var im = new ReviewAlignments(pairs, path) { Owner = main };
            im.ShowDialog();
            if (im.Aborted)
            {
                MessageBox.Show("Aborted.", "User aborted", MessageBoxButton.OK, MessageBoxImage.Exclamation);
                return;
            }

            int dcount = 0;
            int rcount = 0;
            var ar = im.AlignmentsReviewed;
            foreach (var pair in pairs)
            {
                if (!ar.ContainsKey(pair.Key)) continue;
                if (ar[pair.Key].Equals("stop"))
                {
                    foreach (var f in pair.Value)
                        try
                        {
                            File.Delete(Path.Combine(path, f));
                            dcount++;
                        }
                        catch { }
                }
                else
                {
                    foreach (var f in pair.Value)
                    {
                        if (f.Equals(ar[pair.Key]))
                        {
                            File.Move(Path.Combine(path, f), Path.Combine(target_path, pair.Key));
                            rcount++;
                        }
                        else
                            try
                            {
                                File.Delete(Path.Combine(path, f));
                                dcount++;
                            }
                            catch { }
                    }
                }
            }
            MessageBox.Show($"{dcount} files deleted and {rcount} alignments reviewed in aligned_reviewed folder.", "Results", MessageBoxButton.OK, MessageBoxImage.Information);
            im = null;
        }
    }
}
