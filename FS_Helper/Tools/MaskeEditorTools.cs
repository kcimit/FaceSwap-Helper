using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace FS_Helper
{ 
    public static partial class Tools
    {
        public static void BackupOld(string dir, string _target)
        {
            if (string.IsNullOrEmpty(dir))
            {
                MessageBox.Show("Select workspace directory first.", "Problem", MessageBoxButton.OK, MessageBoxImage.Exclamation);
                return;
            }

            var alignments = Path.Combine(dir, _target, "aligned");

            var dirAlInfo = new DirectoryInfo(alignments);
            var alInfo = dirAlInfo.GetFiles("*.*");
            if (alInfo.Length == 0)
            {
                MessageBox.Show("There are no files in aligned directory", "Problem", MessageBoxButton.OK, MessageBoxImage.Exclamation);
                return;
            }
            var creationDates = alInfo.Select(r => r.LastWriteTime).OrderBy(r => r).ToList();
            var tupleDates = new List<Tuple<DateTime, DateTime>>();
            var start_date = creationDates.First();
            var end_date = creationDates.First();
            foreach (var d in creationDates)
            {
                if ((d - end_date) < new TimeSpan(0, 1, 0))
                {
                    end_date = d;
                    continue;
                }
                tupleDates.Add(new Tuple<DateTime, DateTime>(start_date, end_date));
                start_date = d;
                end_date = d;
            }
            var last_period = new Tuple<DateTime, DateTime>(start_date, end_date);
            if (!tupleDates.Any() || (tupleDates.Last() != last_period))
                tupleDates.Add(last_period);
            var choices = new Dictionary<string, Tuple<DateTime, DateTime>>();
            tupleDates.Reverse();
            foreach (var tdate in tupleDates)
                choices.Add($"{tdate.Item1} - {tdate.Item2}", tdate);
            var cbdialog = new CBDialog("Select date ranges", "Select date range of files to be kept in aligned folder for mask editor", choices.Keys.ToList());
            cbdialog.ShowDialog();
            if (!cbdialog.Selected) return;
            var selectedDates = choices[cbdialog.SelectedItem];

            // Preparing target folders
            var aligned_backup = Path.Combine(dir, _target, "aligned_backup");
            if (!Directory.Exists(aligned_backup))
                Directory.CreateDirectory(aligned_backup);

            var dirInfo = new DirectoryInfo(aligned_backup);
            var info = dirInfo.GetFiles("*.*");
            if (info.Length > 0)
            {
                var res = MessageBox.Show("There are files in aligned_backup folder. Do you want to remove them?", "Question", MessageBoxButton.YesNoCancel, MessageBoxImage.Question);
                if (res != MessageBoxResult.Yes)
                {
                    MessageBox.Show($"Aborted!", "User aborted", MessageBoxButton.OK, MessageBoxImage.Exclamation);
                    return;
                }
                foreach (var f in info)
                    File.Delete(f.FullName);
            }
            var debug_backup = Path.Combine(dir, _target, "debug_backup");
            if (!Directory.Exists(debug_backup))
                Directory.CreateDirectory(debug_backup);

            var dirInfo_debug = new DirectoryInfo(debug_backup);
            var info_debug = dirInfo_debug.GetFiles("*.*");
            if (info_debug.Length > 0)
            {
                var res = MessageBox.Show("There are files in debug_backup folder. Do you want to remove them?", "Question", MessageBoxButton.YesNoCancel, MessageBoxImage.Question);
                if (res != MessageBoxResult.Yes)
                {
                    MessageBox.Show($"Aborted!", "User aborted", MessageBoxButton.OK, MessageBoxImage.Exclamation);
                    return;
                }
                foreach (var f in info_debug)
                    File.Delete(f.FullName);
            }

            // Moving files
            var a_count = 0;
            var d_count = 0;
            foreach (var f in alInfo)
            {
                if (f.LastWriteTime >= selectedDates.Item1 && f.LastWriteTime <= selectedDates.Item2) continue;
                var new_name = Path.Combine(aligned_backup, f.Name);
                File.Move(f.FullName, new_name);
                a_count++;
                var source_name_debug = Path.Combine(dir, _target, "aligned_debug", f.Name);
                var new_name_debug = Path.Combine(debug_backup, f.Name);
                File.Move(source_name_debug, new_name_debug);
                d_count++;
            }
            MessageBox.Show($"Done! Moved {a_count} alignments and {d_count} debug alignments.", "Results", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        public static void MergeBackOld(string dir, string _target)
        {
            if (string.IsNullOrEmpty(dir))
            {
                MessageBox.Show("Select workspace directory first.", "Problem", MessageBoxButton.OK, MessageBoxImage.Exclamation);
                return;
            }

            var aligned_backup = Path.Combine(dir, _target, "aligned_backup");
            if (!Directory.Exists(aligned_backup))
            {
                MessageBox.Show("aligned_backup folder does not exists. Aborting.", "Problem", MessageBoxButton.OK, MessageBoxImage.Exclamation);
                return;
            }
            var dirInfo = new DirectoryInfo(aligned_backup);
            var info = dirInfo.GetFiles("*.*");

            var debug_backup = Path.Combine(dir, _target, "debug_backup");
            if (!Directory.Exists(debug_backup))
            {
                MessageBox.Show("debug_backup folder does not exists. Aborting.", "Problem", MessageBoxButton.OK, MessageBoxImage.Exclamation);
                return;
            }

            var dirInfo_debug = new DirectoryInfo(debug_backup);
            var info_debug = dirInfo_debug.GetFiles("*.*");

            // Moving files
            var a_count = 0;
            var d_count = 0;
            foreach (var f in info)
            {
                var new_name = Path.Combine(dir, _target, "aligned", f.Name);
                File.Move(f.FullName, new_name);
                a_count++;
            }
            foreach (var f in info_debug)
            {
                var new_name_debug = Path.Combine(dir, _target, "aligned_debug", f.Name);
                File.Move(f.FullName, new_name_debug);
                d_count++;
            }
            MessageBox.Show($"Done! Moved {a_count} alignments and {d_count} debug alignments.", "Results", MessageBoxButton.OK, MessageBoxImage.Information);
        }
    }
}
