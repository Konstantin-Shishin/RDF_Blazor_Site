using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Xml.Linq;
//using sema2012m;
using Polar.Cassettes.DocumentStorage;


namespace OpenArchive
{
    /// <summary>
    /// Summary description for StaticObjects
    /// </summary>
    public static class StaticObjects
    {
        private static System.Diagnostics.Stopwatch sw = new System.Diagnostics.Stopwatch();
        public static void RestartStopWatch() { sw.Restart(); }
        public static void StopStopWatch() { sw.Stop(); }
        public static long ElapsedMilliseconds { get { return sw.ElapsedMilliseconds; } }
        private static bool _initiated = false;
        public static bool Initiated { get { sw.Restart(); return _initiated; } }
        private static string _path;
        private static XElement _config;
        // Доступ к движку только через свойство Engine и только последовательно!!! Но как??? Через методы! В адаптере есть!
        private static DStorage storage = null;
        private static DbAdapter _engine;
        public static DbAdapter engine { get { return _engine; } }

        public static void Init() { Init(_path); }
        public static void Init(string path)
        {
            _path = path;
            if (_path[_path.Length - 1] != '/' && _path[_path.Length - 1] != '\\') _path = _path + "/";

            // Инициирование системного лога проекта Тургунда
            InitLog(out turlog, _path + "logs/turlog.txt", true);
            turlog("OpenArchive initiating... path=" + path);

            _config = XElement.Load(_path + "config.xml");
            string connectionstring = _config.Element("database").Attribute("connectionstring").Value;
            // Инициируем движок
            storage = new DStorage();
            storage.Init(_config);
            if (connectionstring.StartsWith("xml:"))
            {
                _engine = new XmlDbAdapter();
            }
            else if (connectionstring.StartsWith("polar:"))
            {
                connectionstring = connectionstring.Substring("polar:".Length);
                //_engine = new sema2012m.PolarBasedAdapter(connectionstring);
            }
            else throw new Exception("Unknown engine: " + connectionstring);
            storage.InitAdapter(_engine);

            // Присоединимся к кассетам через список из конфигуратора (это надо перенести в загрузочную часть) 
            try
            {
                storage.LoadFromCassettesExpress();
            }
            catch (Exception ex)
            {
                turlog("Error while OpenArchive initiating: " + ex.Message);
                return;
            }
            // Попытка вычислить константы
            //try
            {
                CalculateConstants();
            }
            //catch (Exception ex) { }
        }

        public static void Reload()
        {
            Init();
            //engine.DropTables(turlog, turlog);
            engine.StartFillDb(turlog);
            //engine.LoadFromCassettesExpress(
            //    CassetteKernel.CassettesConnection.GetFogFiles()
            //    .Select((factograph.RDFDocumentInfo docinfo) => docinfo.filePath)
            //    .ToArray(),
            //    turlog, turlog);
            storage.LoadFromCassettesExpress();
            engine.FinishFillDb(turlog);
            // Попытка вычислить константы
            //try
            {
                try
                {
                    if (System.IO.File.Exists(_path + "logs/xtree.xml")) System.IO.File.Delete(_path + "logs/xtree.xml");
                }
                catch (Exception ex) { }
                CalculateConstants();
            }
            //catch (Exception) { }
        }

