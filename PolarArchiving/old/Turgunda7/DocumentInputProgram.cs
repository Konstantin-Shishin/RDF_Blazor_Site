using System;
using System.IO;
using System.Linq;
using ExifLib;
using FreeImageAPI;
using System.Drawing;

namespace DocumentInput
{
    class Program
    {
        // ================ Пробная программа - не используется ==================
        static void DocumentInputMain(string[] args)
        {
            string path = @"D:\Home\data\";
            Console.WriteLine("Start DocumentInput");
            //string fname = path + "WP_20170528_006.jpg";
            string fname = path + "pharris1.tiff";
            Stream stream = File.OpenRead(fname);

            //ExifInfo(stream);

            using (var original = FreeImageBitmap.FromStream(stream))
            {
                
                Console.WriteLine($"Width={original.Width} Height={original.Height} ImageFormat={original.ImageFormat} {original.ToString()}");
                foreach (var m in original.Metadata)
                {
                    Console.WriteLine($"{m}");
                }
                int x = original.Width, y = original.Height;
                double factor = 150.0 / (x > y ? (double)x : (double)y);
                int width = (int)(factor * x);
                int height = (int)(factor * y);
                var resized = new FreeImageBitmap(original, width, height);
                // JPEG_QUALITYGOOD is 75 JPEG.
                // JPEG_BASELINE strips metadata (EXIF, etc.)
                resized.Save(path + "out.jpg", FREE_IMAGE_FORMAT.FIF_JPEG,
                    FREE_IMAGE_SAVE_FLAGS.JPEG_QUALITYGOOD |
                    FREE_IMAGE_SAVE_FLAGS.JPEG_BASELINE);
            }
            //stream.Position = 0L;
            //Console.WriteLine($"ExifDate={ExifDate(stream)}");
            //Console.WriteLine($"ExifDate={ExifDate(fname)}");
            using (var image = new Bitmap(fname))
            {
                Console.WriteLine($"Width={image.Width} Height={image.Height}");
                foreach (var prop in image.PropertyItems)
                {
                    Console.WriteLine($"{prop.Id} {prop.Len} {prop.Type} {prop.Value} ");
                }

            }

            string text = "";
            // Используем пакет MesiaInfo.DotNetWrapper
            using (MediaInfo.DotNetWrapper.MediaInfo mediaInfo = new MediaInfo.DotNetWrapper.MediaInfo())
            {
                text += "\r\n\r\nOpen\r\n";
                mediaInfo.Open(fname);

                text += "\r\n\r\nInform with Complete=false\r\n";
                mediaInfo.Option("Complete");
                text += mediaInfo.Inform();

                text += "\r\n\r\nInform with Complete=true\r\n";
                mediaInfo.Option("Complete", "1");
                text += mediaInfo.Inform();

                text += "\r\n\r\nCustom Inform\r\n";
                mediaInfo.Option("Inform", "General;File size is %FileSize% bytes");
                text += mediaInfo.Inform();

                //foreach (string param in new[] { "BitRate", "BitRate/String", "BitRate_Mode" })
                //{
                //    text += "\r\n\r\nGet with Stream=Audio and Parameter='" + param + "'\r\n";
                //    text += mediaInfo.Get(StreamKind.Audio, 0, param);
                //}

                //text += "\r\n\r\nGet with Stream=General and Parameter=46\r\n";
                //text += mediaInfo.Get(StreamKind.General, 0, 46);

                //text += "\r\n\r\nCount_Get with StreamKind=Stream_Audio\r\n";
                //text += mediaInfo.CountGet(StreamKind.Audio);

                //text += "\r\n\r\nGet with Stream=General and Parameter='AudioCount'\r\n";
                //text += mediaInfo.Get(StreamKind.General, 0, "AudioCount");

                //text += "\r\n\r\nGet with Stream=Audio and Parameter='StreamCount'\r\n";
                //text += mediaInfo.Get(StreamKind.Audio, 0, "StreamCount");
            }

            Console.WriteLine(text);

        }

        private static void ExifInfo(Stream stream)
        {
            // Instantiate the reader
            using (ExifReader reader = new ExifReader(stream))
            {
                // Extract the tag data using the ExifTags enumeration
                DateTime datePictureTaken;
                if (reader.GetTagValue<DateTime>(ExifTags.DateTimeDigitized,
                                                out datePictureTaken))
                {
                    //// Do whatever is required with the extracted information
                    //MessageBox.Show(this, string.Format("The picture was taken on {0}",
                    //   datePictureTaken), "Image information", MessageBoxButtons.OK);
                    Console.WriteLine(datePictureTaken);
                }
            }
        }
        public static DateTime ExifDate(Stream stream)
        {
            using (ExifReader reader = new ExifReader(stream))
            {
                // Extract the tag data using the ExifTags enumeration
                DateTime datePictureTaken = System.DateTime.Now;
                reader.GetTagValue<DateTime>(ExifTags.DateTimeDigitized, out datePictureTaken);
                return datePictureTaken;
            }
        }
        public static DateTime ExifDate(string fname)
        {
            using (ExifReader reader = new ExifReader(fname))
            {
                // Extract the tag data using the ExifTags enumeration
                DateTime datePictureTaken = System.DateTime.Now;
                reader.GetTagValue<DateTime>(ExifTags.DateTimeDigitized, out datePictureTaken);
                return datePictureTaken;
            }
        }
    }
}
