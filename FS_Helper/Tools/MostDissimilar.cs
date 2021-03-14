using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;
using XnaFan.ImageComparison;

namespace FS_Helper
{
    public static partial class Tools
    {

        public static void StartDissimilar(string dir, string _target, ConnectionViewModel cvm)
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
            var target_path = Path.Combine(dir, _target, "aligned_most_dissimilar");
            if (!Directory.Exists(target_path)) Directory.CreateDirectory(target_path);

            var info = dirInfo.GetFiles("*.*");
            var source = new List<string>();
            foreach (var f in info.OrderBy(r => r.Name))
            {
                source.Add(f.Name);
            }
            
            var tsk =
                    Task.Factory.StartNew(() =>
                    {
                        return FoundDissimilar(source, path, cvm);
                    });

            while (!tsk.IsCompleted)
                Application.Current.Dispatcher.Invoke(DispatcherPriority.Background, new Action(delegate { }));
            cvm.Status = "Ready.";
            var most_dis = tsk.Result;
            
            int rcount = 0;
            foreach (var f in most_dis)
            {
                try
                {
                    File.Copy(Path.Combine(path, f), Path.Combine(target_path, f));
                    rcount++;
                }
                catch { }
            }
            MessageBox.Show($"{rcount} most dissimilar alignments found.", "Results", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private static List<string> FoundDissimilar(List<string> source, string path, ConnectionViewModel cvm)
        {
            var most_dis = new List<string>();
            most_dis.Add(source.First());
            for (int i = 1; i < source.Count; i++)
            {
                int difference = (int)(100.0 * ImageTool.GetPercentageDifference(System.IO.Path.Combine(path, source[i - 1]), System.IO.Path.Combine(path, source[i])));
                if (difference > 55) most_dis.Add(source[i]);
                cvm.Status = $"Comparing {i}/{source.Count}, found {most_dis.Count}";
            }
            return most_dis;
        }
    }
}
