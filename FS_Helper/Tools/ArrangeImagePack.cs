using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
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
            char letter = 'a';
            var inputDialog = new InputDialog("Please enter starting letter (a-z):", "a");
            if (inputDialog.ShowDialog() == true)
                letter = inputDialog.Answer.ToLower()[0];
            var isAlphaBet = Regex.IsMatch(letter.ToString(), "[a-z]", RegexOptions.IgnoreCase);
            if (!isAlphaBet)
            {
                MessageBox.Show("Only alphabet letter (a-z) is allowed. Aborting.", "Invalid input", MessageBoxButton.OK, MessageBoxImage.Exclamation);
                return;
            }
            var ind_1st_char = alphabet.IndexOf(letter);

            var extractPrefix = MessageBox.Show("Do you want to extract and use prefix (text preceeding number, or example, 'set0001')?", "Question", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes;
            var useMergePath = MessageBox.Show("Do you want to specify the path were arranged files are going to be merged to?", "Question", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes;

            var dir = string.Empty;
            using (var dialog = new System.Windows.Forms.FolderBrowserDialog { Description = "Indicate source folder" })
            {
                var result = dialog.ShowDialog();
                if (result == System.Windows.Forms.DialogResult.OK)
                    dir = dialog.SelectedPath;
                else return;
            }

            var mergePath = string.Empty;
            var existingFiles = new List<string>();
            if (useMergePath)
                using (var dialog = new System.Windows.Forms.FolderBrowserDialog { Description = "Indicate merge path" })
                {
                    var result = dialog.ShowDialog();
                    if (result == System.Windows.Forms.DialogResult.OK)
                        mergePath = dialog.SelectedPath;
                    else return;

                    existingFiles = new DirectoryInfo(mergePath).GetFiles("*.jp*", SearchOption.TopDirectoryOnly)
                        .Select(r => r.Name.Replace(".jpg", "").Replace("0", "").Replace("1", "").Replace("2", "").Replace("3", "").Replace("4", "").Replace("5", "").Replace("6", "").Replace("7", "").Replace("8", "").Replace("9", ""))
                        .Distinct().ToList();
                }

            var directories = CustomSearcher.GetDirectories(dir);

            if (!directories.Any())
            {
                MessageBox.Show($"No subdirectories are found in {dir}", "Problem", MessageBoxButton.OK, MessageBoxImage.Exclamation);
                return;
            }
            var ind_2nd_char = 0;
            var ren_dict = new Dictionary<string, Dictionary<string, string>>();
            var global_cnt = 0;
            directories.Sort();
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

                var commonPart = GetCommonPart(files.Select(r => r.Name).ToList());

                var preDict = new Dictionary<string, int>();
                var preStringsDict = new Dictionary<string, string>();

                var numbers = new List<int>();
                foreach (var file in files)
                {
                    var s = file.Name;
                    if (!string.IsNullOrEmpty(commonPart))
                        s = s.Replace(commonPart, "");
                    var n = Regex.Match(s, @"\d+", RegexOptions.RightToLeft);

                    if (n.Success)
                        preDict.Add(file.FullName, Convert.ToInt32(n.Value));
                }
                if (preDict.Count == files.Count)
                {
                    foreach (var pair in preDict.OrderBy(r => r.Value))
                        preStringsDict.Add(pair.Key, files.First(r => r.FullName == pair.Key).Name);
                }
                else
                    foreach (var file in files.OrderBy(r => r.FullName))
                        preStringsDict.Add(file.FullName, file.Name);

                ren_dict.Add(pack, new Dictionary<string, string>());
                var dirName = files.First().DirectoryName;
                foreach (var file in preStringsDict)
                {
                    var name = file.Value.Replace("_", "");

                    while (true)
                    {
                        var test_name = $"{char1}{char2}";
                        if (extractPrefix)
                            test_name = $"{commonPart}_{char1}{char2}";

                        if (existingFiles.Contains(test_name))
                        {
                            char1 = alphabet[ind_1st_char];
                            char2 = alphabet[ind_2nd_char];
                            if (ind_2nd_char == alphabet.Count() - 1)
                            {
                                ind_1st_char++;
                                ind_2nd_char = 0;
                            }
                            else
                                ind_2nd_char++;
                        }
                        else
                            break;
                    }

                    var new_fn_name = $"{dirName}\\{char1}{char2}{cnt:D5}.jpg";
                    if (extractPrefix)
                        new_fn_name = $"{dirName}\\{commonPart}_{char1}{char2}{cnt:D5}.jpg";

                    cnt++; global_cnt++;
                    ren_dict[pack].Add(file.Key, new_fn_name);
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

        private static string GetCommonPart(List<string> list)
        {
            var pos = 0;

            while (pos < list.Select(r => r.Length).OrderBy(r => r).First())
            {
                var ch = list[0][pos];
                var common = true;
                for (var i = 0; list.Count > i; i++)
                {
                    if (ch != list[i][pos])
                        common = false;
                }
                if (common)
                    pos++;
                else break;
            }
            pos--;

            if (pos < 1)
                return string.Empty;
            else
                return list[0].Substring(0, pos+1);
        }
    }


public static class CustomSearcher
    {
        public static List<string> GetDirectories(string path, string searchPattern = "*",
            SearchOption searchOption = SearchOption.AllDirectories)
        {
            if (searchOption == SearchOption.TopDirectoryOnly)
                return Directory.GetDirectories(path, searchPattern).ToList();

            var directories = GetDirectories(path, searchPattern);

            for (var i = 0; i < directories.Count; i++)
                directories.AddRange(GetDirectories(directories[i], searchPattern));

            return directories;
        }

        private static List<string> GetDirectories(string path, string searchPattern)
        {
            try
            {
                path = Path.Combine(path, "");
                return Directory.GetDirectories(path, searchPattern).ToList();
            }
            catch (Exception e)
            {
                return new List<string>();
            }
        }
    }
}
