using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
//using System.Windows.Media.Imaging;
using System.Xml.Linq;
using System.IO;
//using Microsoft.DeepZoomTools;

namespace Polar.Cassettes
{
    public static class CassetteExtension
    {
        // Методы выдают дефолтные значения, если есто для идентификаторов архива и источника
        internal static string DefaultArchiveId(this Cassette cassette)
        {
            XElement archEl = cassette.finfo.Element("archive");
            if(archEl == null) return null;
            XAttribute archIdatt = archEl.Attribute("id");
            if(archIdatt == null) return null;
            return archIdatt.Value;
        }
        internal static string DefaultSourceId(this Cassette cassette)
        {
            XElement archEl = cassette.finfo.Element("archive");
            if(archEl == null) return null;
            XAttribute sourceIdatt = archEl.Attribute("source");
            if(sourceIdatt == null) return null;
            return sourceIdatt.Value;
        }
        // Path для используемых дополнительных программ
        public static string App_Bin_Path = "";

        //public CassetteExtension(string path) : base(path)  {  }
        //public CassetteExtension(DirectoryInfo dir, string cassName, string fn, string dn, XElement db) :
        //    base(dir, cassName, fn, dn, db) { }

        /// <summary>
        /// Вычисляет превьюшки для фоток. Режим - набор (подмножество) букв "smn". Возвращает Uri оригинала.
        /// </summary>
        /// <param name="iisstore"></param>
        /// <param name="regimes"></param>
        /// <returns></returns>
        public static Uri MakePhotoPreviews(this Cassette cassette, XElement iisstore, string regimes)
        {
            string ur = iisstore.Attribute(ONames.AttUri).Value;
            string original = iisstore.Attribute(ONames.AttOriginalname).Value;
            string filePath = ur.Substring(ur.Length - 9);
            string imageSource = cassette.Dir.FullName + "/originals/" + filePath + original.Substring(original.LastIndexOf('.'));

            Uri _source = null;
            try
            {
                _source = new Uri(imageSource);
            }
            catch(Exception exc)
            {
                Polar.Cassettes.LOG.WriteLine(exc.Message + " Не загрузился файл " + iisstore.ToString());
                return null;
            }

            //BitmapImage bi = new BitmapImage(_source);
            //double width = (double)bi.PixelWidth;
            //double height = (double)bi.PixelHeight;
            //// добавляем информацию в iisstore, вычисляя width и height
            //XAttribute att_width = iisstore.Attribute("width");
            //if(att_width == null) iisstore.Add(new XAttribute("width", width.ToString()));
            //else att_width.Value = width.ToString();
            //XAttribute att_height = iisstore.Attribute("height");
            //if(att_height == null) iisstore.Add(new XAttribute("height", height.ToString()));
            //else att_height.Value = height.ToString();
            //// Трансформации
            //var transformAtt = iisstore.Attribute("transform");
            //if(transformAtt == null) { transformAtt = new XAttribute("transform", ""); iisstore.Add(transformAtt); }
            //int nrotations = transformAtt.Value.Length;
            //BitmapSource bs = new TransformedBitmap(bi, new System.Windows.Media.RotateTransform(90 * nrotations));

            //double dim = (width > height ? width : height);
            //TODO:
            //if(regimes.Contains('s'))
            //    ImageLab.TransformSave(bs, double.Parse(cassette.GetPreviewParameter(iisstore, "small", "previewBase")) / dim,
            //        int.Parse(cassette.GetPreviewParameter(iisstore, "small", "qualityLevel")),
            //        cassette.Dir.FullName + "/documents/small/" + filePath + ".jpg");
            //if(regimes.Contains('m'))
            //    ImageLab.TransformSave(bs, double.Parse(cassette.GetPreviewParameter(iisstore, "medium", "previewBase")) / dim,
            //        int.Parse(cassette.GetPreviewParameter(iisstore, "medium", "qualityLevel")),
            //        cassette.Dir.FullName + "/documents/medium/" + filePath + ".jpg");
            //if(regimes.Contains('n'))
            //    ImageLab.TransformSave(bs, double.Parse(cassette.GetPreviewParameter(iisstore, "normal", "previewBase")) / dim,
            //        int.Parse(cassette.GetPreviewParameter(iisstore, "normal", "qualityLevel")),
            //        cassette.Dir.FullName + "/documents/normal/" + filePath + ".jpg");
            //if(regimes.Contains('z'))
            //{
            //    ImageCreator icreator = new ImageCreator { ImageQuality = 0.8 };
            //    if(!Directory.Exists(cassette.Dir.FullName + "/documents/deepzoom"))
            //        Directory.CreateDirectory(cassette.Dir.FullName + "/documents/deepzoom");
            //    if(!Directory.Exists(cassette.Dir.FullName + "/documents/deepzoom/" + filePath.Substring(0, 4)))
            //        Directory.CreateDirectory(cassette.Dir.FullName + "/documents/deepzoom/" + filePath.Substring(0, 4));
            //    var destinationPath = cassette.Dir.FullName + "/documents/deepzoom/" + filePath;
            //    icreator.Create(imageSource, destinationPath + ".xml");
            //    // тестирование
            //    Sarc.Create(destinationPath + ".sarc2", new FileSystemInfo[] 
            //    {
            //        new FileInfo(cassette.Dir.FullName + "/documents/deepzoom/" + filePath + ".xml"),
            //        new DirectoryInfo(cassette.Dir.FullName + "/documents/deepzoom/" + filePath + "_files")
            //    });
            //    File.Delete(cassette.Dir.FullName + "/documents/deepzoom/" + filePath + ".xml");
            //    Directory.Delete(cassette.Dir.FullName + "/documents/deepzoom/" + filePath + "_files", true);
            //}
            return _source;
        }
        public static void RebuildPhotoPreviews(this Cassette cassette)
        {
            var qu = cassette.db.Elements(ONames.TagPhotodoc);
            foreach(XElement pdoc in qu)
            {
                cassette.MakePhotoPreviews(pdoc.Element(ONames.TagIisstore), "smn");
            }
        }
        public static string RebuildPhotoPreviewsAsync(this Cassette cassette, System.ComponentModel.BackgroundWorker worker)
        {
            var qu = cassette.db.Elements(ONames.TagPhotodoc);
            int num = qu.Count();
            int cnt = 0;
            foreach(XElement pdoc in qu)
            {
                if(worker.CancellationPending) { return "cancelled"; }
                cassette.MakePhotoPreviews(pdoc.Element(ONames.TagIisstore), "smn");
                cnt++;
                worker.ReportProgress(100 * cnt / num);
            }
            return "ok";
        }        //public event EventHandler NeedToCalculateVideoPreview = null;
        public static void RebuildVideoPreviews(this Cassette cassette)
        {
            var qu = cassette.db.Elements(ONames.TagVideo);
            foreach(XElement pdoc in qu)
            {
                cassette.MakeVideoPreview(pdoc.Element(ONames.TagIisstore));
            }
        }
        //private static Rotation[] rotations = new Rotation[] { Rotation.Rotate0, Rotation.Rotate90, Rotation.Rotate180, Rotation.Rotate270 };