        // Константы, зависящие от данных
        public static string funds_id = null;
        private static XElement formattoparentcollection =
            new XElement("record",
                new XElement("inverse", new XAttribute("prop", "http://fogid.net/o/collection-item"),
                    new XElement("record",
                        new XElement("direct", new XAttribute("prop", "http://fogid.net/o/in-collection"),
                            new XElement("record", new XAttribute("type", "http://fogid.net/o/collection"),
                                new XElement("field", new XAttribute("prop", "http://fogid.net/o/name")))))));
        private static XElement formattochildfromcollection =
            new XElement("record",
                new XElement("inverse", new XAttribute("prop", "http://fogid.net/o/in-collection"),
                    new XElement("record",
                        new XElement("direct", new XAttribute("prop", "http://fogid.net/o/collection-item"),
                            new XElement("record",
                                new XElement("field", new XAttribute("prop", "http://fogid.net/o/name")))))));
        public static IEnumerable<XElement> GetDocsFromCollection(string id)
        {
            //XElement xtree = _engine.GetItemById(id, formattochildfromcollection);
            //var query = xtree.Elements("inverse")
            //    .Where(xi => xi.Attribute("prop").Value == "http://fogid.net/o/in-collection")
            //    .Select(xi => xi.Element("record").Element("direct").Element("record"));
            //foreach (XElement child in query)
            //{
            //    if (child.Attribute("type").Value == "http://fogid.net/o/document") yield return child;
            //    foreach (XElement x in GetDocsFromCollection(child.Attribute("id").Value)) yield return x;
            //}
            XElement rec = xtree.DescendantsAndSelf("record")
                .FirstOrDefault(x => x.Attribute("id").Value == id);
            if (rec != null)
            {
                return rec.DescendantsAndSelf("record")
                    .Select(r => new XElement("record", new XAttribute("id", r.Attribute("id").Value)));
            }
            return Enumerable.Empty<XElement>();
        }
        public static XElement xtree;
        public static void CalculateConstants()
        {
            string funds_name = "Фонды";
            XElement rec = StaticObjects.engine.SearchByName(funds_name)
                .FirstOrDefault(r =>
                {
                    if (r.Attribute("type").Value != "http://fogid.net/o/collection") return false;
                    var na = r.Elements("field")
                        .FirstOrDefault(f => f.Attribute("prop").Value == "http://fogid.net/o/name" && f.Value == funds_name);
                    if (na == null) return false;
                    return true;
                });
            //.ToArray();
            if (rec != null)
            {
                funds_id = rec.Attribute("id").Value;
                //Сделаем дерево. Будем вкладывать коллекции и документы в коллекции более высокого уровня

                // Пройдемся по дереву фондов
                //int sum = Traverse(funds_id);

                try
                {
                    xtree = XElement.Load(_path + "logs/xtree.xml");
                }
                catch (Exception ex)
                {
                    xtree = new XElement(rec);
                    xtree.Add(CollectChilds(rec.Attribute("id").Value));
                    xtree.Save(_path + "logs/xtree.xml");
                }
            }
        }
        private static int Traverse(string coll)
        {
            int sum = 0;
            var ite = _engine.GetItemByIdBasic(coll, false);
            if (ite == null) return 0;
            if (ite.Attribute("type").Value == "http://fogid.net/o/document") return 1;
            if (ite.Attribute("type").Value != "http://fogid.net/o/collection") return 0;
            var item = _engine.GetItemByIdBasic(coll, true);
            var query = item.Elements("inverse")
                .Where(xi => xi.Attribute("prop").Value == "http://fogid.net/o/in-collection")
                .Select(xi => xi.Element("record").Attribute("id").Value);
            foreach (var rel_id in query)
            {
                XElement rel = _engine.GetItemByIdBasic(rel_id, false);
                XElement collection_item = rel.Elements("direct")
                    .Where(xi => xi.Attribute("prop").Value == "http://fogid.net/o/collection-item")
                    .FirstOrDefault();
                if (collection_item == null) continue;
                sum += Traverse(collection_item.Element("record").Attribute("id").Value);
            }
            return sum;
        }

        private static int Traverse0(string coll)
        {
            XElement xt = _engine.GetItemById(coll, formattochildfromcollection);
            var query = xt.Elements("inverse")
                .Where(xi => xi.Attribute("prop").Value == "http://fogid.net/o/in-collection")
                .Select(xi => xi.Element("record").Element("direct").Element("record"));
            int sum = 0;
            foreach (var item in query)
            {
                if (item.Attribute("type").Value == "http://fogid.net/o/document")
                {
                    sum++;
                }
                else if (item.Attribute("type").Value == "http://fogid.net/o/collection")
                {
                    sum += Traverse0(item.Attribute("id").Value);
                }

            }
            return sum;
        }
        private static IEnumerable<XElement> CollectChilds(string id)
        {
            XElement xt = _engine.GetItemById(id, formattochildfromcollection);
            var query = xt.Elements("inverse")
                .Where(xi => xi.Attribute("prop").Value == "http://fogid.net/o/in-collection")
                .Select(xi => xi.Element("record").Element("direct").Element("record"));
            return query
                .Select(ch =>
                {
                    if (ch.Attribute("type").Value == "http://fogid.net/o/document")
                    {
                        return new XElement(ch);
                    }
                    else if (ch.Attribute("type").Value == "http://fogid.net/o/collection")
                    {
                        XElement xx = new XElement(ch);
                        xx.Add(CollectChilds(ch.Attribute("id").Value));
                        return xx;
                    }
                    else return null;
                });
        }

