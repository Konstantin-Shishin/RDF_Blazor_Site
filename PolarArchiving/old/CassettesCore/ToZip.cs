using System.Collections.Generic;
using System.IO;
using System.Linq;
using System;
using System.Text;
using System.Xml.Linq;
//using Fogid.Cassettes;

namespace Polar.Cassettes
{
    public static class Sarc
    {
        private static long prologLength = 16L;
        /// <summary>
        /// Рекурсивно разворачивает набор файлов и директорий в словарь, значений, ключ формируется как dir_path/filename
        /// </summary>
        /// <param name="dir_path"></param>
        /// <param name="infos"></param>
        /// <returns></returns>
        public static IEnumerable<KeyValuePair<string, FileInfo>> Traverse(string dir_path, IEnumerable<FileSystemInfo> infos)
        {
            string dir_prefix = string.IsNullOrEmpty(dir_path) ? "" : dir_path + "/";
            foreach (var info in infos)
            {
                if (info is FileInfo)
                {
                    var fi = info as FileInfo;
                    yield return new KeyValuePair<string, FileInfo>(dir_prefix + fi.Name, fi); 
                }
                else // info is DirectoryInfo
                {
                    var di = info as DirectoryInfo;
                    foreach (var tr in Traverse(dir_prefix + di.Name, di.GetFiles().Cast<FileSystemInfo>().Concat(
                        di.GetDirectories().Cast<FileSystemInfo>()))) { yield return tr; }
                }
            }
        }
        public static void Create(string sarc_name, IEnumerable<FileSystemInfo> infos)
        {
            // текущая директория
            int last_slash = sarc_name.Replace('\\', '/').LastIndexOf('/');
            string current_dir = sarc_name.Substring(0, last_slash + 1);
            // текущее смещение
            long filebeg = 0L;
            // Создадим каталог архива
            XElement xtable = new XElement("table", new XAttribute("version", "25.05.2012"),
                Traverse("", infos)
                .Select(pair =>
                {
                    long start = filebeg;
                    long length = pair.Value.Length;
                    filebeg += length;
                    return new XElement("file",
                        new XElement("path", pair.Key),
                        new XElement("start", "" + start),
                        new XElement("length", "" + length));
                }));
            // Запишем таблицу в поток байтов
            var memstream = new MemoryStream();
            xtable.Save(new StreamWriter(memstream, Encoding.UTF8)/*, SaveOptions.DisableFormatting*/);
            memstream.Seek(0, SeekOrigin.Begin);
            // Записываемое значение
            long sarc_offset = prologLength + memstream.Length;
            Encoding enc = Encoding.UTF8;
            byte[] sarc_offset_b = enc.GetBytes("" + sarc_offset);
            // Массив из prologLength (16) байтов
            byte[] arr16 = new byte[prologLength];
            for (int i=0; i< sarc_offset_b.Length; i++) arr16[i] = sarc_offset_b[i];
            // Еще можно вервию архиватора записать в последний байт
            // Формирование файла
            FileStream stream = new FileStream(sarc_name, FileMode.Create, FileAccess.Write);
            BinaryWriter writer = new BinaryWriter(stream);
            // записываем prologLength (16) байтов
            writer.Write(arr16);
            byte[] buffer = new byte[4096];
            BinaryReader reader = new BinaryReader(memstream);
            int nb = 0;
            // записываем каталог
            while ((nb = reader.Read(buffer, 0, buffer.Length)) > 0) writer.Write(buffer, 0, nb);
            reader.Close();
            // записываем файлы
            foreach (string file_name in xtable.Elements("file").Select(f => f.Element("path").Value))
            {
                reader = new BinaryReader(new FileStream(current_dir + file_name, FileMode.Open, FileAccess.Read));
                while ((nb = reader.Read(buffer, 0, buffer.Length)) > 0) writer.Write(buffer, 0, nb);
                reader.Close();
            }
        
            // заканчиваем
            writer.Flush();
            writer.Close();
            stream.Close();
        }
        public struct SarcFileLocation
        {
            public string relative_path; // key
            public string sarc_full_name;
            public long file_offset;
            public long file_length;
        }
        private static Dictionary<string, SarcFileLocation> SarcCatalog = null;
        public static SarcFileLocation GetFileLocation(string sarc_full_name, string relative_path)
        {
            if (SarcCatalog == null || SarcCatalog.Count > 100000) SarcCatalog = new Dictionary<string, SarcFileLocation>();
            if (!SarcCatalog.ContainsKey(relative_path))
            {
                Stream stream = new FileStream(sarc_full_name, FileMode.Open, FileAccess.Read);
                //stream.Seek(prologLength, SeekOrigin.Begin);
                long offset = 0L;
                for (int i = 0; i < 16; i++) { int b = stream.ReadByte(); if (b != 0) offset = offset * 10 + (b - '0'); }
                byte[] catalog_bytes = new byte[offset - 16];
                var v = stream.Read(catalog_bytes, 0, (int)(offset - 16));
                MemoryStream mem = new MemoryStream(catalog_bytes);
                //Encoding etable = Encoding.UTF8;
                //XElement cat = XElement.Parse(etable.GetString(catalog_bytes));
                System.Xml.XmlReader xreader = new System.Xml.XmlTextReader(mem);
                //XElement catalog = XElement.Load(new StreamReader(stream, System.Text.Encoding.UTF8));
                XElement catalog = XElement.Load(xreader);
                stream.Close();
                foreach (XElement xfile in catalog.Elements("file"))
                {
                    string r_path = xfile.Element("path").Value;
                    SarcFileLocation sfl = new SarcFileLocation()
                    {
                        relative_path = r_path,
                        sarc_full_name = sarc_full_name,
                        file_offset = Int64.Parse(xfile.Element("start").Value) + offset,
                        file_length = Int64.Parse(xfile.Element("length").Value)
                    };
                    SarcCatalog.Add(r_path, sfl);
                }
            }
            return SarcCatalog[relative_path];
        }
        public static Stream GetFileAsStream(string sarc_name_full, string relative_path)
        {
            SarcFileLocation f_location = GetFileLocation(sarc_name_full, relative_path);
            MemoryStream mem = new MemoryStream();
            Stream stream = new FileStream(sarc_name_full, FileMode.Open);
            stream.Seek(f_location.file_offset, SeekOrigin.Begin);

            byte[] buffer = new byte[4096];
            BinaryReader reader = new BinaryReader(stream);
            BinaryWriter writer = new BinaryWriter(mem);
            int nb = 0;
            // записываем часть файла в mem
            long nbytes = f_location.file_length;
            while (nbytes > 0)
            {
                int portion = buffer.Length < nbytes ? buffer.Length : (int)nbytes;
                nb = reader.Read(buffer, 0, portion);
                writer.Write(buffer, 0, nb);
                nbytes -= nb;
            }
            reader.Close(); stream.Close();
            mem.Seek(0, SeekOrigin.Begin);
            return mem;
        }
    }
    //public class ToZip
    //{
    //    public static void ZipFolder(string fileName, string dirPath, string dirInArch, string withFile)
    //    {
    //        if(!Directory.Exists(dirPath)) throw new DirectoryNotFoundException(dirPath);
    //        //  var fileName = Path.Combine(Path.GetDirectoryName(dirPath), Path.GetFileName(dirPath)) + ".zip";
    //        using(var zip = new ZipFile { CompressionLevel = Ionic.Zlib.CompressionLevel.None })
    //        {
    //            zip.AddDirectory(dirPath, dirInArch);
    //            zip.AddFile(withFile, "");
    //            zip.Save(fileName);
    //        }
    //        // return fileName;
    //    }