        /// <summary>
        /// Дабавляет файл, возвращает добавленные в базу данных элементы
        /// </summary>
        /// <param name="file"></param>
        /// <param name="collectionId"></param>
        /// <returns></returns>
        public static IEnumerable<XElement> AddFile(this Cassette cassette, FileInfo file, string collectionId)
        {
            // Эта реализация используется (только) в CManager'е
            List<XElement> addedElements = new List<XElement>(); // string smallImageFullName = null;
            string fname = file.FullName;
            XElement doc = null;
            // Первая задача - выделить тождественный документ, он помещается в doc
            // Проверка на существование документа в архиве, удовлетворяющего условиям: размер совпадает,
            // расширитель совпадает и байтовая последовательность также совпадает
            string fileSize = file.Length.ToString();
            string fileExt = Path.GetExtension(fname).ToLower();//fname.Substring(fname.LastIndexOf('.')).ToLower(); // расширение вместе с точкой
            doc =
                cassette.db.Elements()
                    .FirstOrDefault(
                        (XElement e) =>
                        {
                            XElement iisstore = e.Element(ONames.TagIisstore);
                            if(iisstore == null) return false;
                            XAttribute on = iisstore.Attribute(ONames.AttOriginalname);
                            XAttribute sz = iisstore.Attribute(ONames.AttSize);
                            if(on == null || sz == null) return false;
                            string uri = iisstore.Attribute(ONames.AttUri).Value;
                            if(!on.Value.ToLower().EndsWith(fileExt) || sz.Value != fileSize) return false;
                            if(File.ReadAllBytes(fname)
                                .SequenceEqual(
                                    File.ReadAllBytes(cassette.Dir.FullName + "/" + cassette.GetOriginalDocumentPath(uri)))) return true;
                            return false;
                        });
            bool docIsNew = (doc == null);
            if(docIsNew)
            {
                // Включение файла в архив
                string pure_fname = fname.Split(Cassette.slashes).Last();
                //int p = pure_fname.LastIndexOf('.');
                string ext = Path.GetExtension(pure_fname).ToLower();//(p != -1 ? pure_fname.Substring(p).ToLower() : "");
                // копируем оригинал под имеющимся расширением 
                var filepath = cassette.Dir.FullName + "/originals/" + cassette._folderNumber + "/" + cassette._documentNumber + ext;
                try
                {
                    File.Copy(fname, filepath);
                }
                catch(Exception exc)
                {
                    LOG.WriteLine("ошибка добавления документа, не послучилось скопировать  в " + filepath+
                    Environment.NewLine+
                    exc.Message+
                    Environment.NewLine+
                    exc.StackTrace
                        );
                    return null;
                } // Надо бы эту нештатную ситуацию зафиксировать в каком-нибудь логе

                // Создаем запись и привязываем ее к коллекции
                DocType triple = Cassette.docTypes.Where(doct => doct.ext == ext || doct.ext == "unknown").First();
                var ex = triple.ext;
                var documenttype = triple.content_type;
                var docTag = triple.tag;

                XName docId = XName.Get(cassette.Name + "_" + cassette._folderNumber + "_" + cassette._documentNumber);
                var iisstore = new XElement(ONames.TagIisstore,
                            new XAttribute(ONames.AttUri, "iiss://" + cassette.Name + "@iis.nsk.su/0001/" + cassette._folderNumber + "/" + cassette._documentNumber),
                            new XAttribute(ONames.AttOriginalname, fname),
                            new XAttribute(ONames.AttSize, file.Length.ToString()),
                            new XAttribute(ONames.AttDocumenttype, documenttype),
                            new XAttribute(ONames.AttDocumentcreation, file.CreationTimeUtc.ToString("s")),
                            new XAttribute(ONames.AttDocumentfirstuploaded, System.DateTime.Now.ToString("s")),
                            new XAttribute(ONames.AttDocumentmodification, file.LastWriteTimeUtc.ToString("s")),
                            new XAttribute(ONames.AttChecksum, "qwerty"));
                //string documentname = cassette.Name + " " + cassette._folderNumber + " " + cassette._documentNumber;
                //TODO: поменял концепцию формирования имени документа
                int lastpoint = fname.LastIndexOf('.');
                int afterlastslash = Math.Max(fname.LastIndexOf('/'), fname.LastIndexOf('\\')) + 1;
                string documentname = cassette.DocNamePrefix +
                    fname.Substring(afterlastslash, lastpoint > afterlastslash ? lastpoint - afterlastslash : fname.Length - afterlastslash);
                doc = new XElement(docTag,
                        new XAttribute(ONames.rdfabout, docId),
                        new XElement(ONames.TagName, documentname),
                        new XElement(ONames.TagComment, fname),
                        iisstore);
                // Дополнительная обработка для фотографий
                if(docTag == ONames.TagPhotodoc)
                {
                    using (var stream = new System.IO.FileStream(file.FullName, System.IO.FileMode.Open, System.IO.FileAccess.Read))
                    using (var original = FreeImageAPI.FreeImageBitmap.FromStream(stream))
                    {
                        stream.Position = 0L;
                        DateTime datePictureTaken = DateTime.Now;
                        bool exifok = false;
                        // Использую ExifLib.Standard
                        if (ext == ".jpg")
                        {
                        }
                        try
                        {
                            using (ExifLib.ExifReader reader = new ExifLib.ExifReader(stream))
                            {
                                // Extract the tag data using the ExifTags enumeration
                                if (reader.GetTagValue<DateTime>(ExifLib.ExifTags.DateTimeDigitized,
                                                                out datePictureTaken))
                                {
                                    exifok = true;
                                }
                            }
                        }
                        catch (Exception)
                        {
                            Console.WriteLine("Cant make EXIF object");
                        }


                        var fi = file;
                        var md = fi.CreationTimeUtc;
                        //original.Save(fpath);
                        Console.WriteLine($"Width={original.Width} Height={original.Height} ImageFormat={original.ImageFormat} {datePictureTaken} ");
                        
                        string dirpath = cassette.Dir.FullName + "/";
                        string foldernumb = cassette._folderNumber;
                        string docnumb = cassette._documentNumber;
                        int ns = 150, nm = 600, nl = 1200;
                        Int32.TryParse(cassette.finfo.Element("image")?.Element("small")?.Attribute("previewBase")?.Value, out ns);
                        Int32.TryParse(cassette.finfo.Element("image")?.Element("medium")?.Attribute("previewBase")?.Value, out nm);
                        Int32.TryParse(cassette.finfo.Element("image")?.Element("normal")?.Attribute("previewBase")?.Value, out nl);
                        Sizing(original, ns, dirpath + "documents/small/" + foldernumb + "/" + docnumb + ".jpg");
                        Sizing(original, nm, dirpath + "documents/medium/" + foldernumb + "/" + docnumb + ".jpg");
                        Sizing(original, nl, dirpath + "documents/normal/" + foldernumb + "/" + docnumb + ".jpg");
                    }
                }
                // Дополнительная обработка для видео
                if (docTag == ONames.TagVideo)
                {
                    int o_width = 640; // Это значения "от фонаря"
                    int o_height = 480;
                    // Сначала, почитаем метаинформацию. Используется программа MediaInfo
                    string output="";
                    try
                    {
                        var proc = new System.Diagnostics.Process();
                        // Redirect the output stream of the child process.
                        proc.StartInfo.UseShellExecute = false;
                        proc.StartInfo.RedirectStandardOutput = true;
                        proc.StartInfo.FileName = App_Bin_Path + "MediaInfo.exe";
                        proc.StartInfo.Arguments = " --Output=XML " +
                            cassette.Dir.FullName + "/originals/"+ cassette._folderNumber + "/" + cassette._documentNumber + ext;
                        proc.Start();
                        // Do not wait for the child process to exit before
                        // reading to the end of its redirected stream.
                        // proc.WaitForExit();
                        // Read the output stream first and then wait.
                        output = proc.StandardOutput.ReadToEnd();
                        proc.WaitForExit();
                        var xoutput = XElement.Parse(output).Element("File");
                        string o_width_s = xoutput.Elements("track").First(tr => tr.Attribute("type").Value == "Video").Element("Width").Value;
                        string o_height_s = xoutput.Elements("track").First(tr => tr.Attribute("type").Value == "Video").Element("Height").Value;
                        o_width = Int32.Parse(new string(o_width_s.Where(c => c >= '0' && c <= '9').ToArray()));
                        o_height = Int32.Parse(new string(o_height_s.Where(c => c >= '0' && c <= '9').ToArray()));
                        // Display_aspect_ratio
                        string o_aspect_s = xoutput.Elements("track").First(tr => tr.Attribute("type").Value == "Video").Element("Display_aspect_ratio").Value;
                        iisstore.Add(
                            new XAttribute("width", "" + o_width),
                            new XAttribute("height", "" + o_height),
                            new XAttribute("duration",
                                xoutput.Elements("track").First(tr => tr.Attribute("type").Value == "Video").Element("Duration").Value),
                            new XAttribute("framerate",
                                xoutput.Elements("track").First(tr => tr.Attribute("type").Value == "Video").Element("Frame_rate").Value),
                            o_aspect_s == "4:3" || o_aspect_s == "16:9" ? 
                            new XAttribute("aspect", o_aspect_s) : null,
                            null);
                    }
                    catch(Exception exc)
                    {
                        LOG.WriteLine(" ошибка добавления документа, не получилось обработать видео " + output +
                        Environment.NewLine +
                        exc.Message +
                        Environment.NewLine +
                        exc.StackTrace
                        );
                    }

                    string sz = cassette.GetPreviewParameter(iisstore, "medium", "framesize");
                    string[] w_h = sz.Split(new char[] { 'x' });
                    int m_width = Int32.Parse(w_h[0]);
                    int m_height = Int32.Parse(w_h[1]);
                    int m_width_corrected = m_height * o_width / o_height;
                    // Если кадр меньше запланированного, оставляем "родной" размер
                    string output_size = m_height < o_height ? m_width_corrected + "x" + m_height : o_width + "x" + o_height;
                    // Зафиксируем размер в iisstore
                    if(output_size != cassette.finfo.Element("video").Element("medium").Attribute("framesize").Value)
                    {
                        var framesize_att = iisstore.Attribute("video.medium.framesize");
                        if(framesize_att == null)
                        {
                            iisstore.Add(new XAttribute("video.medium.framesize", "" + output_size));
                        }
                        else
                        {
                            framesize_att.Value = "" + output_size;
                        }
                    }
                    // Сформируем команду на вычисление превьюшки 
                    cassette.MakeVideoPreview(iisstore);
                    // Зафиксируем дату съемки по LastWriteTime
                    DateTime dt =
                        cassette.Dir.GetFiles("originals/" + cassette._folderNumber + "/" + cassette._documentNumber + ext)[0].LastWriteTime;
                    doc.Add(new XElement(ONames.TagFromdate, dt.ToString("s")));
                }
                // дополнительная обработка аудио
                if (docTag == ONames.TagAudio)
                {
                    cassette.MakeAudioPreview(iisstore);
                }

                cassette.IncrementDocumentNumber();
                addedElements.Add(doc);
                cassette.db.Add(doc);
                // попытка добавить принадлежность к архиву
                XElement archMember = cassette.CreateArchiveMember(docId.ToString());
                if(archMember != null)
                {
                    addedElements.Add(archMember);
                    cassette.db.Add(archMember);
                }
            }
            else { doc = XElement.Parse(doc.ToString()); doc.Add(new XAttribute("itIsCopy", "true")); addedElements.Add(doc); }
            string dId = doc.Attribute(ONames.rdfabout).Value;
            // Проверим наличие связи
            bool membershipExists = 
            (!docIsNew) &&
           
                //var collmem = 
                    cassette.db.Elements(ONames.TagCollectionmember)
                    .Any(cm =>
                        cm.Element(ONames.TagCollectionitem).Attribute(ONames.rdfresource).Value == dId &&
                        cm.Element(ONames.TagIncollection).Attribute(ONames.rdfresource).Value == collectionId);
            //  if (collmem != null) membershipExists = true;

            // А теперь, наконец, добавим членство в коллекции
            if(!membershipExists)
            {
                XElement collectionmember =
                    new XElement(ONames.TagCollectionmember,
                        new XAttribute(ONames.rdfabout, cassette.Name + "_" + collectionId + "_" + dId),
                        new XElement(ONames.TagIncollection, new XAttribute(ONames.rdfresource, collectionId)),
                        new XElement(ONames.TagCollectionitem, new XAttribute(ONames.rdfresource, dId)));
                addedElements.Add(collectionmember);
                cassette.db.Add(collectionmember);
            }
            return addedElements;
        }





