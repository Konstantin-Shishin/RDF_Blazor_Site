using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml.Linq;
using Polar.TripleStore;

namespace Polar.Cassettes.DocumentStorage
{
    /// <summary>
    /// База данных на основе TripleStore-построения. База данных объединяет записи fog-документов
    /// без дублирования и цепочек эквивалентности. 
    /// </summary>
    public class TripleStoreAdapter : DbAdapter
    {
        //private class DbNode
        //{
        //    public string id; // Нужен поскольку определяющая запись может быть отсутствовать
        //    public XElement xel;
        //    public List<XElement> inverse;
        //}
        //private XElement db = null;

        // ========= Носитель базы данных ==========
        //private Dictionary<string, DbNode> records = null;
        private Polar.TripleStore.TripleStoreInt32 store;

        private Action<string> errors = s => { Console.WriteLine(s); };
        string dbfolder = "D:/Home/data/Databases/";
        int file_no = 0;

        /// <summary>
        /// Инициирование базы данных
        /// </summary>
        /// <param name="connectionstring">префикс варианта базы данных ts:, после этого - директория для оператиыной БД</param>
        public override void Init(string connectionstring)
        {
            if (connectionstring != null && connectionstring.StartsWith("ts:"))
            {
                dbfolder = connectionstring.Substring(3);
            }
            Func<Stream> GenStream = () =>
                new System.IO.FileStream(dbfolder + (file_no++) + ".bin", FileMode.OpenOrCreate, FileAccess.ReadWrite);
            store = new TripleStore.TripleStoreInt32(GenStream, dbfolder);
        }
        // Загрузка базы данных
        public override void StartFillDb(Action<string> turlog)
        {
            //db = new XElement("db");
            store.Clear();
        }
        public override void LoadFromCassettesExpress(IEnumerable<string> fogfilearr, Action<string> turlog, Action<string> convertlog)
        {
            // Нужно для чтиния в кодировке windows-1251. Нужен также Nuget System.Text.Encoding.CodePages
            var v = CodePagesEncodingProvider.Instance;
            Encoding.RegisterProvider(v);
            //var v = System.Text.Encoding.CodePagesEncodingProvider.Instance;
            System.Text.Encoding.RegisterProvider(v);

            InitTableRI();
            foreach (string filename in fogfilearr)
            {
                XElement fog = XElement.Load(filename);
                var xflow = fog.Elements()
                    .Select(el => ConvertXElement(el));
                AppendXflowToRiTable(xflow, filename, errors);
            }
            // Использование таблицы
            foreach (string filename in fogfilearr)
            {
                XElement fog = XElement.Load(filename);
                var xflow = fog.Elements()
                    .Select(el => ConvertXElement(el));
                LoadXFlowUsingRiTable(xflow);
            }

        }
        public override void Save(string filename)
        {
            //db.Save(filename);
            //store.Flush(); //TODO:
        }

        public override void LoadXFlowUsingRiTable(IEnumerable<XElement> xflow)
        {
            int a_prop = store.CodeEntity(ONames.rdftype.NamespaceName + ONames.rdftype.LocalName);
            
            foreach (XElement xel in xflow)
            {
                if (xel.Name == "{http://fogid.net/o/}delete" || xel.Name == "{http://fogid.net/o/}substitute") continue;
                var aboutatt = xel.Attribute(Cassettes.ONames.rdfabout);
                if (aboutatt == null) continue;
                string id = aboutatt.Value;
                // выявим Resource Info
                if (!table_ri.TryGetValue(id, out ResInfo ri)) continue; // если нет, то отбрасываем вариант (это на всякий случай)
                if (ri.removed) continue; // пропускаем уничтоженные
                if (ri.id != id || ri.processed) continue; // это не оригинал или запись уже обрабатывалась
                // Выявляем временную отметку
                DateTime modificationTime_new = DateTime.MinValue;
                XAttribute mt = xel.Attribute("mT");
                if (mt != null) DateTime.TryParse(mt.Value, out modificationTime_new);
                modificationTime_new = modificationTime_new.ToUniversalTime();

                if (modificationTime_new != ri.timestamp) continue; // отметка времени не совпала

                // Зафиксируем в базе данных
                //db.Add(xel); // старый вариант

                string ent = store.DecodeEntity(1);
                int rec_type = store.CodeEntity("http://fogid.net/o/" + xel.Name.LocalName);
                int id_ent = store.CodeEntity(id);
                IEnumerable<object> query = (new object[] { new object[] { id_ent, a_prop, new object[] { 1, rec_type } } })
                    .Concat(xel.Elements().Select(subel => // новый вариант
                        {
                            var prop = store.CodeEntity(subel.Name.NamespaceName + subel.Name.LocalName);
                            var resource = subel.Attribute(ONames.rdfresource);
                            if (resource == null)
                            {
                                return new object[] { id_ent, prop, new object[] { 2, subel.Value } };
                            }
                            else
                            {
                                int target = store.CodeEntity(resource.Value);
                                return new object[] { id_ent, prop, new object[] { 1, target } };
                            }

                        }));
                store.Load(query);

                ri.processed = true; // Это на случай, если будет второй оригинал, он обрабатываться не будет.
            }
        }


