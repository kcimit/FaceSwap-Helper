using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;

namespace FS_Helper
{
    public static partial class Tools
    {
        internal static string CompareFolder1="";
        internal static string CompareFolder2="";
        public static void StartCompareImages(MainWindow main)
        {
            var selectedPath = "";
            if (!string.IsNullOrEmpty(CompareFolder1))
                selectedPath = CompareFolder1;
            var path0 = "";
            using (var dialog = new System.Windows.Forms.FolderBrowserDialog() {  Description= "Select first folder", SelectedPath=selectedPath })
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
            var im = new CompareImages(path0, path1) { Owner = main };
            im.ShowDialog();
        }
    }
}
