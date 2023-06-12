using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;

namespace FS_Helper
{
    public static partial class Tools
    {
        public static void RenameFolderTest(MainWindow main)
        {
            var selectedPath = "";
            using (var dialog = new System.Windows.Forms.FolderBrowserDialog() {  Description= "Select first folder", SelectedPath=selectedPath })
            {
                var result = dialog.ShowDialog();
                if (result == System.Windows.Forms.DialogResult.OK)
                    selectedPath = dialog.SelectedPath;
                else return;
            }

            var directories = CustomSearcher.GetDirectories(selectedPath);

            if (!directories.Any())
            {
                MessageBox.Show($"No subdirectories are found in {selectedPath}", "Problem", MessageBoxButton.OK, MessageBoxImage.Exclamation);
                return;
            }
            var dict=new Dictionary<string, string>();
            foreach (var dir in directories)
            {
                var d = dir.Trim();
                var ind1=d.LastIndexOf('(');
                var ind2 = d.LastIndexOf(')');
                if (ind2-ind1!=11)
                    continue;
                var date=d.Substring(ind1+1, 10);
                if (!int.TryParse(date.Substring(0, 2), out int day))
                    continue;
                if (!int.TryParse(date.Substring(3, 2), out int month))
                    continue;
                if (!int.TryParse(date.Substring(6, 4), out int year))
                    continue;
                var newName = $"{d.Substring(0, d.LastIndexOf('\\'))}\\{year:D4}-{month:D2}-{day:D2} {d.Substring(d.LastIndexOf('\\')+1, ind1- d.LastIndexOf('\\')-1)}".Trim();
                dict.Add(dir, newName);
            }
            foreach (var d in dict)
            {
                Directory.Move(d.Key, d.Value);
            }
        }
    }
}