        public override void FinishFillDb(Action<string> turlog)
        {
            //records = new Dictionary<string, DbNode>();

            //foreach (XElement xel in db.Elements())
            //{
            //    // Это есть запись, которую надо зафиксировать под ее именем
            //    string id = xel.Attribute(Cassettes.ONames.rdfabout).Value;
            //    if (records.TryGetValue(id, out DbNode node))
            //    { // есть определение
            //        node.xel = xel;
            //    }
            //    else
            //    {
            //        records.Add(id, new DbNode() { id = id, xel = xel, inverse = new List<XElement>() });
            //    }
            //    // Теперь проанализируем прямые ссылки
            //    foreach (XElement el in xel.Elements())
            //    {
            //        XAttribute res_att = el.Attribute(Cassettes.ONames.rdfresource);
            //        if (res_att == null) continue;
            //        string resource_id = res_att.Value;
            //        if (records.TryGetValue(resource_id, out node))
            //        { // есть определение
            //            node.inverse.Add(el);
            //        }
            //        else
            //        {
            //            records.Add(resource_id,
            //                new DbNode()
            //                {
            //                    id = resource_id,
            //                    inverse = Enumerable.Repeat<XElement>(el, 1).ToList()
            //                });
            //        }
            //    }
            //}
            // по-новому
            store.Build();
        }

        public override IEnumerable<XElement> SearchByName(string searchstring)
        {
            var query2 = SearchByName2(searchstring);
            return query2;
        }
        public IEnumerable<XElement> SearchByName2(string searchstring)
        {
            string ss = searchstring == null ? "" : searchstring.ToLower();
            int a_code = store.CodeEntity(ONames.rdftype.NamespaceName + ONames.rdftype.LocalName);
            var query = store.Like(ss)
                .Cast<object[]>()
                .Select(el =>
                {
                    string id = store.DecodeEntity((int)el[0]);
                    string name = (string)((object[])el[2])[1];
                    var qu = store.Get_s((int)el[0]);;
                    object[] a_triple = (object[])qu.FirstOrDefault(ob => 
                    {
                        object[] tri = (object[])ob;
                        int pred = (int)tri[1];
                        return pred == a_code;
                    });
                    string type = a_triple == null ? "notype" : store.DecodeEntity((int)((object[])a_triple[2])[1]);
                    return new XElement("record",
                        new XAttribute("id", id),
                        new XAttribute("type", type),
                        new XElement("field",
                            new XAttribute("prop", "http://fogid.net/o/" + ONames.TagName.LocalName),
                            name));
                });
            return query;
        }
        public XElement GetItemByIdBasic2(string id, bool addinverse)
        {
            XElement record = null;
            var query = store.Get_s(id);
            string type = null;
            // сканируем чтобы заполнить переменную type
            foreach (object[] tr in query)
            {
                if ((int)tr[1] == store.Code_rdftype && type == null)
                    type = store.DecodeEntity((int)((object[])tr[2])[1]);
            }
            record = new XElement("record",
                new XAttribute("id", id),
                new XAttribute("type", type),
                query.Cast<object[]>()
                .Where(tr => (int)tr[1] != store.Code_rdftype) // пропустить определение типа
                .Select(tr =>
                {
                    string prop = store.DecodeEntity((int)tr[1]);
                    object[] ob = (object[])tr[2];
                    if ((int)ob[0] == 1)
                        return new XElement("direct",
                            new XAttribute("prop", prop),
                            new XElement("record", new XAttribute("id", store.DecodeEntity((int)ob[1]))));
                    else
                        return new XElement("field",
                            new XAttribute("prop", prop), (string)ob[1]);
                }));
            if (addinverse)
            {
                var query_inverse = store.Get_t(id);
                record.Add(
                    query_inverse.Cast<object[]>()
                    .Select(tr =>
                    {
                        string prop = store.DecodeEntity((int)tr[1]);
                        return new XElement("inverse",
                            new XAttribute("prop", prop),
                            new XElement("record", new XAttribute("id", store.DecodeEntity((int)tr[0]))));
                    }));
            }
            return record;
        }
        public override XElement GetItemByIdBasic(string id, bool addinverse)
        {
            var query2 = GetItemByIdBasic2(id, addinverse);
            return query2;
        }
        private XElement CodeTree(XElement tree)
        {
            XElement rformat = new XElement(tree);
            CodeAttributes(rformat);
            return rformat;
        }
        private XElement DecodeTree(XElement itree)
        {
            XElement rformat = new XElement(itree);
            DecodeAttributes(rformat);
            return rformat;
        }
        private void CodeAttributes(XElement tree)
        {
            CodeAtt(tree, "id");
            CodeAtt(tree, "type");
            CodeAtt(tree, "prop");
            foreach (XElement el in tree.Elements()) CodeAttributes(el);
        }
        private void DecodeAttributes(XElement itree)
        {
            DecodeAtt(itree, "id");
            DecodeAtt(itree, "type");
            DecodeAtt(itree, "prop");
            foreach (XElement el in itree.Elements()) DecodeAttributes(el);
        }
        private void CodeAtt(XElement format, string att_name)
        {
            var att = format.Attribute(att_name);
            if (att != null)
            {
                string val = att.Value;
                att.Value = "" + store.CodeEntity(val);
            }
        }
        private void DecodeAtt(XElement format, string att_name)
        {
            var att = format.Attribute(att_name);
            if (att != null)
            {
                string val = att.Value;
                att.Value = store.DecodeEntity(Int32.Parse(val));
            }
        }

