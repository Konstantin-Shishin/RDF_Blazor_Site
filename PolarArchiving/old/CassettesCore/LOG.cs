using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Xml.Linq;

namespace Polar.Cassettes
{
  public class LOG
    {
        public static string fileName;
        public static string DataBackupfileName;
        public static object locker = new object();
        public static void WriteBackUp(XElement text)
        {
            lock (locker)
            {
            //    var name = DataBackupfolderName + DateTime.Now.ToString().Replace(":", " ") + ".txt";
                try
                {
                    var  saver =// new StreamWriter(
                    XElement.Load(DataBackupfileName);
                    saver.Add(text);
                    saver.Save(DataBackupfileName);
                //  new StreamWriter(DataBackupfolderName, true, Encoding.UTF8);

                    //saver.WriteLine(text);
                    //saver.Close();
                }
                catch (Exception ex)
                {
                    WriteLine("Ошибка записи в backup  "+ex.Message);
                }
            }
        }
        public static void WriteLine(string text)
        {
            lock (locker)
            {
                try
                {              
                var saver = new StreamWriter(fileName, true, Encoding.UTF8);
                saver.WriteLine(DateTime.Now.ToString("s"));               
                saver.WriteLine(text);
                saver.WriteLine();
                saver.Close();
                }
                catch (Exception)
                {              
                }
            }
        }
        private static DateTime tt0;
        public static void StartTimer() { tt0 = DateTime.Now; }
        public static TimeSpan LookTimer()
        {
            return DateTime.Now - tt0;
        }
    }
}
