using System;
using System.Linq;
using System.Collections.Generic;
using System.Xml.Linq;

using Polar.Cassettes;
using Polar.Cassettes.DocumentStorage;

namespace Turgunda7
{
    public class SObjects
    {
        public static XElement look = new XElement("empty");

        public static XElement accounts = null;
        public static string path = null;
        public static string extappbinpath = null;
        public static string newcassettesdirpath = null;
        public static XElement finfo_configdefault = null;
        public static string appname = "Открытый архив СО РАН";
        //internal static DStorage storage = null;
        //private static DbAdapter engine = null;
        //private static CassetteData.CassetteIntegration _engine;
        //public static CassetteData.CassetteIntegration Engine { get { return _engine; } }
        private static DbAdapter _engine;
        public static DbAdapter Engine { get { return _engine; } }

        public static bool Initiated = true;
        private static System.Text.StringBuilder _errors = new System.Text.StringBuilder();
        public static string Errors { get { return _errors.ToString(); } }
        public static XElement xconfig = null;
        public static void SaveConfig() { xconfig.Save(path + "wwwroot/config.xml"); }
        public static void SaveAccounts() { accounts.Save(path + "wwwroot/accounts.xml"); }
        public static string OADataUri = null; 

        // Выделенный объект, это надо будет сделать как-то подругому
        public static string funds_id = null;

        public static void Init() { Init(path); }
        public static void Init(string pth)
        {
            path = pth + "/";
            //try
            //{
                if (!System.IO.File.Exists(path + "wwwroot/config.xml"))
                {
                    if (System.IO.File.Exists(path + "wwwroot/config0.xml"))
                        System.IO.File.Copy(path + "wwwroot/config0.xml", path + "wwwroot/config.xml");
                    else (new XElement("config")).Save(path + "wwwroot/config.xml");
                }
                xconfig = XElement.Load(path + "wwwroot/config.xml");
                // Работа с параметрами конфигурации
                extappbinpath = xconfig.Element("Parameters")?.Attribute("extappbinpath")?.Value;
                if (extappbinpath == null) extappbinpath = path + "wwwroot/extappbinpath/";
                newcassettesdirpath = xconfig.Element("Parameters")?.Attribute("newcassettesdirpath")?.Value;
                if (newcassettesdirpath == null) newcassettesdirpath = path + "wwwroot/newcassettesdirpath/";
                string appn = xconfig.Element("params")?.Attribute("appname")?.Value;
                if (appn != null) appname = appn;

                finfo_configdefault = xconfig.Element("finfo");
                // WARNING! Это опасно передавать параметры через статические переменные класса!!!
                if (finfo_configdefault != null) Cassette.finfo_default = finfo_configdefault;

            //storage = new DStorage();
            //storage.Init(xconfig);
            //engine = new XmlDbAdapter();
            //storage.InitAdapter(engine);

            _engine = new CassetteData.CassetteIntegration(xconfig);
                // Загрузка профиля и онтологии
            if (!System.IO.File.Exists(path + "wwwroot/ApplicationProfile.xml"))
                {
                    System.IO.File.Copy(path + "wwwroot/ApplicationProfile0.xml", path + "wwwroot/ApplicationProfile.xml");
                }
                appProfile = XElement.Load(path + "wwwroot/ApplicationProfile.xml");
                XElement ontology = XElement.Load(path + "wwwroot/ontology_iis-v12-doc_ruen.xml");
                TurgundaCommon.ModelCommon.formats = appProfile.Element("formats");
                TurgundaCommon.ModelCommon.EditRecords = appProfile.Element("EditRecords");
                TurgundaCommon.ModelCommon.LoadOntNamesFromOntology(ontology);
                TurgundaCommon.ModelCommon.LoadInvOntNamesFromOntology(ontology);

            //}
            //catch (Exception e) { Initiated = false; _errors.Append(e.Message); Log(e.Message); }
                //storage.SaveDb("C:/Home/syp_db.xml");
            try
            {
                accounts = XElement.Load(path + "wwwroot/accounts.xml");
            } catch (Exception)
            {
                accounts = new XElement("accounts");
                AccountsSave();
            }

            // Тест
            //var item = Turgunda7.SObjects.Engine.GetItemByIdBasic("syp2001-p-marchuk_a", true);
            //XElement format = new XElement("record",
            //    new XElement("field", new XAttribute("prop", "http://fogid.net/o/name")),
            //    null);
            //var item = Turgunda7.SObjects.Engine.GetItemById("syp2001-p-marchuk_a", format);

            // Заплата вычисления выделенного объекта
            string funds_name = "Фонды";
            //funds_id = Turgunda7.SObjects.Engine.SearchByName(funds_name)
            //    .FirstOrDefault(r =>
            //    {
            //        if (r.Attribute("type").Value != "http://fogid.net/o/collection") return false;
            //        var na = r.Elements("field")
            //            .FirstOrDefault(f => f.Attribute("prop").Value == "http://fogid.net/o/name" && f.Value == funds_name);
            //        if (na == null) return false;
            //        return true;
            //    })?.Attribute("id").Value;

        }

