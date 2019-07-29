using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;


namespace FS_Helper
{
    public static class ConvertToJpg
    {
        private static int _globalCount;
        private static int _allCount;
        private static Mutex _locker;

        public static void ConvertAll(string folder, ConnectionViewModel cvm)
        {
            _globalCount = 0;
            _locker=new Mutex();

            var numCores = Math.Min(Environment.ProcessorCount, 10);

            var dirInfo = new DirectoryInfo(folder);
            var alFiles = dirInfo.GetFiles("*.png");
            _allCount = alFiles.Length;

            if (_allCount == 0)
            {
                MessageBox.Show("Nothing to convert. Aborting.");
                return;
            }

            var batchSize = (_allCount / numCores) + 1;

            var count = 0;
            var tasks = new List<Task>();

            for (var i = 0; i < numCores; i++)
            {
                var list = new List<string>();
                for (var x = 0; x < batchSize; x++)
                {
                    list.Add(alFiles[count].FullName);
                    count++;
                    if (count >= _allCount)
                        break;
                }

                if (!list.Any()) break;
                var tsk =
                    Task.Factory.StartNew(() =>
                {
                    ToJpg(list, cvm);
                    return;
                });
                tasks.Add(tsk);
            }

            while (tasks.Any(r => !r.IsCompleted))
                Application.Current.Dispatcher.Invoke(DispatcherPriority.Background, new Action(delegate { }));
            cvm.Status = "Ready.";
        }

        /// <summary>
        /// Converts all images in a folder to JPG
        /// </summary>
        private static void ToJpg(IReadOnlyCollection<string> files, ConnectionViewModel cvm)
        {
            var encoder = System.Drawing.Imaging.Encoder.Quality;
            var encoderParameters = new EncoderParameters(1) {Param = {[0] = new EncoderParameter(encoder, 85L)}};

            var jpgEncoder = GetEncoder(ImageFormat.Jpeg);
            foreach (var file in files)
            {
                var extension = System.IO.Path.GetExtension(file);
                if (extension != ".png") continue;
                var name = System.IO.Path.GetFileNameWithoutExtension(file);
                var path = System.IO.Path.GetDirectoryName(file);
                lock (_locker)
                {
                    cvm.Status = $"Converting {++_globalCount}/{_allCount}";
                }

                var png = Image.FromFile(file);
                png.Save($@"{path}\{name}.jpg", jpgEncoder, encoderParameters);
                png.Dispose();
                File.Delete(file);
            }
        }

        private static ImageCodecInfo GetEncoder(ImageFormat format)
        {
            var codecs = ImageCodecInfo.GetImageEncoders();
            return codecs.FirstOrDefault(codec => codec.FormatID == format.Guid);
        }
    }
}