        /// <summary>
        /// Добавляет файл, возвращает добавленные в базу данных элементы ============ Новый вариант =============
        /// Если файл уже был, что проверяется, возвращает пустую последовательность.
        /// </summary>
        /// <param name="file"></param>
        /// <param name="collectionId"></param>
        /// <returns></returns>
        public static IEnumerable<XElement> AddFile1(this Cassette cassette, FileInfo file, string collectionId)
        {
            // А это - используется в Web-приложениях под Core!!!
            List<XElement> addedElements = new List<XElement>(); // string smallImageFullName = null;
            string fname = file.FullName;
            XElement doc = null;
            // Первая задача - выделить тождественный документ, он помещается в doc
            // Проверка на существование документа в архиве, удовлетворяющего условиям: размер совпадает,
            // расширитель совпадает и байтовая последовательность также совпадает
            string fileSize = file.Length.ToString();
            string fileExt = Path.GetExtension(fname).ToLower();//fname.Substring(fname.LastIndexOf('.')).ToLower(); // расширение вместе с точкой
            doc =
                cassette.db.Elements()
                    .FirstOrDefault(
                        (XElement e) =>
                        {
                            string ssize = null;
                            string uri = null;
                            string originalname = null;
                            XElement iisstore = null;
                            XElement docmetainfo = null;
                            iisstore = e.Element(ONames.TagIisstore);
                            if (iisstore == null) docmetainfo = e.Element("{http://fogid.net/o/}docmetainfo");
                            if (iisstore == null && docmetainfo == null) return false;
                            //XAttribute on = iisstore.Attribute(ONames.AttOriginalname);
                            //XAttribute sz = iisstore.Attribute(ONames.AttSize);
                            //if (on == null || sz == null) return false;
                            //string uri = iisstore.Attribute(ONames.AttUri).Value;
                            //if (!on.Value.ToLower().EndsWith(fileExt) || sz.Value != fileSize) return false;
                            if (iisstore != null)
                            {
                                ssize = iisstore.Attribute(ONames.AttSize)?.Value;
                                uri = iisstore.Attribute(ONames.AttUri)?.Value;
                                originalname = iisstore.Attribute(ONames.AttOriginalname)?.Value;
                            }
                            else
                            {
                                string[] parts = docmetainfo.Value.Split(';');
                                var sizepart = parts 
                                    .FirstOrDefault(p => p.StartsWith("size:"));
                                if (sizepart != null) ssize = sizepart.Substring(5);
                                uri = e.Element("{http://fogid.net/o/}uri")?.Value;
                                string opart = parts
                                    .FirstOrDefault(p => p.StartsWith("originalname:"));
                                if (opart != null) originalname = opart.Substring("originalname:".Length);
                            }
                            //if (ssize == null) return false;
                            if (ssize != fileSize) return false;
                            string[] uriparts = uri.Split('/');
                            // Учитываю только uri данной кассеты
                            string uricassname = uriparts[2].Substring(0, uriparts[2].Length - 11);
                            if (uricassname != cassette.Name) return false;
                            int n = uriparts.Length;
                            string ext = originalname.Split('.').Last();
                            string fileincass = cassette.Dir.FullName +
                                        "/originals/" + uriparts[n-2] + "/" + uriparts[n-1] + "." + ext;
                            if (File.ReadAllBytes(fname)
                                .SequenceEqual(File.ReadAllBytes(fileincass)))
                                return true;
                            return false;
                        });
            bool docIsNew = (doc == null);
            if (docIsNew)
            {
                // Включение файла в архив
                string pure_fname = fname.Split(Cassette.slashes).Last();
                //int p = pure_fname.LastIndexOf('.');
                string ext = Path.GetExtension(pure_fname).ToLower();//(p != -1 ? pure_fname.Substring(p).ToLower() : "");
                // копируем оригинал под имеющимся расширением 
                var filepath = cassette.Dir.FullName + "/originals/" + cassette._folderNumber + "/" + cassette._documentNumber + ext;
                try
                {
                    File.Copy(fname, filepath);
                }
                catch (Exception exc)
                {
                    LOG.WriteLine("ошибка добавления документа, не послучилось скопировать  в " + filepath +
                    Environment.NewLine +
                    exc.Message +
                    Environment.NewLine +
                    exc.StackTrace
                        );
                    return null;
                } // Надо бы эту нештатную ситуацию зафиксировать в каком-нибудь логе

                // Создаем запись и привязываем ее к коллекции
                DocType triple = Cassette.docTypes.Where(doct => doct.ext == ext || doct.ext == "unknown").First();
                var ex = triple.ext;
                var documenttype = triple.content_type;
                var docTag = triple.tag;

                //XName docId = XName.Get(cassette.Name + "_" + cassette._folderNumber + "_" + cassette._documentNumber);
                string docId = cassette.GenerateNewId();// cassette.Name + "_" + cassette._folderNumber + "_" + cassette._documentNumber);
                var iisstore = new XElement(ONames.TagIisstore,
                            new XAttribute(ONames.AttUri, "iiss://" + cassette.Name + "@iis.nsk.su/0001/" + cassette._folderNumber + "/" + cassette._documentNumber),
                            new XAttribute(ONames.AttOriginalname, fname),
                            new XAttribute(ONames.AttSize, file.Length.ToString()),
                            new XAttribute(ONames.AttDocumenttype, documenttype),
                            null);
                DateTime dateDocumentCreated = file.CreationTimeUtc;
                //string documentname = cassette.Name + " " + cassette._folderNumber + " " + cassette._documentNumber;
                //TODO: поменял концепцию формирования имени документа
                int lastpoint = fname.LastIndexOf('.');
                int afterlastslash = Math.Max(fname.LastIndexOf('/'), fname.LastIndexOf('\\')) + 1;
                string documentname = cassette.DocNamePrefix +
                    fname.Substring(afterlastslash, lastpoint > afterlastslash ? lastpoint - afterlastslash : fname.Length - afterlastslash);
                doc = new XElement(docTag,
                        new XAttribute(ONames.rdfabout, docId),
                        new XElement(ONames.TagName, documentname),
                        new XElement(ONames.TagComment, fname),
                        iisstore);
                // Дополнительная обработка для фотографий
                if (docTag == ONames.TagPhotodoc)
                {
                    using (var stream = new System.IO.FileStream(file.FullName, System.IO.FileMode.Open, System.IO.FileAccess.Read))
                    using (var original = FreeImageAPI.FreeImageBitmap.FromStream(stream))
                    {
                        stream.Position = 0L;
                        // Использую ExifLib.Standard
                        try
                        {
                            using (ExifLib.ExifReader reader = new ExifLib.ExifReader(stream))
                            {
                                // Extract the tag data using the ExifTags enumeration
                                if (reader.GetTagValue<DateTime>(ExifLib.ExifTags.DateTimeDigitized, out dateDocumentCreated)) {  }
                            }
                        }
                        catch (Exception) { Console.WriteLine("Can't create EXIF!!!!!"); }

                        var fi = file;
                        Console.WriteLine($"Width={original.Width} Height={original.Height} ImageFormat={original.ImageFormat} {dateDocumentCreated} ");
                        iisstore.Add(
                            new XAttribute("width", "" + original.Width),
                            new XAttribute("height", "" + original.Height),
                            null);


                        string dirpath = cassette.Dir.FullName + "/";
                        string foldernumb = cassette._folderNumber;
                        string docnumb = cassette._documentNumber;
                        int ns = 150, nm = 600, nl = 1200;
                        Int32.TryParse(cassette.finfo.Element("image")?.Element("small")?.Attribute("previewBase")?.Value, out ns);
                        Int32.TryParse(cassette.finfo.Element("image")?.Element("medium")?.Attribute("previewBase")?.Value, out nm);
                        Int32.TryParse(cassette.finfo.Element("image")?.Element("normal")?.Attribute("previewBase")?.Value, out nl);
                        Sizing(original, ns, dirpath + "documents/small/" + foldernumb + "/" + docnumb + ".jpg");
                        Sizing(original, nm, dirpath + "documents/medium/" + foldernumb + "/" + docnumb + ".jpg");
                        Sizing(original, nl, dirpath + "documents/normal/" + foldernumb + "/" + docnumb + ".jpg");
                    }
                }
                // Дополнительная обработка для видео
                if (docTag == ONames.TagVideo)
                {
                    int o_width = 640; // Это значения "от фонаря"
                    int o_height = 480;
                    // Сначала, почитаем метаинформацию. Используется программа MediaInfo
                    string output = "";
                    try
                    {
                        var proc = new System.Diagnostics.Process();
                        // Redirect the output stream of the child process.
                        proc.StartInfo.UseShellExecute = false;
                        proc.StartInfo.RedirectStandardOutput = true;
                        proc.StartInfo.FileName = App_Bin_Path + "MediaInfo.exe";
                        proc.StartInfo.Arguments = " --Output=XML " +
                            cassette.Dir.FullName + "/originals/" + cassette._folderNumber + "/" + cassette._documentNumber + ext;
                        proc.Start();
                        // Do not wait for the child process to exit before
                        // reading to the end of its redirected stream.
                        // proc.WaitForExit();
                        // Read the output stream first and then wait.
                        output = proc.StandardOutput.ReadToEnd();
                        proc.WaitForExit();
                        var xoutput = XElement.Parse(output).Element("File");
                        string o_width_s = xoutput.Elements("track").First(tr => tr.Attribute("type").Value == "Video").Element("Width").Value;
                        string o_height_s = xoutput.Elements("track").First(tr => tr.Attribute("type").Value == "Video").Element("Height").Value;
                        o_width = Int32.Parse(new string(o_width_s.Where(c => c >= '0' && c <= '9').ToArray()));
                        o_height = Int32.Parse(new string(o_height_s.Where(c => c >= '0' && c <= '9').ToArray()));
                        // Display_aspect_ratio
                        string o_aspect_s = xoutput.Elements("track").First(tr => tr.Attribute("type").Value == "Video").Element("Display_aspect_ratio").Value;
                        iisstore.Add(
                            new XAttribute("width", "" + o_width),
                            new XAttribute("height", "" + o_height),
                            new XAttribute("duration",
                                xoutput.Elements("track").First(tr => tr.Attribute("type").Value == "Video").Element("Duration").Value),
                            new XAttribute("framerate",
                                xoutput.Elements("track").First(tr => tr.Attribute("type").Value == "Video").Element("Frame_rate").Value),
                            o_aspect_s == "4:3" || o_aspect_s == "16:9" ?
                            new XAttribute("aspect", o_aspect_s) : null,
                            null);
                    }
                    catch (Exception exc)
                    {
                        LOG.WriteLine(" ошибка добавления документа, не получилось обработать видео " + output +
                        Environment.NewLine +
                        exc.Message +
                        Environment.NewLine +
                        exc.StackTrace
                        );
                    }

                    string sz = cassette.GetPreviewParameter(iisstore, "medium", "framesize");
                    string[] w_h = sz.Split(new char[] { 'x' });
                    int m_width = Int32.Parse(w_h[0]);
                    int m_height = Int32.Parse(w_h[1]);
                    int m_width_corrected = m_height * o_width / o_height;
                    // Если кадр меньше запланированного, оставляем "родной" размер
                    string output_size = m_height < o_height ? m_width_corrected + "x" + m_height : o_width + "x" + o_height;
                    // Зафиксируем размер в iisstore
                    if (output_size != cassette.finfo.Element("video").Element("medium").Attribute("framesize").Value)
                    {
                        var framesize_att = iisstore.Attribute("video.medium.framesize");
                        if (framesize_att == null)
                        {
                            iisstore.Add(new XAttribute("video.medium.framesize", "" + output_size));
                        }
                        else
                        {
                            framesize_att.Value = "" + output_size;
                        }
                    }
                    // Сформируем команду на вычисление превьюшки 
                    cassette.MakeVideoPreview(iisstore);
                    // Зафиксируем дату съемки по LastWriteTime
                    DateTime dt =
                        cassette.Dir.GetFiles("originals/" + cassette._folderNumber + "/" + cassette._documentNumber + ext)[0].LastWriteTime;
                    doc.Add(new XElement(ONames.TagFromdate, dt.ToString("s")));
                }
                // дополнительная обработка аудио
                if (docTag == ONames.TagAudio)
                {
                    cassette.MakeAudioPreview(iisstore);
                }
                iisstore.Add(
                    new XAttribute(ONames.AttDocumentcreation, dateDocumentCreated.ToString("s")),
                    null);

                cassette.IncrementDocumentNumber();
                addedElements.Add(doc);
                //cassette.db.Add(doc);
                // попытка добавить принадлежность к архиву
                XElement archMember = cassette.CreateArchiveMember(docId.ToString());
                if (archMember != null)
                {
                    addedElements.Add(archMember);
                    //cassette.db.Add(archMember);
                }
                // А теперь, наконец, добавим членство в коллекции
                XElement collectionmember =
                    new XElement(ONames.TagCollectionmember,
                        new XAttribute(ONames.rdfabout, cassette.GenerateNewId()),
                        new XElement(ONames.TagIncollection, new XAttribute(ONames.rdfresource, collectionId)),
                        new XElement(ONames.TagCollectionitem, new XAttribute(ONames.rdfresource, docId)));
                addedElements.Add(collectionmember);
            }
            //else { doc = XElement.Parse(doc.ToString()); doc.Add(new XAttribute("itIsCopy", "true")); addedElements.Add(doc); }
            //string dId = doc.Attribute(ONames.rdfabout).Value;
            //// Проверим наличие связи
            //bool membershipExists =
            //(!docIsNew) &&

            //        //var collmem = 
            //        cassette.db.Elements(ONames.TagCollectionmember)
            //        .Any(cm =>
            //            cm.Element(ONames.TagCollectionitem).Attribute(ONames.rdfresource).Value == dId &&
            //            cm.Element(ONames.TagIncollection).Attribute(ONames.rdfresource).Value == collectionId);
            ////  if (collmem != null) membershipExists = true;

            //// А теперь, наконец, добавим членство в коллекции
            //if (!membershipExists)
            //{
            //    //cassette.db.Add(collectionmember);
            //}
            return addedElements;
        }