    //    public static void ZipFiles(string fileName, Dictionary<string, string> files)
    //    {
    //        using(var zip = new ZipFile { CompressionLevel = Ionic.Zlib.CompressionLevel.None })
    //        {
    //            foreach(var file in files)
    //                if(File.Exists(file.Key))
    //                    zip.AddFile(file.Key, file.Value);
    //                else if(Directory.Exists(file.Key))
    //                    zip.AddDirectory(file.Key, file.Value);
    //                else
    //                    throw new FileNotFoundException(file.Key);
    //            zip.Save(fileName);
    //        }
    //    }
    //    //public static void ExtractJustFile(this string archPath, string filePath, ref Stream output)
    //    //{
    //    //    if (!File.Exists(archPath)) throw new FileNotFoundException(archPath);
    //    //    using (ZipFile zip=ZipFile.Read(archPath))
    //    //    {
    //    //        var fileEntry = zip[filePath];
    //    //        if (filePath == null) throw new FileNotFoundException(filePath);
    //    //        fileEntry.Extract(output);
    //    //    }
    //    //}
    //    public static void UnZipFolder(string zipPath, string dirPath)
    //    {
    //        // if(!File.Exists(zipPath)) throw new FileNotFoundException(zipPath);
    //        using(var zip = ZipFile.Read(zipPath))
    //            zip.ExtractAll(dirPath, ExtractExistingFileAction.DoNotOverwrite);
    //    }
    //}
    public static class ToArchive
    {
        static Dictionary<string, string> AddFolderSubfiles(Dictionary<string, string> folders)
        {
            if(folders==null) throw new Exception("folders is null"); ;
            Action<string> AddFilesfromCurrent=null;
            foreach(var folder in folders.Keys.ToList())
                AddFilesfromCurrent(folder);
            return folders;
        }

