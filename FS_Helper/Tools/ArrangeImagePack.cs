using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;

namespace FS_Helper
{
    public static partial class Tools
    {
        public static void ArrangeImagePack(ConnectionViewModel cvm)
        {
            var alphabet = "abcdefghijklmnopqrstuvwxyz";

            var dir = string.Empty;
            using (var dialog = new System.Windows.Forms.FolderBrowserDialog())
            {
                var result = dialog.ShowDialog();
                if (result == System.Windows.Forms.DialogResult.OK)
                    dir = dialog.SelectedPath;
                else return;
            }

            var directories = CustomSearcher.GetDirectories(dir);

            if (!directories.Any())
            {
                MessageBox.Show($"No subdirectories are found in {dir}", "Problem", MessageBoxButton.OK, MessageBoxImage.Exclamation);
                return;
            }
            var ind_1st_char = 0;
            var ind_2nd_char = 0;
            var ren_dict = new Dictionary<string, Dictionary<string, string>>();
            var global_cnt = 0;
            foreach (var pack in directories)
            {
                var d = new DirectoryInfo(pack);
                var files = d.GetFiles("*.jp*", SearchOption.TopDirectoryOnly).ToList();
                if (!files.Any()) continue;
                var char1 = alphabet[ind_1st_char];
                var char2 = alphabet[ind_2nd_char];
                if (ind_2nd_char == alphabet.Count() - 1)
                {
                    ind_1st_char++;
                    ind_2nd_char = 0;
                }
                else
                    ind_2nd_char++;

                var cnt = 0;
                ren_dict.Add(pack, new Dictionary<string, string>());
                foreach (var file in files.OrderBy(r => r.FullName))
                {
                    var new_fn_name = $"{file.DirectoryName}\\{char1}{char2}{cnt.ToString("D5")}{file.Extension}";
                    cnt++; global_cnt++;
                    ren_dict[pack].Add(file.FullName, new_fn_name);

                }
            }
            var res = MessageBox.Show($"You are about to rename {global_cnt} files in {ren_dict.Count} folders. Confirm?", "Confirmation", MessageBoxButton.YesNoCancel, MessageBoxImage.Question);
            if (res != MessageBoxResult.Yes)
                return;

            var arranged_folder = $"{dir}\\Arranged";
            if (!Directory.Exists(arranged_folder))
                Directory.CreateDirectory(arranged_folder);
            var i = 0;
            var tsk =
                    Task.Factory.StartNew(() =>
                    {
                        try
                        {

                            foreach (var pack in ren_dict)
                            {
                                foreach (var f in pack.Value)
                                {
                                    File.Move(f.Key, f.Value);
                                    File.Copy(f.Value, $"{arranged_folder}\\{Path.GetFileName(f.Value)}", true);
                                    cvm.Status = $"Arranging file {++i}/{global_cnt}";
                                }
                            }
                        }

                        catch (Exception e)
                        {
                            MessageBox.Show($"Problem renaming {e.Message}", "Problem", MessageBoxButton.OK, MessageBoxImage.Exclamation);
                        }
                        finally
                        {
                            MessageBox.Show($"Done!", "Results", MessageBoxButton.OK, MessageBoxImage.Information);
                        }
                        return;
                    });

            while (!tsk.IsCompleted)
                Application.Current.Dispatcher.Invoke(DispatcherPriority.Background, new Action(delegate { }));

            cvm.Status = "Ready.";
        }
    }

    public static class CustomSearcher
    {
        public static List<string> GetDirectories(string path, string searchPattern = "*",
            SearchOption searchOption = SearchOption.AllDirectories)
        {
            if (searchOption == SearchOption.TopDirectoryOnly)
                return Directory.GetDirectories(path, searchPattern).ToList();

            var directories = new List<string>(GetDirectories(path, searchPattern));

            for (var i = 0; i < directories.Count; i++)
                directories.AddRange(GetDirectories(directories[i], searchPattern));

            return directories;
        }

        private static List<string> GetDirectories(string path, string searchPattern)
        {
            try
            {
                return Directory.GetDirectories(path, searchPattern).ToList();
            }
            catch (UnauthorizedAccessException)
            {
                return new List<string>();
            }
        }
    }
}