        private static void Sizing(FreeImageAPI.FreeImageBitmap original, int maxbase, string previewpath)
        {
            int x = original.Width, y = original.Height;
            double factor = ((double)maxbase) / (x > y ? (double)x : (double)y);
            int width = (int)(factor * x);
            int height = (int)(factor * y);
            var resized = new FreeImageAPI.FreeImageBitmap(original, width, height);
            // JPEG_QUALITYGOOD is 75 JPEG.
            // JPEG_BASELINE strips metadata (EXIF, etc.)
            resized.Save(previewpath);//, FREE_IMAGE_FORMAT.FIF_JPEG,
        }

        public static IEnumerable<XElement> AddVideoFile(this Cassette cassette, FileInfo file, string collectionId, out string executeLine)
        {
            //ExecuteLine = "";
            var result = AddFile(cassette, file, collectionId);
            executeLine = App_Bin_Path + "ffmpeg.exe ";// +ExecuteLine;
            return result;
        }
        /// <summary>
        /// Вообще-то это приватный метод, но пока его приходится вызывать из меню. 
        /// Метод создает запись принадлежности документа с идентификатором docId
        /// архиву, заданному дефолтным параметром 
        /// </summary>
        /// <param name="cassette"></param>
        /// <param name="docId"></param>
        /// <returns></returns>
        public static XElement CreateArchiveMember(this Cassette cassette, string docId)
        {
            string archiveId = cassette.DefaultArchiveId();
            string sourceId = cassette.DefaultSourceId();
            if(archiveId != null)
            {
                XElement arch_member = new XElement("archive-member",
                    new XAttribute(ONames.rdfabout, cassette.GenerateNewId()),
                    new XElement("archive-item", new XAttribute(ONames.rdfresource, docId)),
                    new XElement("in-archive", new XAttribute(ONames.rdfresource, archiveId)),
                    sourceId == null ? null : new XElement("info-source", new XAttribute(ONames.rdfresource, sourceId)),
                    new XElement(ONames.TagFromdate, System.DateTime.Now.ToString("s")),
                    null);
                return arch_member;
            }
            return null;
        }

