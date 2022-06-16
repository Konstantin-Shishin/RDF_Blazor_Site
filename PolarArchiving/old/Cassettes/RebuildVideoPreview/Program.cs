using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace RebuildVideoPreview
{
    public class Program
    {
        /// <summary>
        /// Первый аргумент: директория кассеты
        /// </summary>
        /// <param name="args"></param>
        public static void Main(string[] args)
        {
            Console.WriteLine("Start RebuildVideo");
            DirectoryInfo cass_di = new DirectoryInfo(@"D:\home\FactographProjects\LenaCA2010\");
            string App_Bin_Path = (new DirectoryInfo("../../../App_Bin/")).FullName;//"../../../App_Bin/";
            StreamWriter bat_writer = new StreamWriter(new FileStream(cass_di.FullName + "\\video_preview.bat", FileMode.Create, FileAccess.Write));
            // Цикл по директориям и файлам оригинала
            foreach (DirectoryInfo orig_di in cass_di.EnumerateDirectories("originals/*"))
            {
                string dnum = orig_di.Name;
                Console.WriteLine(dnum);
                // Проверим на валидность
                int num = -1;
                if (!Int32.TryParse(dnum, out num)) continue;
                // Цикл по файлам в директории
                foreach (FileInfo fi in orig_di.EnumerateFiles())
                {
                    // Оставим только видео
                    string ext = fi.Extension;
                    string point_ext = ext.ToLower();
                    var doct = Fogid.Cassettes.Cassette.docTypes
                        .Where(dt => dt.ext == point_ext)
                        .Select(dt => new Tuple<Fogid.Cassettes.DocType>(dt))
                        .FirstOrDefault();
                    if (doct == null) continue;
                    if (doct.Item1.tag != Fogid.Cassettes.ONames.TagVideo) continue;
                    Console.Write(" {0}", fi.Name);
                    //Console.WriteLine(doct);

                    string cassetteDirFullName = cass_di.FullName;
                    string cassette_folderNumber = dnum;
                    string cassette_documentNumber = fi.Name.Substring(0, fi.Name.Length - ext.Length);

                    string o_aspect_s = "4x3";

                    try
                    {
                        var proc = new System.Diagnostics.Process();
                        // Redirect the output stream of the child process.
                        proc.StartInfo.UseShellExecute = false;
                        proc.StartInfo.RedirectStandardOutput = true;
                        proc.StartInfo.FileName = App_Bin_Path + "MediaInfo.exe";
                        proc.StartInfo.Arguments = " --Output=XML " +
                            cassetteDirFullName + "/originals/" + cassette_folderNumber + "/" +
                            cassette_documentNumber + ext;
                        proc.Start();
                        // Do not wait for the child process to exit before
                        // reading to the end of its redirected stream.
                        // proc.WaitForExit();
                        // Read the output stream first and then wait.
                        string output = proc.StandardOutput.ReadToEnd();
                        proc.WaitForExit();
                        var xoutput = XElement.Parse(output).Element("File");

                        string o_width_s = xoutput.Elements("track").First(tr => tr.Attribute("type").Value == "Video").Element("Width").Value;
                        string o_height_s = xoutput.Elements("track").First(tr => tr.Attribute("type").Value == "Video").Element("Height").Value;
                        int o_width = Int32.Parse(new string(o_width_s.Where(c => c >= '0' && c <= '9').ToArray()));
                        int o_height = Int32.Parse(new string(o_height_s.Where(c => c >= '0' && c <= '9').ToArray()));
                        // Display_aspect_ratio
                        o_aspect_s = xoutput.Elements("track").First(tr => tr.Attribute("type").Value == "Video").Element("Display_aspect_ratio").Value;
                    }
                    catch (Exception) { }

                    string oext = ".webm";
                    string executeLine =
                        "-i \"" + cassetteDirFullName + "/originals/" + cassette_folderNumber + "/" +
                        cassette_documentNumber + ext +
                        "\" -y -ar 22050 -b:v 450k -r 10 " + 
                        " -s " + (o_aspect_s == "4x3" ? "400x300" : "512x288") + " ";
                    string codecs = "-vcodec libvpx -acodec libvorbis ";
                    string outfile = cassetteDirFullName + "/documents/medium/" + cassette_folderNumber + "/" +
                        cassette_documentNumber;
                    if (!File.Exists(outfile + oext)) bat_writer.WriteLine(App_Bin_Path + "ffmpeg.exe " + executeLine + codecs + outfile + oext);
                    codecs = "-vcodec libx264 ";
                    oext = ".mp4";
                    if (!File.Exists(outfile + oext)) bat_writer.WriteLine(App_Bin_Path + "ffmpeg.exe " + executeLine + codecs + outfile + oext);

                }
                Console.WriteLine();
            }
            bat_writer.Close();
        }
    }
}
