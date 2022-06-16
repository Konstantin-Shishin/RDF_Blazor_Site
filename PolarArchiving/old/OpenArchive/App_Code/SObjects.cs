using System;
using System.Linq;
using System.Xml.Linq;
using System.Collections.Generic;

using Polar.Cassettes;
using Polar.Cassettes.DocumentStorage;

namespace OpenArchive
{
    public class SObjects
    {
        private static string path = null;
        internal static DStorage storage = null;
        internal static DbAdapter engine = null;
        public static void Init() { Init(path); }
        public static void Init(string pth)
        {
            path = pth + "/";
            XElement xconfig = XElement.Load(path + "config.xml");
            storage = new DStorage();
            storage.Init(xconfig);
            engine = new XmlDbAdapter();
            storage.InitAdapter(engine);

            //// Загрузка профиля и онтологии
            //appProfile = XElement.Load(path + "wwwroot/ApplicationProfile.xml");
            //XElement ontology = XElement.Load(path + "wwwroot/ontology_iis-v12-doc_ruen.xml");
            //Models.Common.formats = appProfile.Element("formats");
            //Models.Common.LoadOntNamesFromOntology(ontology);
            //Models.Common.LoadInvOntNamesFromOntology(ontology);

            storage.LoadFromCassettesExpress(); // Штатно, это выполняется по специальному запросу LoadFromCassettesExpress(), такой вариант годится для динамического формирования базы данных, напр. движком engine = new XmlDbAdapter(); 
            //storage.SaveDb("C:/Home/syp_db.xml");
        }
        //private static XElement appProfile;

        //public static void LoadFromCassettesExpress() { throw new NotImplementedException(); }

        public static IEnumerable<XElement> SearchByName(string searchstring) { return engine.SearchByName(searchstring); }
        //public static string GetType(string id) { return adapter.GetType(id); }
        public static XElement GetItemById(string id, XElement format) { return engine.GetItemById(id, format); }
        public static XElement GetItemByIdSpecial(string id) { return engine.GetItemByIdBasic(id, true); }

        //// ============== Редактирование базы данных ===============
        //public static string AddInvRelation(string eid, string prop, string rtype, string username)
        //{
        //    int pos = rtype.LastIndexOf('/');
        //    int pos2 = prop.LastIndexOf('/');
        //    XElement xrecord = PutItemToDb(new XElement(XName.Get(rtype.Substring(pos + 1), rtype.Substring(0, pos + 1)),
        //        new XElement(XName.Get(prop.Substring(pos2 + 1), prop.Substring(0, pos2 + 1)),
        //            new XAttribute(ONames.rdfresource, eid))),
        //        true, username);
        //    if (xrecord == null) return null;
        //    return xrecord.Attribute(ONames.rdfabout).Value;
        //}

        //// Возвращает идентификатор созданного айтема или null
        //public static string CreateNewItem(string name, string rtype, string username)
        //{
        //    int pos = rtype.LastIndexOf('/');
        //    XElement xrecord = PutItemToDb(new XElement(XName.Get(rtype.Substring(pos + 1), rtype.Substring(0, pos + 1)),
        //        new XElement("{http://fogid.net/o/}name", new XAttribute(ONames.xmllang, "ru"), name)),
        //        true, username);

        //    return xrecord.Attribute(ONames.rdfabout).Value;
        //}
        //public static void DeleteItem(string id, string username)
        //{
        //    //engine.DeleteRecord(id);
        //    PutItemToDb(new XElement("{http://fogid.net/o/}delete", new XAttribute("id", id)),
        //        false, username);
        //}

        //public static XElement GetEditFormat(string type, string prop)
        //{
        //    XElement res = appProfile.Element("EditRecords").Elements("record").FirstOrDefault(re => re.Attribute("type").Value == type);
        //    if (res == null) return null;
        //    XElement resu = new XElement(res);
        //    if (prop != null)
        //    {
        //        XElement forbidden = resu.Elements("direct").FirstOrDefault(di => di.Attribute("prop").Value == prop);
        //        if (forbidden != null) forbidden.Remove();
        //    }
        //    return resu;
        //}
        //private static object saveInDb = new object();
        ///// <summary>
        ///// Базовый метод. Помещает айтем, включая и операторы, в обе базы данных. Если требуется, то формирует идентификатор
        ///// </summary>
        ///// <param name="item">Айтем в "стандартном" виде. В записи допустимо отсуттсвие rdf:about, тогда tocreate Должен быть true</param>
        ///// <param name="tocreateid">нужно ли создавать для записи новый идентификатор</param>
        ///// <param name="username">идентификатор пользователя</param>
        ///// <returns>скорректированный элемент, помещаенный в базы данных</returns>
        //public static XElement PutItemToDb(XElement item, bool tocreateid, string username)
        //{
        //    XElement item_corrected = new XElement(item);
        //    item_corrected.Add(new XAttribute("owner", username), new XAttribute("mT", DateTime.Now.ToUniversalTime().ToString("u")));
        //    lock (saveInDb)
        //    {
        //        item_corrected = storage.EditCommand(item_corrected);
        //    }
        //    return item_corrected;
        //}
    }
}