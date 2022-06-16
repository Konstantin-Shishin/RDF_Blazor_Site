using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml.Linq;
using Fogid.Cassettes;

namespace Fogid.DocumentStorage
{
    /// <summary>
    /// Класс, реализующий документное хранилище
    /// </summary>
    public class DStorage
    {
        private string connectionstring = "";
        private string cs_prefix = "";
        private CassettesConnection connection = new CassettesConnection();
        // Возможно, надо будет отказаться от следующего конструктора
        public DStorage() { }
        // Загрузка config.xml
        public void Init(XElement xconfig)
        {
            XElement database_element = xconfig.Element("database");
            if (database_element != null)
            {
                XAttribute cs_att = database_element.Attribute("connectionstring");
                if (cs_att != null)
                {
                    connectionstring = cs_att.Value;
                    int pos = connectionstring.IndexOf('.');
                    if (pos > 0)
                    {
                        cs_prefix = connectionstring.Substring(0, pos);
                        connectionstring = connectionstring.Substring(pos + 1);
                    }
                }

            }
            // Присоединимся к кассетам через список из конфигуратора 
            try
            {
                connection.ConnectToCassettes(xconfig.Elements("LoadCassette"),
                    s => Console.WriteLine(s));
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error while Turgunda initiating: " + ex.Message);
                return;
            }
        }
        // ================= Доступ к хранимым файлам (получение path к файлам без расширений) ===============
        public string GetOriginalFileName(string u)
        {
            if (string.IsNullOrEmpty(u) || !connection.cassettesInfo.ContainsKey(u.Substring(0, u.Length - 15).ToSpecialCase()))
            { return null; }
            string path = connection.cassettesInfo[u.Substring(0, u.Length - 15).ToSpecialCase()].url +
                "originals/";
            string filenam = path + u.Substring(u.Length - 9);
            return filenam;
        }
        public string GetPhotoFileName(string u, string s)
        {
            string fileName = null;
            string cass_name = u.Substring(0, u.Length - 15);
            if (!string.IsNullOrEmpty(u) && connection.cassettesInfo.ContainsKey(u.Substring(0, u.Length - 15).ToSpecialCase()))
                fileName = connection.cassettesInfo[u.Substring(0, u.Length - 15).ToSpecialCase()].url +
                    "documents/" + s + "/" + u.Substring(u.Length - 9);
            return fileName;
        }
        // Использование
        //public ActionResult GetPhoto(string u, string s)
        //{
        //    string filenam = GetPhotoFileName(u, s);
        //    return new FilePathResult(filename + ".jpg", "image/jpeg");
        //}
        public string GetVideoFileName(string u)
        {
            if (string.IsNullOrEmpty(u) || !connection.cassettesInfo.ContainsKey(u.Substring(0, u.Length - 15).ToSpecialCase()))
            { return null; }
            //string ext = ".webm";
            string path = connection.cassettesInfo[u.Substring(0, u.Length - 15).ToSpecialCase()].url +
                "documents/medium/";
            string fileName = path + u.Substring(u.Length - 9);
            return fileName;
        }
        // Использовани в MVC
        //public FilePathResult GetVideo(string u)
        //{
        //    string filenam = GetVideoFileName(u);
        //    string video_extension = "";
        //    if (System.IO.File.Exists(filenam + ".mp4")) video_extension = ".mp4";
        //    else if (System.IO.File.Exists(filenam + ".flv")) video_extension = ".flv";
        //    return new FilePathResult(filenam + video_extension, "video/" + video_extension.Substring(1));
        //}
        public string GetAudioFileName(string u)
        {
            if (string.IsNullOrEmpty(u) || !connection.cassettesInfo.ContainsKey(u.Substring(0, u.Length - 15).ToSpecialCase()))
            { return null; }
            string ext = ".mp3";
            string path = connection.cassettesInfo[u.Substring(0, u.Length - 15).ToSpecialCase()].url +
                "originals/";
            string fileName = path + u.Substring(u.Length - 9) + ext;
            return fileName;
        }
        public string GetDZFilePath(string cassetteNeme, string directoryNumber, string fileNumber)
        {
            if (string.IsNullOrEmpty(cassetteNeme)) return null;
            string fileName = null;
            var cassetteFullName = ("iiss://" + cassetteNeme + "@iis.nsk.su").ToSpecialCase();

                if (connection.cassettesInfo.ContainsKey(cassetteFullName))
            {
                fileName = connection.cassettesInfo[cassetteFullName].url +
                           "documents/deepzoom/" + directoryNumber + "/" + fileNumber;
            }
            return fileName;
        }

        public Stream GetFileFromSarc(string cassetteNeme, string directoryNumber, string fileNumber, string pathInArchive)
        {
           return Sarc.GetFileAsStream(GetDZFilePath(cassetteNeme, directoryNumber, fileNumber)+".sarc2", pathInArchive);
        }

        // ====== Вспомогательные процедуры Log - area
        private object locker = new object();
        public Action<string> turlog = s => { };
        private Action<string> changelog = s => { };
        private Action<string> convertlog = s => { };
        private void InitLog(out Action<string> log, string path, bool timestamp)
        {
            log = (string line) =>
            {
                lock (locker)
                {
                    try
                    {
                        var saver = new System.IO.StreamWriter(path, true, System.Text.Encoding.UTF8);
                        saver.WriteLine((timestamp ? (DateTime.Now.ToString("s") + " ") : "") + line);
                        saver.Close();
                    }
                    catch (Exception)
                    {
                        //LogFile.WriteLine("Err in buildlog writing: " + ex.Message);
                    }
                }
            };
        }