        public static Encoding TableEncoding=Encoding.UTF8, TableLengthEncoding=Encoding.UTF7;
        private static int MaxTableLengthLength = 16;
        public static void Create(this string path, Dictionary<string, string> filesAndDirsRelativePaths)
        {
            //var fileTable=new StreamWriter(path+".txt");
           var filesRelativePaths = AddFolderSubfiles(filesAndDirsRelativePaths.ToDictionary(i=>i.Key, i=>i.Value));
            using (var fileAllOrigin = new MemoryStream())
            using (var fileAll = File.Create(path))
            {
                var startFileTable=new Dictionary<string, long>();
                var endFileTable=new Dictionary<string, long>();
                foreach (var file in filesRelativePaths)
                    using (var fileStream = File.OpenRead(file.Key))
                    {
                        startFileTable.Add(file.Value, fileAllOrigin.Length);
                        endFileTable.Add(file.Value, fileStream.Length);

                        var bytes = new byte[fileStream.Length];
                        fileStream.Read(bytes, 0, bytes.Length);
                        fileAllOrigin.Write(bytes, 0, bytes.Length);
                    }
                var xTable = new XElement("table", new XAttribute("version", "25.05.2012"),
                                          endFileTable.Select(i => new XElement("file",
                                                                                new XElement("path", i.Key),
                                                                                new XElement("start",
                                                                                             startFileTable[i.Key]),
                                                                                new XElement("length", i.Value))));
                var tableBytes=TableEncoding.GetBytes(xTable.ToString());
                var tableLengthbytes = TableLengthEncoding.GetBytes(tableBytes.Length.ToString());
                //int MaxTableLengthLength = 13;
                if(tableLengthbytes.Length>MaxTableLengthLength)
                {
                    LOG.WriteLine("Длина " + tableBytes.Length + tableLengthbytes.Length + " таблицы архива больше 10тб");
                    return;
                }
                var fullTableLengthBytes = new byte[MaxTableLengthLength];
                for (int i = 0; i < tableLengthbytes.Length; i++)
                    fullTableLengthBytes[i] = tableLengthbytes[i];
                fileAll.Write(fullTableLengthBytes, 0, MaxTableLengthLength);
                fileAll.Write(tableBytes, 0, tableBytes.Length);
                fileAllOrigin.Seek(0, SeekOrigin.Begin);
                fileAllOrigin.WriteTo(fileAll);

                //foreach(var item in filesAndDirsRelativePaths)
                //    ClearDirOrFile(item.Key);
              
            }
      }
         public static void ClearDirOrFile(this string path)
         {
             try
             {
                 File.Delete(path);
             }
             catch
             {
                 foreach(var subFile in Directory.GetFiles(path))
                     File.Delete(subFile);
                 foreach(var directory in Directory.GetDirectories(path))
                     ClearDirOrFile(directory);
                 Directory.Delete(path);
             }
         }
        public static Dictionary<string, KeyValuePair<long, long>> CacheInArchives;
        public static Dictionary<string, bool> CachedArchives;