        // ====== Вспомогательные процедуры Log - area
        private static object locker = new object();
        private static Action<string> turlog = s => { };
        private static Action<string> changelog = s => { };
        private static Action<string> convertlog = s => { };
        private static void InitLog(out Action<string> log, string path, bool timestamp)
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
        // Статические методы выявления узлов и полей
        public static string GetDates(XElement item)
        {
            var fd_el = item.Elements("field").FirstOrDefault(f => f.Attribute("prop").Value == "http://fogid.net/o/from-date");
            var td_el = item.Elements("field").FirstOrDefault(f => f.Attribute("prop").Value == "http://fogid.net/o/to-date");
            string res = (fd_el == null ? "" : fd_el.Value) + (td_el == null ? "" : "—" + td_el.Value);
            return res;
        }
        public static string GetField(XElement item, string prop)
        {
            var el = item.Elements("field").FirstOrDefault(f => f.Attribute("prop").Value == prop);
            return el == null ? "" : el.Value;
        }
        public static IEnumerable<XElement> GetCollectionPath(string id)
        {
            //return _getCollectionPath(id);
            System.Collections.Generic.Stack<XElement> stack = new Stack<XElement>();
            XElement node = engine.GetItemByIdBasic(id, false);
            if (node == null) return Enumerable.Empty<XElement>();
            stack.Push(node);
            bool ok = GetCP(id, stack);
            if (!ok) return Enumerable.Empty<XElement>();
            int n = stack.Count();
            // Уберем первый и последний
            var query = stack.Skip(1).Take(n - 2).ToArray();
            return query;
        }
        // В стеке накоплены элементы пути, следующие за id. Последним является узел с id
        private static bool GetCP(string id, Stack<XElement> stack)
        {
            if (id == funds_id) return true;
            XElement tree = engine.GetItemById(id, formattoparentcollection);
            if (tree == null) return false;
            foreach (var n1 in tree.Elements("inverse"))
            {
                var n2 = n1.Element("record"); if (n2 == null) return false;
                var n3 = n2.Element("direct"); if (n3 == null) return false;
                var node = n3.Element("record"); if (node == null) return false;
                string nid = node.Attribute("id").Value;
                stack.Push(node);
                bool ok = GetCP(nid, stack);
                if (ok) return true;
                stack.Pop();
            }
            return false;
        }
        private static IEnumerable<XElement> _getCollectionPath(string id)
        {
            if (id == funds_id) return Enumerable.Empty<XElement>();
            XElement tree = engine.GetItemById(id, formattoparentcollection);
            if (tree == null) return Enumerable.Empty<XElement>();
            var n1 = tree.Element("inverse"); if (n1 == null) return Enumerable.Empty<XElement>();
            var n2 = n1.Element("record"); if (n2 == null) return Enumerable.Empty<XElement>();
            var n3 = n2.Element("direct"); if (n3 == null) return Enumerable.Empty<XElement>();
            var n4 = n3.Element("record"); if (n4 == null) return Enumerable.Empty<XElement>();
            XElement node = n4; // tree.Element("inverse").Element("record").Element("direct").Element("record");
            string nid = node.Attribute("id").Value;
            if (nid == funds_id) return GetCollectionPath(nid);
            return GetCollectionPath(nid).Concat(new XElement[] { node });
        }
        public static string GetParentCollectionId(string item_id)
        {
            XElement ext_record = engine.GetItemByIdBasic(item_id, true);
            if (ext_record == null) return null;
            XElement inv_prop = ext_record.Elements("inverse")
                .FirstOrDefault(x => x.Attribute("prop").Value == "http://fogid.net/o/collection-item");
            if (inv_prop == null) return null;
            XElement rec = inv_prop.Element("record");
            if (rec == null) return null;
            string rel_id = rec.Attribute("id").Value;

            XElement ext_record2 = engine.GetItemByIdBasic(rel_id, false);
            if (ext_record2 == null) return null;
            XElement dir_prop = ext_record2.Elements("direct")
                .FirstOrDefault(x => x.Attribute("prop").Value == "http://fogid.net/o/in-collection");
            if (dir_prop == null) return null;
            XElement rec2 = dir_prop.Element("record");
            if (rec2 == null) return null;
            return rec2.Attribute("id").Value;
        }
    }
    public class SCompare : IComparer<string>
    {
        public int Compare(string s1, string s2)
        {
            if (string.IsNullOrEmpty(s1) && string.IsNullOrEmpty(s2)) return 0;
            if (string.IsNullOrEmpty(s1)) return 1;
            if (string.IsNullOrEmpty(s2)) return -1;
            return s1.CompareTo(s2);
        }
        public static SCompare comparer = new SCompare();
    }
}