        public override XElement GetItemById(string id, XElement format)
        {
            // Кодирование идентификатора и формата
            int iid = store.CodeEntity(id);
            XElement iformat = CodeTree(format);
            XElement x2 = GetItemByNode2(iid, store.Get_s(id), iformat);
            // Декодирование результата
            XElement x3 = DecodeTree(x2);
            return x3;
        }
        /// <summary>
        /// Рекурсивная процедура получения дерева результата в кодированной форме. Параметры: id и format кодированы.
        /// </summary>
        /// <param name="id"></param>
        /// <param name="dtriples">Прямые свойства</param>
        /// <param name="format"></param>
        /// <returns></returns>
        private XElement GetItemByNode2(int id, IEnumerable<object> dtriples,  XElement format)
        {
            int type = Int32.MinValue;
            foreach (object[] tr in dtriples)
                if ((int)tr[1] == store.Code_rdftype)
                    type = (int)((object[])tr[2])[1];
            return new XElement("record",
                new XAttribute("id", id),
                new XAttribute("type", type),
                format.Elements().Where(fel => fel.Name == "field" || fel.Name == "direct" || fel.Name == "inverse")
                .SelectMany(fel =>
                {
                    int iprop = Int32.Parse(fel.Attribute("prop").Value);
                    if (fel.Name == "field")
                    {
                        return dtriples.Cast<object[]>()
                            .Where(tr => (int)tr[1] == iprop)
                            .Select(tr => new XElement("field", new XAttribute("prop", iprop),
                                (string)((object[])tr[2])[1]));
                    }
                    else if (fel.Name == "direct")
                    {
                        return dtriples.Cast<object[]>()
                            .Where(tr => (int)tr[1] == iprop)
                            .Select(tr =>
                            {
                                int iresource = (int)((object[])tr[2])[1];
                                var sub_dtriples = store.Get_s(iresource);
                                var tt = sub_dtriples.Cast<object[]>()
                                    .FirstOrDefault(tri => (int)tri[1] == store.Code_rdftype);
                                if (tt == null) return null;
                                XElement f = fel.Elements("record")
                                    .FirstOrDefault(fr =>
                                    {
                                        XAttribute t_att = fr.Attribute("type");
                                        if (t_att == null) return true;
                                        return Int32.Parse(t_att.Value) == (int)((object[])tt[2])[1];
                                    });
                                if (f == null) return null;
                                return new XElement("direct", new XAttribute("prop", iprop),
                                    GetItemByNode2(iresource, sub_dtriples, f));
                            });
                    }
                    else if (fel.Name == "inverse")
                    {
                        var itriples = store.Get_t(id);
                        var query = itriples.Cast<object[]>()
                            .Where(tr => (int)tr[1] == iprop)
                            .Select(tr =>
                            {
                                int subject = (int)tr[0];
                                var sub_dtriples = store.Get_s(subject);
                                var tt = sub_dtriples.Cast<object[]>()
                                    .FirstOrDefault(tri => (int)tri[1] == store.Code_rdftype);
                                // tt не должен быть нулем 
                                if (tt == null) throw new Exception("Err in GetItemByNode2: sub format " + fel);
                                // находим подходящий формат записи
                                XElement f = fel.Elements("record")
                                    .FirstOrDefault(fr =>
                                    {
                                        XAttribute t_att = fr.Attribute("type");
                                        if (t_att == null) return true;
                                        return Int32.Parse(t_att.Value) == (int)((object[])tt[2])[1];
                                    });
                                var x = new XElement("inverse",
                                    new XAttribute("prop", iprop),
                                    GetItemByNode2(subject, sub_dtriples, f));
                                return x;
                            });
                        return query;
                    }
                    else return new XElement[0];
                }));
        }

