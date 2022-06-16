using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ConvertVideo
{
    public class Program
    {
        public static void Main(string[] args)
        {
            string path = @"C:\Home\syp\syp2018\toProcess\";
            //string ext = ".mp4";
            //string ext = ".avi";
            string ext = ".mts";
            //string ext = ".mpg";
            System.IO.DirectoryInfo di = new DirectoryInfo(path);
            FileInfo[] files = di.GetFiles("*" + ext);
            using (StreamWriter bat = new StreamWriter(new FileStream(path + "do.bat", FileMode.Create, FileAccess.Write)))
            {
                foreach (FileInfo file in files)
                {
                    //DateTime created = file.CreationTime;
                    //string filenamenew = created.ToString("yyyyMMdd");
                    DateTime modified = file.LastWriteTime;
                    string filenamenew = modified.ToString("yyyyMMdd");
                    string ss = file.Name;
                    filenamenew = filenamenew + "_" + ss.Substring(0, ss.Length - ext.Length);

                    Console.WriteLine("{0}", filenamenew);
                    //bat.WriteLine("ffmpeg -i {0} -y -vcodec libx264 -b 4050K -ar 22050 -s 1440x1080 {1}.mp4", file.Name, filenamenew);
                    bat.WriteLine("C:/Home/bin/ffmpeg -i {0} -y -vcodec libx264 -b 2020K {1}.mp4", file.Name, filenamenew);
                }
            }
        }
    }
}
