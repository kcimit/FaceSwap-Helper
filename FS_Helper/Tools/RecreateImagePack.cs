using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;

namespace FS_Helper
{
    public static partial class Tools
    {
        private static string GetHash(HashAlgorithm hashAlgorithm, FileStream input)
        {
            // Convert the input string to a byte array and compute the hash.
            byte[] data = hashAlgorithm.ComputeHash(input);

            // Create a new Stringbuilder to collect the bytes
            // and create a string.
            var sBuilder = new StringBuilder();

            // Loop through each byte of the hashed data
            // and format each one as a hexadecimal string.
            for (int i = 0; i < data.Length; i++)
            {
                sBuilder.Append(data[i].ToString("x2"));
            }

            // Return the hexadecimal string.
            return sBuilder.ToString();
        }

        public static void ReCreateImagePack(ConnectionViewModel cvm)
        {
            var dirArranged = string.Empty;
            var dirImagePack = string.Empty;

            using (var dialog = new System.Windows.Forms.FolderBrowserDialog())
            {
                dialog.Description = "Select arranged images folder";
                var result = dialog.ShowDialog();
                if (result == System.Windows.Forms.DialogResult.OK)
                    dirArranged = dialog.SelectedPath;
                else return;
                dialog.Description = "Select image pack root folder";
                result = dialog.ShowDialog();
                if (result == System.Windows.Forms.DialogResult.OK)
                    dirImagePack = dialog.SelectedPath;
                else return;
            }
            var d = new DirectoryInfo(dirArranged);
            var arrangedFiles = d.GetFiles("*.jp*", SearchOption.TopDirectoryOnly).ToList();
            if (!arrangedFiles.Any())
            {
                MessageBox.Show($"No images found in arranged folder {dirArranged}", "Problem", MessageBoxButton.OK, MessageBoxImage.Exclamation);
                return;
            }

            var directories = CustomSearcher.GetDirectories(dirImagePack);
            if (!directories.Any())
            {
                MessageBox.Show($"No subdirectories are found in {dirImagePack}", "Problem", MessageBoxButton.OK, MessageBoxImage.Exclamation);
                return;
            }
            var imagePackHashes = new Dictionary<string, Dictionary<string, List<string>>>();
            int i = 0;
            var tsk =
                   Task.Run(() =>
                   {
                       using (var md5 = MD5.Create())
                       {
                           foreach (var pack in directories)
                           {
                               try
                               {
                                   var dp = new DirectoryInfo(pack);
                                   var files = dp.GetFiles("*.jp*", SearchOption.TopDirectoryOnly).ToList();
                                   if (!files.Any()) continue;
                                   foreach (var file in files)
                                   {
                                       using (var stream = File.OpenRead(file.FullName))
                                       {
                                           var hash = GetHash(md5, stream);
                                           if (!imagePackHashes.TryGetValue(hash, out Dictionary<string, List<string>> packFolders))
                                               imagePackHashes.Add(hash, new Dictionary<string, List<string>>() { { dp.Name, new List<string>() { file.FullName } } });
                                           else
                                           {
                                               if (!packFolders.TryGetValue(dp.Name, out List<string> packFiles))
                                                   packFolders.Add(dp.Name, new List<string>() { file.FullName });
                                               else
                                                   packFiles.Add(file.FullName);
                                           }
                                       }
                                       cvm.Status = $"Getting MD5 checksum file {++i}";
                                   }
                               }
                               catch (Exception e) { MessageBox.Show(e.Message); }
                           }

                           i = 0;
                           foreach (var file in arrangedFiles)
                           {
                               bool found = false;
                               Dictionary<string, List<string>> folderFile = null;
                               using (var stream = File.OpenRead(file.FullName))
                               {
                                   var hash = GetHash(md5, stream);
                                   if (imagePackHashes.TryGetValue(hash, out folderFile))
                                       found = true;
                               }
                               if (found)
                               {
                                   var fileMoved = false;
                                   foreach (var pathName in folderFile.Keys)
                                   {
                                       var newPath = Path.Combine(dirArranged, pathName);
                                       if (!Directory.Exists(newPath)) Directory.CreateDirectory(newPath);
                                       if (!fileMoved)
                                       {
                                           File.Move(file.FullName, Path.Combine(newPath, Path.GetFileName(file.FullName)));
                                           fileMoved = true;
                                       }
                                   }
                               }
                               cvm.Status = $"Processing MD5 checksum of arranged file {++i}/{arrangedFiles.Count}";
                           }
                       }
                   });
            while (!tsk.IsCompleted)
                Application.Current.Dispatcher.Invoke(DispatcherPriority.Background, new Action(delegate { }));

            cvm.Status = "Ready.";
        }
    }
}
