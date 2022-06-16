using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Linq;
//using Fogid.Cassettes;

namespace Polar.Cassettes.DocumentStorage
{
    abstract public class DbAdapter
    {
        public bool firsttime = true;
        public DS localstorage;
        public abstract void Init(string connectionstring); 
        // ============== Основные методы доступа к БД =============
        public abstract IEnumerable<XElement> SearchByName(string searchstring);
        public abstract XElement GetItemByIdBasic(string id, bool addinverse);
        public abstract XElement GetItemById(string id, XElement format);
        public abstract XElement GetItemByIdSpecial(string id);

        // ============== Загрузка базы данных ===============
        public abstract void StartFillDb(Action<string> turlog);
        public abstract void LoadFromCassettesExpress(IEnumerable<string> fogfilearr, Action<string> turlog, Action<string> convertlog);
        public abstract void FinishFillDb(Action<string> turlog);
        public abstract void Save(string filename);

        // ============== Редактирование базы данных ============= Возвращают итоговый (или исходный для Delete) вариант записи
        public abstract XElement Delete(string id);
        // Полная (для Add) или неполная (для AddUpdate) записи. Идентификатор обязателен.
        public abstract XElement Add(XElement record);
        public abstract XElement AddUpdate(XElement record);
        // Заменяет предыдущие 3. Помещает запись в базу данных, если у нее нет идентификатора, то генерирует его. Возвращает зафиксированную запись
        public abstract XElement PutItem(XElement record);

        private const string fogi = "{http://fogid.net/o/}";
        private const string fog = "http://fogid.net/o/";
        private static Func<string, string> ConvertId = id => id;
        public static Func<XElement, XElement> ConvertXElement = xel =>
        {
            if (xel.Name == "delete" || xel.Name == fogi + "delete") return new XElement(fogi + "delete",
                new XAttribute("id", ConvertId(xel.Attribute("id").Value)));
            else if (xel.Name == "substitute" || xel.Name == fogi + "substitute") return new XElement(fogi + "substitute",
                new XAttribute("old-id", ConvertId(xel.Attribute("old-id").Value)),
                new XAttribute("new-id", ConvertId(xel.Attribute("new-id").Value)));
            else
            {
                string id = ConvertId(xel.Attribute(ONames.rdfabout).Value);
                XAttribute mt_att = xel.Attribute("mT");
                XElement iisstore = xel.Element("iisstore");
                if (iisstore != null)
                {
                    var att_uri = iisstore.Attribute("uri");
                    var att_contenttype = iisstore.Attribute("contenttype");
                    string docmetainfo = iisstore.Attributes()
                        .Where(at => at.Name != "uri" && at.Name != "contenttype")
                        .Select(at => at.Name + ":" + at.Value.Replace(';', '|') + ";")
                        .Aggregate((sum, s) => sum + s);
                    //if (att_uri != null)
                    //{
                    //    string uri = att_uri.Value;
                    //    // В качестве идентификатора берем uri
                    //    var short_id = uri.Replace("://", "_").Replace("@iis.nsk.su/", "_").Replace('/', '_');
                    //    XElement xfilestore = new XElement(ONames.tag_filestore,
                    //        new XAttribute(ONames.rdfabout, short_id),
                    //        new XElement(ONames.tag_fordocument, new XAttribute(ONames.rdfresource, id)),
                    //        new XElement(ONames.tag_uri, att_uri.Value),
                    //        att_owner == null ? null : new XElement(ONames.tag_owner, att_owner.Value),
                    //        new XElement(ONames.tag_contenttype, att_contenttype.Value),
                    //        new XElement(ONames.tag_docmetainfo, docmetainfo),
                    //        null);
                    //    return xfilestore;
                    //}
                    iisstore.Remove();
                    if (att_uri != null) xel.Add(new XElement("uri", att_uri.Value));
                    if (att_contenttype != null) xel.Add(new XElement("contenttype", att_contenttype.Value));
                    if (docmetainfo != "") xel.Add(new XElement("docmetainfo", docmetainfo));
                }
                XElement xel1 = new XElement(XName.Get(xel.Name.LocalName, fog),
                    new XAttribute(ONames.rdfabout, ConvertId(xel.Attribute(ONames.rdfabout).Value)),
                    mt_att == null ? null : new XAttribute("mT", mt_att.Value),
                    xel.Elements()
                    .Where(sub => sub.Name.LocalName != "iisstore")
                    .Select(sub => new XElement(XName.Get(sub.Name.LocalName, fog),
                        sub.Value,
                        sub.Attributes()
                        .Select(att => att.Name == ONames.rdfresource ?
                            new XAttribute(ONames.rdfresource, ConvertId(att.Value)) :
                            new XAttribute(att)))));
                return xel1;
            }
            //return null;
        };