        public static bool reCache;

        public static string[] UriToCassetteAndPath(this string uri)
        {
            var substrs = uri.Split(new[] {"@iis.nsk.su"},StringSplitOptions.None);
            if(substrs.Length!=2)
                LOG.WriteLine("strange uri "+uri);
            return substrs;// Tuple.Create(substrs[0], substrs[1]);
        }

        public static KeyValuePair<long, long> Extract(this CassetteInfo cassette, string archRelativePath, string inArchivePath)
        {
            KeyValuePair<long, long> inArchivePosLength=new KeyValuePair<long, long>();
                    string key = cassette.cassette.Name + "/" + archRelativePath + "/" + inArchivePath;
            bool isOk=false;
            CachedArchives.Add(cassette.cassette.Name + "/" + archRelativePath, true);
            CachedArchives[cassette.cassette.Name + "/" + archRelativePath] = ExtractArchiveToMemory(cassette.cassette.Name, inArchivePath, cassette.url, archRelativePath);
            //task.GetAwaiter().OnCompleted(() => CachedArchives[cassette.Name + "/" + archRelativePath] = task.Result);
            //while (task.Status == TaskStatus.Running && (isOk = !CacheInArchives.TryGetValue(key, out inArchivePosLength)))
            //{}
           if(!isOk && !CacheInArchives.TryGetValue(key, out inArchivePosLength))
           {
               LOG.WriteLine("Нет в архиве "+cassette.url+archRelativePath+" файла "+inArchivePath);
               throw new Exception();//return null;
           }
            return
                new KeyValuePair<long, long>(inArchivePosLength.Key, inArchivePosLength.Value);
            //CacheInArchives = CacheInArchives.Concat()
            //             .ToDictionary(i => i.Key, i => i.Value); Task<bool>   Tuple.Create
        }

        public static bool ExtractArchiveToMemory(string cassetteName, string inArchivePath, string archivePath, string archiveName)
        {
            archivePath += archiveName + ".sarc";
            if (!File.Exists(archivePath))
            {
                LOG.WriteLine("не найден файл " + archivePath);
                return false;
            }
            if (reCache || CacheInArchives.Count > 100000)
            {
                //  lock (CacheTables)
                CacheInArchives.Clear();
                // lock (CachedArchives)
                CachedArchives.Clear();
            }    
            using (Stream read = File.Open(archivePath, FileMode.Open))
                try
                {
                    var tableLengthBytes = new byte[MaxTableLengthLength];
                    read.Read(tableLengthBytes, 0, tableLengthBytes.Length);
                    var tableLengthString = TableLengthEncoding.GetString(tableLengthBytes);
                    if (!int.TryParse(tableLengthString, out int tableLength))
                    {
                        LOG.WriteLine(tableLength + " is Not Long in " + archivePath);
                        return false;
                    }
                    var tableBytes = new byte[tableLength];
                    read.Read(tableBytes, 0, tableLength);
                    var xTable = XElement.Parse(TableEncoding.GetString(tableBytes));
                    foreach (var i in xTable.Elements().ToDictionary(                     //AsParallel()
                        x => cassetteName + "/" + archiveName + "/" + x.Element("path").Value,
                        x =>
                            {
                                long newVal1=-1, newVal2=-1;
                                if (x.Element("start")==null || !long.TryParse(x.Element("start").Value, out newVal1) ||
                                   x.Element("length")==null || !long.TryParse(x.Element("length").Value, out newVal2))
                                    LOG.WriteLine("can't parse " + x.ToString() + " to long in " + archivePath);
                                return new KeyValuePair<long, long>(newVal1, newVal2);
                            })) CacheInArchives.Add(i.Key, i.Value);
                    return true;
                }
                catch
                {
                    return false;
                }
        }
    
    }
}