        public override XElement GetItemByIdSpecial(string id)
        {
            return GetItemByIdBasic(id, true);
        }


        private XElement RemoveRecord(string id)
        {
            //if (records.TryGetValue(id, out DbNode node))
            //{
            //    // Удаляемый объект состоит из записи и из элементов-ссылок, попавших в "чужие" списки
            //    // Сначала "работаем" по чужим спискам
            //    XElement xel = node.xel;
            //    foreach (XElement el in xel.Elements())
            //    {
            //        XAttribute resource = el.Attribute(Cassettes.ONames.rdfresource);
            //        if (resource == null) continue;
            //        // находим список
            //        string id2 = resource.Value;
            //        if (records.TryGetValue(id2, out DbNode node2))
            //        {
            //            // Уберем из списка
            //            node2.inverse.Remove(el);
            //        }
            //    }
            //    // Открепляем из базы данных
            //    xel.Remove();
            //    return xel;
            //}
            //return null;
            throw new Exception("Unimplemented method RemoveRecord");
        }
        public override XElement Delete(string id)
        {
            //XElement record = RemoveRecord(id);
            //if (record == null) return null;
            //records.Remove(id);
            //return record;
            throw new Exception("Unimplemented method Delete");
        }

        public override XElement Add(XElement record)
        {
            //// Работаем по "чужим спискам
            //XElement xel = new XElement(record);
            //foreach (XElement el in xel.Elements())
            //{
            //    XAttribute resource = el.Attribute(Cassettes.ONames.rdfresource);
            //    if (resource == null) continue;
            //    // находим список
            //    string id2 = resource.Value;
            //    if (records.TryGetValue(id2, out DbNode node2))
            //    {
            //        // добавляем в список
            //        node2.inverse.Add(el);
            //    }
            //}

            //// Прикрепляем (зачем-то) запись к базе данных
            //db.Add(xel);
            //// Добавляем к словарю
            //string id = record.Attribute(Cassettes.ONames.rdfabout).Value;

            //if (records.TryGetValue(id, out DbNode node))
            //{ // Если узел есть, то прикрепляем к полю узла
            //    node.xel = xel;
            //}
            //else
            //{ // Если узла нет, то создаем
            //    records.Add(id, new DbNode() { id = id, xel = xel, inverse = new List<XElement>() });
            //}
            //return xel;
            throw new Exception("Unimplemented method Add");
        }

        public override XElement AddUpdate(XElement record)
        {
            //string id = record.Attribute(Cassettes.ONames.rdfabout).Value;
            //if (records.TryGetValue(id, out DbNode node))
            //{
            //    // Будем перебирать элементы "старого" значения и, если таких нет, добавлять в новое
            //    foreach (XElement old_el in node.xel.Elements())
            //    {
            //        XAttribute l_att = old_el.Attribute("{http://www.w3.org/XML/1998/namespace}lang");
            //        if (record.Elements().Any(el => el.Name == old_el.Name &&
            //            (l_att == null ? true :
            //                (el.Attribute("{http://www.w3.org/XML/1998/namespace}lang") != null &&
            //                    l_att.Value == el.Attribute("{http://www.w3.org/XML/1998/namespace}lang").Value)))) continue;
            //        record.Add(old_el);
            //    }
            //    RemoveRecord(id);
            //}
            //Add(record);
            //return record;
            throw new Exception("Unimplemented method AddUpdate");
        }

        public override XElement PutItem(XElement record)
        {
            throw new NotImplementedException();
        }
    }
}