        // =========== Адаптер базы данных ===========
        private DbAdapter dbadapter = null;
        public void InitAdapter(DbAdapter adapter) 
        { 
            dbadapter = adapter;
            if (adapter != null)
            {
                dbadapter.Init(connectionstring);
            }
        }

        // =========== Загрузка базы данных через адаптер ===========
        public void LoadFromCassettesExpress()
        {
            turlog("Начало загрузки данных");
            dbadapter.StartFillDb(turlog);

            var fogfilearr = connection.GetFogFiles().Select(d => d.filePath).ToArray();
            turlog("Загрузка кассет");
            dbadapter.LoadFromCassettesExpress(fogfilearr, turlog, convertlog);
            turlog("Конец загрузки кассет");
            dbadapter.FinishFillDb(turlog);

            turlog("Конец загрузки данных.");
            //factograph.RDFDocumentInfo.logwrite = mess => turlog(mess); // Это для выдачи ошибок из кассетных методов в системный лог
        }
        public void SaveDb(string filename)
        {
            dbadapter.Save(filename);
        }
        
        // =========== Основные процедуры ==========
        public IEnumerable<XElement> SearchByName(string searchstring) { return dbadapter.SearchByName(searchstring); }
        //public string GetType(string id) { return adapter.GetType(id); }
        public XElement GetItemByIdBasic(string id, bool addinverse) { return dbadapter.GetItemByIdBasic(id, addinverse); }
        public XElement GetItemById(string id, XElement format) { return dbadapter.GetItemById(id, format); }
        public XElement GetItemByIdSpecial(string id) { return dbadapter.GetItemByIdSpecial(id); }

        // =========== Дополнительные процедуры ==========
        // Создает массив строк из идентификатора и полей записи по заданному набору имен свойств полей
        private static string[] GetFields(XElement tree, string[] fieldprops)
        {
            var query = (new string[] { tree.Attribute("id").Value }).Concat(fieldprops.Select(prop =>
            {
                var field_el = tree.Elements("field").FirstOrDefault(f => f.Attribute("prop").Value == prop);
                return field_el == null ? null : field_el.Value;
            })).ToArray();
            return query;
        }
        // Создает массив строк развертки (идентификатора, полей)... по формату
        public static string[] ScanFields(XElement tree, XElement format)
        {
            string[] fieldprops = format.Elements("field").Select(f => f.Attribute("prop").Value).ToArray();
            string[] res = GetFields(tree, fieldprops);
            foreach (XElement dirinv_format in format.Elements().Where(el => el.Name == "direct" || el.Name == "inverse"))
            {
                // Берем первую запись
                XElement re_format = dirinv_format.Element("record");
                // Находим первый элемент
                var subtree = tree.Elements(dirinv_format.Name)
                    .Where(se => se.Attribute("prop").Value == dirinv_format.Attribute("prop").Value)
                    .FirstOrDefault();
                if (subtree == null) continue;
                XElement subrecord = subtree.Element("record");
                if (subrecord == null) continue;
                res = res.Concat(ScanFields(subrecord, re_format)).ToArray();
            }
            return res;
        }
        public static IEnumerable<string[]> InverseRows(XElement tree, XElement format, string iprop)
        {
            // формат данной инверсной цепочки
            var first = format.Elements("inverse")
                .FirstOrDefault(inv => inv.Attribute("prop").Value == iprop);
            if (first == null) return Enumerable.Empty<string[]>();
            XElement iformat = first.Element("record");
            if (iformat == null) return Enumerable.Empty<string[]>();
            var query = tree.Elements("inverse")
                .Where(el => el.Attribute("prop").Value == iprop)
                .Select(el => el.Element("record"))
                //.Where(re => rtype==null || re.Attribute("type").Value == rtype)
                .Select(re => ScanFields(re, iformat));
            return query;
        }

        //=============== Редактирующие методы =================
        public XElement EditCommand(XElement comm)
        {
            // Определение активного fog-документа
            XAttribute owner_att = comm.Attribute("owner");
            if (owner_att == null) return null;
            Fogid.Cassettes.RDFDocumentInfo active_fog = connection.GetFogFiles()
                .Where(fg => fg.owner == owner_att.Value && fg.isEditable)
                .FirstOrDefault();
            if (active_fog == null) return null;
            owner_att.Remove();
            // Еще надо бы проверить временную отметку. Сейчас мы "поверим", что она более поздняя
            if (comm.Name == "{http://fogid.net/o/}delete")
            {
                XAttribute id_att = comm.Attribute("id");
                if (id_att != null) 
                {
                    string id = id_att.Value;
                    active_fog.GetRoot().Add(new XElement(comm));
                    active_fog.isChanged = true;
                    active_fog.Save();
                    dbadapter.Delete(id);
                }
            }
            else if (comm.Name == "{http://fogid.net/o/}substitute")
            {
                throw new NotImplementedException();
            }
            else
            {
                XAttribute id_att = comm.Attribute(Fogid.Cassettes.ONames.rdfabout);
                if (id_att == null)
                {
                    string id = active_fog.NextId();
                    comm.Add(new XAttribute(Fogid.Cassettes.ONames.rdfabout, id));
                    comm = dbadapter.Add(comm);
                }
                else
                {
                    comm = dbadapter.AddUpdate(comm);
                }
                active_fog.GetRoot().Add(new XElement(comm));
                active_fog.isChanged = true;
                active_fog.Save();
            }
            return comm;
        }
        
    }
}
