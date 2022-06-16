using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace Fogid.Cassettes
{
    class ImageLab
    {
        static public void TransformSave(BitmapSource bi, double scale, int quality, string filename)
        {
            var tr = new ScaleTransform(scale, scale);
            TransformedBitmap tb = new TransformedBitmap(bi, tr);
            //if (File.Exists(filename)) File.Delete(filename);
            var stream = new FileStream(filename, FileMode.OpenOrCreate, FileAccess.Write);
            JpegBitmapEncoder encoder = new System.Windows.Media.Imaging.JpegBitmapEncoder();
            encoder.QualityLevel = quality;
            encoder.Frames.Add(BitmapFrame.Create(tb));
            encoder.Save(stream);
            stream.Close();
        }

    }
}