        public static void AccountsSave()
        {
            accounts.Save(path + "wwwroot/accounts.xml");
        }

        public static bool tolog = false;
        public static void Log(string message)
        {
            if (tolog)
            {
                var f = System.IO.File.AppendText(path + "wwwroot/log.txt");
                f.WriteLine($"{System.DateTime.Now.ToUniversalTime()} {message}");
                f.Close();
            }
        }
        private static XElement appProfile;

        public static void LoadFromCassettesExpress() { throw new NotImplementedException(); }

        public static CassetteInfo GetCassetteInfoById(string id)
        {
            // Короткое имя кассеты, если есть
            if (!id.EndsWith("_cassetteId")) { return null; }
            string sid = id.Substring(0, id.Length - "_cassetteId".Length).ToLower();
            string fid = "iiss://" + sid + "@iis.nsk.su";
            // Определяем кассету
            var cassetteExists = SObjects._engine.localstorage.connection.cassettesInfo.ContainsKey(fid);
            if (!cassetteExists) { return null; }
            return SObjects._engine.localstorage.connection.cassettesInfo[fid];
        }
        public static string GetPhotoFileName(string u, string s)
        {
            return _engine.localstorage.GetPhotoFileName(u, s);
        }
        public static string GetVideoFileName(string u)
        {
            return _engine.localstorage.GetVideoFileName(u);
        }
        public static string GetAudioFileName(string u)
        {
            return _engine.localstorage.GetAudioFileName(u);
        }

        public static IEnumerable<XElement> SearchByName(string searchstring)
        {
            lock (saveInDb) { return _engine.SearchByName(searchstring); }
        }
        public static XElement GetItemById(string id, XElement format)
        {
            lock (saveInDb) { return _engine.GetItemById(id, format); }
        }
        public static XElement GetItemByIdBasic(string id, bool addinverse)
        {
            lock (saveInDb) { return _engine.GetItemByIdBasic(id, addinverse); }
        }
        public static XElement GetItemByIdSpecial(string id)
        {
            lock (saveInDb) { return _engine.GetItemByIdBasic(id, true); }
        }

        // ============== Редактирование базы данных ===============
        public static XElement PutItem(XElement record)
        {
            lock (saveInDb) { return _engine.PutItem(record); }
        }

        public static string AddInvRelation(string eid, string prop, string rtype, string username)
        {
            int pos = rtype.LastIndexOf('/');
            int pos2 = prop.LastIndexOf('/');
            XElement xrecord = PutItemToDb(new XElement(XName.Get(rtype.Substring(pos + 1), rtype.Substring(0, pos + 1)),
                new XElement(XName.Get(prop.Substring(pos2 + 1), prop.Substring(0, pos2 + 1)),
                    new XAttribute(ONames.rdfresource, eid))),
                true, username);
            if (xrecord == null) return null;
            return xrecord.Attribute(ONames.rdfabout).Value;
        }

        // Возвращает идентификатор созданного айтема или null
        public static string CreateNewItem(string name, string rtype, string username)
        {
            int pos = rtype.LastIndexOf('/');
            XElement xrecord = PutItemToDb(new XElement(XName.Get(rtype.Substring(pos + 1), rtype.Substring(0, pos + 1)),
                new XElement("{http://fogid.net/o/}name", new XAttribute(ONames.xmllang, "ru"), name)),
                true, username);

            return xrecord?.Attribute(ONames.rdfabout).Value;
        }
        public static XElement Delete(string id) { return _engine.Delete(id); }
        public static XElement Add(XElement record) { return _engine.Add(record); }
        public static XElement AddUpdate(XElement record) { return _engine.AddUpdate(record); }
        public static void DeleteItem(string id, string username)
        {
            //engine.DeleteRecord(id);
            PutItemToDb(new XElement("{http://fogid.net/o/}delete", new XAttribute("id", id)),
                false, username);
        }