        //private static string ExecuteLine;
        private static void MakeVideoPreview(this Cassette cassette, XElement iisstore)
        {
            string ur = iisstore.Attribute(ONames.AttUri).Value;
            string docPath = ur.Substring(ur.Length - 9, 9);
            StreamWriter sw = File.AppendText(cassette.Dir.FullName + "/convertVideo.bat");
            //exec_line =
            //    "-i \"" + Dir.FullName + "/originals/" + docPath + GetOriginalExtensionFromIisstore(iisstore) +
            //    "\" -y -ar " + this.GetPreviewParameter(iisstore, "medium", "audioBitrate") +
            //    " -b " + this.GetPreviewParameter(iisstore, "medium", "videoBitrate") +
            //    " -r " + this.GetPreviewParameter(iisstore, "medium", "rate") +
            //    " -s " + output_size + " -vcodec msmpeg4 -f avi \"" +
            //    Dir.FullName + "/documents/medium/" + _folderNumber + "/" + _documentNumber + ".avi\"";
            //sw.WriteLine("ffmpeg " + exec_line);
            string executeLine =
                "-i \"" + cassette.Dir.FullName + "/originals/" + docPath + Cassette.GetOriginalExtensionFromIisstore(iisstore) +
                "\" -y -ar " + cassette.GetPreviewParameter(iisstore, "medium", "audioBitrate") +
                " -b " + cassette.GetPreviewParameter(iisstore, "medium", "videoBitrate") +
                " -r " + cassette.GetPreviewParameter(iisstore, "medium", "rate") +
                " -s " + cassette.GetPreviewParameter(iisstore, "medium", "framesize");
            // Преобразование в flv
            //string file_pars_flv = " -f flv \"" + cassette.Dir.FullName + "/documents/medium/" + docPath + ".flv\"";
            //sw.WriteLine(App_Bin_Path + "ffmpeg.exe " + executeLine + file_pars_flv);

            // Преобразование в mp4
            string file_pars_webm = " -f webm -vcodec libvpx -acodec libvorbis \"" + cassette.Dir.FullName + "/documents/medium/" + docPath + ".webm\"";
            sw.WriteLine(App_Bin_Path + "ffmpeg.exe " + executeLine + file_pars_webm);

            // Преобразование в webm
            string file_pars_mp4 = " -vcodec libx264 \"" + cassette.Dir.FullName + "/documents/medium/" + docPath + ".mp4\"";
            sw.WriteLine(App_Bin_Path + "ffmpeg.exe " + executeLine + file_pars_mp4);

            // Преобразование в ogg
            //string[] framesize = cassette.GetPreviewParameter(iisstore, "medium", "framesize").Split(new char[] { 'x' });
            //string file_pars_ogg = " -o \"" + cassette.Dir.FullName + "/documents/medium/" + docPath + ".ogg\"" +
            //    " -V " + cassette.GetPreviewParameter(iisstore, "medium", "videoBitrate").Replace("K", "") +
            //    " -x " + framesize[0] + " -y " + framesize[1] +
            //    " \"" + cassette.Dir.FullName + "/originals/" + docPath + Cassette.GetOriginalExtensionFromIisstore(iisstore) + "\"";
            //sw.WriteLine(App_Bin_Path + "ff2ogg.exe " + file_pars_ogg);

            sw.Close();
            //if (NeedToCalculateVideoPreview != null) NeedToCalculateVideoPreview(this, new EventArgs());
            cassette.NeedToCalculate = true;
        }
        private static void MakeAudioPreview(this Cassette cassette, XElement iisstore)
        {
            //          ================= Надо разобраться! =================
            //string ur = iisstore.Attribute(ONames.AttUri).Value;
            //string docPath = ur.Substring(ur.Length - 9, 9);
            //StreamWriter sw = File.AppendText(cassette.Dir.FullName + "/convertVideo.bat");
            //string executeLine =
            //    "-i \"" + cassette.Dir.FullName + "/originals/" + docPath + Cassette.GetOriginalExtensionFromIisstore(iisstore) +
            //    "\" -y -ar " + cassette.GetPreviewParameter(iisstore, "medium", "audioBitrate");

            //// Преобразование в mp3
            //string file_pars_mp3 = "\"" + cassette.Dir.FullName + "/documents/medium/" + docPath + ".mp3\"";
            //sw.WriteLine(App_Bin_Path + "ffmpeg.exe " + executeLine + file_pars_mp3);

            //sw.Close();
            //cassette.NeedToCalculate = true;
        }
        public static IEnumerable<XElement> AddNewRdf(this Cassette cassette, string owner)
        {
            string dbid = cassette.Name + "_" + owner + "_" + DateTime.Now.Ticks.ToString();
            XElement emptyRdfDoc = Cassette.RDFdb(dbid,
                new XAttribute(ONames.AttOwner, owner),
                new XAttribute(ONames.AttPrefix, dbid + "_"),
                new XAttribute(ONames.AttCounter, "1001"));
            string filepath = cassette.Dir.FullName + "/" + dbid + ".fog";
            emptyRdfDoc.Save(filepath);
            var seq = cassette.AddFile1(new FileInfo(filepath), cassette.CollectionId).ToList();
            // Надо добавить владельца в iisstore документа и хорошо бы проставить uri (это надо делать в файле)
            XElement rdfdoc = seq.First(xe => xe.Name == ONames.TagDocument);
            XElement iisstore = rdfdoc.Element(ONames.TagIisstore);
            iisstore.Add(new XAttribute(ONames.AttOwner, owner));
            File.Delete(filepath);
            return seq;
        }
        /// <summary>
        /// Добавляет директорию в указанную коллекцию кассеты. Возвращает добавленные в базу данных элементы
        /// </summary>
        /// <param name="dir"></param>
        /// <param name="collectionId"></param>
        /// <returns>Возвращает добавленные в базу данных элементы</returns>
        public static IEnumerable<XElement> AddDirectory(this Cassette cassette, DirectoryInfo dir, string collectionId)
        {
            List<XElement> addedElements = new List<XElement>();
            var dname = dir.FullName;
            var shortname = dir.Name;
            // TODO: Надо учесть русские имена и с пробелами
            var newcoll_id = "collection_" + cassette.Name + "_" + dname;
            // Поищу, нет ли такой коллекции в базе данных
            var collection_found =
                cassette.db.Elements(ONames.TagCollection)
                    .FirstOrDefault((XElement e) =>
                    {
                        var xabout = e.Attribute(ONames.rdfabout);
                        return (xabout != null && xabout.Value == newcoll_id);
                    });
            if(collection_found != null) { }
            else
            {
                // Создам коллекцию
                XElement coll =
                    new XElement(ONames.TagCollection,
                        new XAttribute(ONames.rdfabout, newcoll_id),
                        new XElement(ONames.TagName, shortname));
                cassette.db.Add(coll);
                addedElements.Add(coll);
                // Присоединим коллекцию к вышестоящей
                XElement collmem =
                    new XElement(ONames.TagCollectionmember,
                        new XAttribute(ONames.rdfabout, "cm_" + newcoll_id),
                        new XElement(ONames.TagCollectionitem,
                            new XAttribute(ONames.rdfresource, newcoll_id)),
                        new XElement(ONames.TagIncollection,
                            new XAttribute(ONames.rdfresource, collectionId)));
                cassette.db.Add(collmem);
                addedElements.Add(collmem);
                cassette.Changed = true;
            }
            // Обработаем файлы и директории                    
            foreach(var f in dir.GetFiles())
            {
                if(f.Name == "Thumbs.db") { }
                else addedElements.AddRange(cassette.AddFile(f, newcoll_id));
            }
            foreach(var d in dir.GetDirectories())
                addedElements.AddRange(cassette.AddDirectory(d, newcoll_id));
            return addedElements;
        }

    }
}
