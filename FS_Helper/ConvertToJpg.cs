using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;
using WPFCustomMessageBox;

namespace FS_Helper
{
    public static class ConvertToJpg
    {
        private static int _globalCount;
        private static int _allCount;
        private static Mutex _locker;

        public static void ConvertAll(string folder, ConnectionViewModel cvm, CancellationToken ct)
        {
            
            _locker = new Mutex();
            var numCores = Math.Min(Environment.ProcessorCount, 10);

            //var dirInfo = new DirectoryInfo(folder);
            //var alFiles = dirInfo.GetFiles("*.png", SearchOption.TopDirectoryOnly);

            var encoder = System.Drawing.Imaging.Encoder.Quality;
            var encoderParameters = new EncoderParameters(1) { Param = { [0] = new EncoderParameter(encoder, 85L) } };

            var jpgEncoder = GetEncoder(ImageFormat.Jpeg);
            var pa = new ParallelOptions
            {
                MaxDegreeOfParallelism = numCores
            };
            while (true)
            {
                var alFiles = Util.GetFilesWithExtension(folder, ".png", ct);
                _allCount = alFiles.Count;

                if (_allCount == 0)
                {
                    MessageBox.Show("Nothing to convert. Aborting.");
                    break;
                }
                _globalCount = 0;

                var tsk = Task.Factory.StartNew(() =>
                             Parallel.ForEach(alFiles, pa, file =>
                         {
                             Image png = null;
                             var success = true;
                             var name = System.IO.Path.GetFileNameWithoutExtension(file);
                             var path = System.IO.Path.GetDirectoryName(file);
                             if (ct.IsCancellationRequested)
                                 return;
                             lock (_locker)
                             {
                                 cvm.Status = $"Converting {++_globalCount}/{_allCount}";
                             }
                             
                             try
                             {
                                 png = Image.FromFile(file);
                                 png.Save($@"{path}\{name}.jpg", jpgEncoder, encoderParameters);
                             }
                             catch 
                             {
                                 success = false;
                             }
                             png.Dispose();
                             try
                             {
                                 if (success) File.Delete(file);
                                 if (!success) File.Delete($@"{path}\{name}.jpg");
                             }
                             catch { }
                         }), ct);

                while (!tsk.IsCompleted)
                    Application.Current.Dispatcher.Invoke(DispatcherPriority.Background, new Action(delegate { }));

                var tskWait = Task.Factory.StartNew(() =>
                {
                    var ts = new Stopwatch();
                    ts.Start();
                    while (ts.Elapsed.TotalSeconds < 15)
                    {
                        Thread.Sleep(100);
                        cvm.Status = $"Waiting for new images {ts.Elapsed.ToString()}";
                        if (ct.IsCancellationRequested)
                            return;
                    }
                    ts.Stop();

                    return;
                }, ct);
                while (!tskWait.IsCompleted)
                    Application.Current.Dispatcher.Invoke(DispatcherPriority.Background, new Action(delegate { }));

                Thread.Sleep(2000);
            }
            if (ct.IsCancellationRequested)
                cvm.Status = "Canceled.";
            else
                cvm.Status = "Conversion is complete.";
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