        public static XElement GetEditFormat(string type, string prop)
        {
            XElement res = appProfile.Element("EditRecords").Elements("record").FirstOrDefault(re => re.Attribute("type").Value == type);
            if (res == null) return null;
            XElement resu = new XElement(res);
            if (prop != null)
            {
                XElement forbidden = resu.Elements("direct").FirstOrDefault(di => di.Attribute("prop").Value == prop);
                if (forbidden != null) forbidden.Remove();
            }
            return resu;
        }
        private static object saveInDb = new object();
        /// <summary>
        /// Базовый метод. Помещает айтем, включая и операторы, в обе базы данных. Если требуется, то формирует идентификатор
        /// </summary>
        /// <param name="item">Айтем в "стандартном" виде. В записи допустимо отсуттсвие rdf:about, тогда tocreate Должен быть true</param>
        /// <param name="tocreateid">нужно ли создавать для записи новый идентификатор</param>
        /// <param name="username">идентификатор пользователя</param>
        /// <returns>скорректированный элемент, помещаенный в базы данных</returns>
        public static XElement PutItemToDb(XElement item, bool tocreateid, string username)
        {
            if (tocreateid && item.Attribute(Polar.Cassettes.ONames.rdfabout) != null) throw new Exception("Err 2982454 in SObjects.PutItemToDb");
            XElement item_corrected = null;
            lock (saveInDb)
            {
                item_corrected = new XElement(item);
                item_corrected.Add(new XAttribute("owner", username), new XAttribute("mT", DateTime.Now.ToUniversalTime().ToString("u")));
                //item_corrected = _engine.localstorage.EditCommand(item_corrected);
                if (!tocreateid)
                {
                    string id = item.Attribute(Polar.Cassettes.ONames.rdfabout).Value;
                    XElement xold = GetItemByIdBasic(id, false);
                    foreach (XElement d in xold.Elements("direct"))
                    {
                        string prop = d.Attribute("prop").Value;
                        int last_slash = prop.LastIndexOf('/');
                        string pr = prop.Substring(last_slash + 1);
                        if (item_corrected.Elements().All(e => e.Name != pr))
                        {
                            item_corrected.Add(new XElement("{" + prop.Substring(0, last_slash) + "}" + pr,
                                new XAttribute("{http://www.w3.org/1999/02/22-rdf-syntax-ns#}resource", d.Element("record").Attribute("id").Value)));
                        }
                    }
                }
                item_corrected = PutItem(item_corrected);
            }
            //Log("PutItemToDb ok?");
            return item_corrected;
        }

        public static string GetField(XElement item, string prop)
        {
            if (item == null) return ""; 
            XElement el = item.Elements("field").FirstOrDefault(f => f.Attribute("prop")?.Value == prop);
            return el == null ? "" : el.Value;
        }
        public static string GetDates(XElement item)
        {
            var fd_el = item.Elements("field").FirstOrDefault(f => f.Attribute("prop").Value == "http://fogid.net/o/from-date");
            var td_el = item.Elements("field").FirstOrDefault(f => f.Attribute("prop").Value == "http://fogid.net/o/to-date");
            string res = (fd_el == null ? "" : fd_el.Value) + (td_el == null || string.IsNullOrEmpty(td_el.Value) ? "" : "—" + td_el.Value);
            return res;
        }
        public static IEnumerable<RDFDocumentInfo> GetFogFiles1() { return _engine.localstorage.connection.GetFogFiles1(); }
        public static IEnumerable<XElement> GetCollectionPath(string id)
        {
            //return _getCollectionPath(id);
            System.Collections.Generic.Stack<XElement> stack = new Stack<XElement>();
            XElement node = _engine.GetItemByIdBasic(id, false);
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
            XElement tree = _engine.GetItemById(id, formattoparentcollection);
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
        private static XElement formattoparentcollection =
    new XElement("record",
        new XElement("inverse", new XAttribute("prop", "http://fogid.net/o/collection-item"),
            new XElement("record",
                new XElement("direct", new XAttribute("prop", "http://fogid.net/o/in-collection"),
                    new XElement("record", new XAttribute("type", "http://fogid.net/o/collection"),
                        new XElement("field", new XAttribute("prop", "http://fogid.net/o/name")))))));


    }

  
}