        // ================= Раздел работы по цепочкам эквивалентности ==============
        protected Dictionary<string, ResInfo> table_ri;
        public void InitTableRI() { table_ri = new Dictionary<string, ResInfo>(); count_delete = count_substitute = 0; }
        public int count_delete = 0, count_substitute = 0;
        public void AppendXflowToRiTable(IEnumerable<XElement> xflow, string ff, Action<string> err)
        {
            foreach (XElement xelement in xflow)
            {
                if (xelement.Name == fogi + "delete")
                {
                    count_delete++;
                    XAttribute att = xelement.Attribute("id");
                    if (att == null) continue;
                    string id = att.Value;
                    if (id == "") continue;
                    if (table_ri.ContainsKey(id))
                    {
                        var ri = table_ri[id];
                        if (!ri.removed) // Если признак уже есть, то действия уже произведены
                        {
                            // проверим, что уничтожается оригинал цепочки
                            if (!(id == ri.id))
                            {
                                err("Уничтожается не оригинал цепочки. fog=" +
                                    ff + " id=" + id);
                            }
                            else ri.removed = true;
                        }
                    }
                    else
                    {
                        table_ri.Add(id, new ResInfo(id) { removed = true });
                    }
                }
                else if (xelement.Name == fogi + "substitute")
                {
                    count_substitute++;
                    XAttribute att_old = xelement.Attribute("old-id");
                    XAttribute att_new = xelement.Attribute("new-id");
                    if (att_old == null || att_new == null) continue;
                    string id_old = att_old.Value;
                    string id_new = att_new.Value;
                    if (id_old == "" || id_new == "") continue;

                    // Добудем старый и новый ресурсы
                    //ResInfo old_res, new_res;
                    if (!table_ri.TryGetValue(id_old, out ResInfo old_res))
                    {
                        old_res = new ResInfo(id_old);
                        table_ri.Add(id_old, old_res);
                    }
                    if (!table_ri.TryGetValue(id_new, out ResInfo new_res))
                    {
                        new_res = new ResInfo(id_new);
                        table_ri.Add(id_new, new_res);
                    }
                    // Проверим, что old-id совпадает с оригиналом локальной цепочки
                    if (id_old != old_res.id)
                    {
                        //LogFile.WriteLine("Разветвление на идентификаторе: " + id_old);
                    }
                    // Перенесем тип из старой цепочки в новый ресурс
                    if (new_res.typeid == null) new_res.typeid = old_res.typeid;
                    else
                    {
                        // Проверим, что цепочки одинакового типа
                        if (old_res.typeid != null && old_res.typeid != new_res.typeid)
                        {
                            err("Err: сливаются цепочки разных типов");
                        }
                    }

                    // добавляем список слитых старых идентификаторов в новый ресурс
                    new_res.merged_ids.AddRange(old_res.merged_ids);
                    // пробегаем по списку старых идентификаторов и перекидываем ссылку на новый ресурс
                    foreach (string oid in old_res.merged_ids) table_ri[oid] = new_res;
                    // перекидываем признак removed из старого ресурса, если он там true.
                    if (old_res.removed)
                    {
                        // Похоже, следующий оператор ошибка. Мы "протягиваем" условие removed
                        //new_res.removed = true;
                        err("Протяжка удаления по цепочке. id=" + id_old);
                    }
                }
                else
                {
                    XAttribute idAtt = xelement.Attribute(ONames.rdfabout);
                    if (idAtt == null) continue;
                    string id = idAtt.Value;
                    if (id == "") continue;

                    // 
                    if (table_ri.ContainsKey(id))
                    {
                        var ri = table_ri[id];
                        DateTime modificationTime = DateTime.MinValue;
                        XAttribute mt = xelement.Attribute("mT");
                        if (mt != null &&
                            DateTime.TryParse(mt.Value, out modificationTime) &&
                            modificationTime.ToUniversalTime() > ri.timestamp)
                        {
                            // Установим эту временную отметку
                            ri.timestamp = modificationTime.ToUniversalTime();
                        }
                        //if (xelement.Name != sema2012m.ONames.rdfdescription) // rdf:Description не несет информации о типе записи
                        //{
                            if (ri.typeid == null) ri.typeid = xelement.Name.NamespaceName + xelement.Name.LocalName;
                            else
                            {
                                // проверка на одинаковость типов
                                if (xelement.Name.NamespaceName + xelement.Name.LocalName != ri.typeid)
                                {
                                    err("Err: тип " + xelement.Name + " для ресурса " + idAtt + " не соответствует ранее определенному типу");
                                }
                            }
                        //}
                    }
                    else
                    { // Это вариант, когда входа еще нет в таблице
                        DateTime modificationTime = DateTime.MinValue;
                        XAttribute mt = xelement.Attribute("mT");
                        if (mt != null)
                            DateTime.TryParse(mt.Value, out modificationTime);
                        var n_resinfo = new ResInfo(id)
                        {
                            removed = false,
                            timestamp = modificationTime.ToUniversalTime()
                            //, typeid = xelement.Name.NamespaceName + xelement.Name.LocalName
                        };
                        //if (xelement.Name != sema2012m.ONames.rdfdescription)
                            n_resinfo.typeid = xelement.Name.NamespaceName + xelement.Name.LocalName;
                        table_ri.Add(id, n_resinfo);
                    }

                }
            }
        }
        public abstract void LoadXFlowUsingRiTable(IEnumerable<XElement> xflow);


    }
    public class ResInfo
    {
        public ResInfo(string id) { this.id = id; this.merged_ids = new List<string> { id }; }
        public string id; // этот идентификатор и есть последний
        public string typeid; // тип ресурса (сущности, записи) или null если не известно
        public bool removed = false;
        public List<string> merged_ids;
        public DateTime timestamp = DateTime.MinValue;
        // Это поле вводится для того, чтобы повторно не обрабатывать определение записи
        public bool processed = false;
    }

}
