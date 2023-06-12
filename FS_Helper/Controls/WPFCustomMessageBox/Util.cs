// -----------------------------------------------------------------------
// <copyright file="Util.cs">
// TODO: Update copyright text.
// </copyright>
// -----------------------------------------------------------------------

namespace WPFCustomMessageBox
{
    using System;
    using System.Collections.Generic;
    using System.Drawing;
    using System.IO;
    using System.Linq;
    using System.Runtime.InteropServices;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;
    using System.Windows;
    using System.Windows.Interop;
    using System.Windows.Media;
    using System.Windows.Media.Imaging;
    using Microsoft.Win32.SafeHandles;

    /// <summary>
    /// TODO: Update summary.
    /// </summary>
    public static class Util
    {
        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        public struct WIN32_FIND_DATA
        {
            public FileAttributes dwFileAttributes;
            public FILETIME ftCreationTime;
            public FILETIME ftLastAccessTime;
            public FILETIME ftLastWriteTime;
            public uint nFileSizeHigh; //changed all to uint, otherwise you run into unexpected overflow
            public uint nFileSizeLow;  //|
            public uint dwReserved0;   //|
            public uint dwReserved1;   //v
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 260)]
            public string cFileName;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 14)]
            public string cAlternate;
        }

        /// <summary>
        /// https://docs.microsoft.com/en-us/windows/win32/api/fileapi/nf-fileapi-findfirstfilew
        /// </summary>
        [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        internal static extern IntPtr FindFirstFile(
            string lpFileName,
            ref WIN32_FIND_DATA lpFindFileData
            );

        /// <summary>
        /// https://docs.microsoft.com/en-us/windows/win32/api/fileapi/nf-fileapi-findnextfilew
        /// </summary>
        [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        internal static extern bool FindNextFile(
            IntPtr hFindFile,
            ref WIN32_FIND_DATA lpFindFileData
            );

        /// <summary>
        /// https://docs.microsoft.com/en-us/windows/win32/api/fileapi/nf-fileapi-findclose
        /// </summary>
        [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        internal static extern bool FindClose(
            IntPtr hFindFile
            );

        public static Tuple<List<string>, List<string>> GetFilesDirectories(string path, CancellationToken token)
        {
            if (String.IsNullOrWhiteSpace(path))
                throw new ArgumentNullException("path", "The provided path is NULL or empty.");

            // If the provided path doesn't end in a backslash, append one.
            if (path.Last() != '\\')
                path += '\\';

            IntPtr hFile = IntPtr.Zero;
            WIN32_FIND_DATA fd = new WIN32_FIND_DATA();
            List<string> directories = new List<string>();
            List<string> files = new List<string>();

            try
            {
                hFile = FindFirstFile(
                    path + "*", // Discover all files/folders by ending a directory with "*", e.g. "X:\*".
                    ref fd
                    );

                // If we encounter an error, or there are no files/directories, we return no entries.
                if (hFile.ToInt64() == -1)
                    return Tuple.Create<List<string>, List<string>>(files, directories);

                //
                // Find (and count) each file/directory, then iterate through each directory in parallel to maximize performance.
                //
                do
                {
                    // If a directory (and not a Reparse Point), and the name is not "." or ".." which exist as concepts in the file system,
                    // count the directory and add it to a list so we can iterate over it in parallel later on to maximize performance.
                    if ((fd.dwFileAttributes & FileAttributes.Directory) != 0 &&
                        (fd.dwFileAttributes & FileAttributes.ReparsePoint) == 0 &&
                        fd.cFileName != "." && fd.cFileName != "..")
                    {
                        directories.Add(System.IO.Path.Combine(path, fd.cFileName));
                    }
                    // Otherwise, if this is a file ("archive")
                    else if ((fd.dwFileAttributes & FileAttributes.Archive) != 0)
                    {
                        files.Add(System.IO.Path.Combine(path, fd.cFileName));
                    }
                }
                while (FindNextFile(hFile, ref fd));

                // Iterate over each discovered directory in parallel to maximize file/directory counting performance,
                // calling itself recursively to traverse each directory completely.
                Parallel.ForEach(
                    directories,
                    new ParallelOptions()
                    {
                        CancellationToken = token
                    },
                    directory =>
                    {
                        var res = GetFilesDirectories(
                            directory,
                            token
                            );

                        if (res.Item2.Any())
                            lock (directories)
                        {
                            directories.AddRange(res.Item2);
                        }

                        if (res.Item1.Any())
                            lock (files)
                            {
                                files.AddRange(res.Item1);
                            }
                    });
            }
            catch (Exception)
            {
                // Handle as desired.
            }
            finally
            {
                if (hFile.ToInt64() != 0)
                   FindClose(hFile);
            }

            return Tuple.Create<List<string>, List<string>>(files, directories);
        }

        public static List<string> GetFilesWithExtension(string path, string extension, CancellationToken token)
        {
            var files = new List<string>();
            if (string.IsNullOrEmpty(extension))
                return new List<string>();

            var fdirs = GetFilesDirectories(path, token);
            var extensions=extension.Split('|');
            Parallel.ForEach(
                    fdirs.Item1,
                    new ParallelOptions()
                    {
                        CancellationToken = token
                    },
                    fdir =>
                    {
                        foreach (var ext in extensions)
                        {
                            if (fdir.EndsWith(ext))
                            {
                                lock (files)
                                {
                                    files.Add(fdir);
                                }
                                break;
                            }
                        }
                    });
            return files;
        }

        internal static ImageSource ToImageSource(this Icon icon)
        {
            ImageSource imageSource = Imaging.CreateBitmapSourceFromHIcon(
                icon.Handle,
                Int32Rect.Empty,
                BitmapSizeOptions.FromEmptyOptions());

            return imageSource;
        }

        /// <summary>
        /// Keyboard Accellerators are used in Windows to allow easy shortcuts to controls like Buttons and 
        /// MenuItems. These allow users to press the Alt key, and a shortcut key will be highlighted on the 
        /// control. If the user presses that key, that control will be activated.
        /// This method checks a string if it contains a keyboard accellerator. If it doesn't, it adds one to the
        /// beginning of the string. If there are two strings with the same accellerator, Windows handles it.
        /// The keyboard accellerator character for WPF is underscore (_). It will not be visible.
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        internal static string TryAddKeyboardAccellerator(this string input)
        {
            const string accellerator = "_";            // This is the default WPF accellerator symbol - used to be & in WinForms

            // If it already contains an accellerator, do nothing
            if (input.Contains(accellerator)) return input;

            return accellerator + input;
        }

        public static void SafeDelete(string file)
        {
            try
            {
                System.IO.File.Delete(file);
            }
            catch { }
        }
    }
}
