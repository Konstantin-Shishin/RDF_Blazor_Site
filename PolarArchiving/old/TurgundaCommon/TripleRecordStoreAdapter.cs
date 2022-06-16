using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml.Linq;

using Polar.Cassettes;
using Polar.Cassettes.DocumentStorage;
//using Polar.TripleStore;

namespace Polar.TripleStore
{
    /// <summary>
    /// База данных на основе TripleStore-построения. База данных объединяет записи fog-документов
    /// без дублирования и цепочек эквивалентности. 
    /// </summary>
    public class TripleRecordStoreAdapter : DbAdapter
    {
        // ========= Носитель базы данных ==========
        private TripleRecordStore store;

        private Action<string> errors = s => { Console.WriteLine(s); };
        string dbfolder = "D:/";
        int file_no = 0;
        //internal bool firsttime = false; // Отмечает (вычисляет) ситуацию, когда базу данных обязательно нужно строить.

        /// <summary>
        /// Инициирование базы данных
        /// </summary>
        /// <param name="connectionstring">префикс варианта базы данных ts:, после этого - директория для оператиыной БД</param>
        public override void Init(string connectionstring)
        {
            if (connectionstring != null && connectionstring.StartsWith("trs:"))
            {
                dbfolder = connectionstring.Substring(4);
            }
            if (File.Exists(dbfolder + "0.bin")) firsttime = false;
            Func<Stream> GenStream = () =>
                new FileStream(dbfolder + (file_no++) + ".bin", FileMode.OpenOrCreate, FileAccess.ReadWrite);
            store = new TripleRecordStore(GenStream, dbfolder, new string[] {
                "http://www.w3.org/1999/02/22-rdf-syntax-ns#type"
                , "http://fogid.net/o/name"
                , "http://fogid.net/o/age"
                , "http://fogid.net/o/person"
                , "http://fogid.net/o/photo-doc"
                , "http://fogid.net/o/reflection"
                , "http://fogid.net/o/reflected"
                , "http://fogid.net/o/in-doc"
            });
            if (!firsttime) store.DynaBuild();            
            store.Flush();
        }
        // Загрузка базы данных
        public override void StartFillDb(Action<string> turlog)
        {
            //db = new XElement("db");
            //store.Clear();
        }
        public override void LoadFromCassettesExpress(IEnumerable<string> fogfilearr, Action<string> turlog, Action<string> convertlog)
        {
            store.Clear();

            // Нужно для чтиния в кодировке windows-1251. Нужен также Nuget System.Text.Encoding.CodePages
            var v = CodePagesEncodingProvider.Instance;
            Encoding.RegisterProvider(v);
            //var v = System.Text.Encoding.CodePagesEncodingProvider.Instance;
            System.Text.Encoding.RegisterProvider(v);

            // Готовим битовый массив для отметки того, что хеш id уже "попадал" в этот бит
            int ba_volume = 1024 * 1024;
            int mask = ~((-1) << 20);
            System.Collections.BitArray bitArr = new System.Collections.BitArray(ba_volume);
            // Хеш-функция будет ограничена 20-ю разрядами, массив и функция нужны только временно
            Func<string, int> Hash = s => s.GetHashCode() & mask;
            // Словарь
            Dictionary<string, DateTime> lastDefs = new Dictionary<string, DateTime>();
            int cnt = 0;
            // Первый проход сканирования фог-файлов
            foreach (string fog_name in fogfilearr)
            {
                // Чтение фога
                XElement fog = XElement.Load(fog_name);
                // Перебор записей
                foreach (XElement rec in fog.Elements())
                {
                    XElement record = ConvertXElement(rec);
                    cnt++;
                    if (record.Name == "delete"
                        || record.Name == "{http://fogid.net/o/}delete"
                        || record.Name == "substitute"
                        || record.Name == "{http://fogid.net/o/}substitute"
                        ) continue;
                    string id = record.Attribute("{http://www.w3.org/1999/02/22-rdf-syntax-ns#}about").Value;
                    int code = Hash(id);
                    if (bitArr.Get(code))
                    {
                        // Добавляем пару в словарь
                        XAttribute mT_att = record.Attribute("mT");
                        DateTime mT = DateTime.MinValue;
                        if (mT_att != null) { mT = DateTime.Parse(mT_att.Value); }
                        if (lastDefs.TryGetValue(id, out DateTime last))
                        {
                            if (mT > last)
                            {
                                lastDefs.Remove(id);
                                lastDefs.Add(id, mT);
                            }
                        }
                        else
                        {
                            lastDefs.Add(id, mT);
                        }
                    }
                    else
                    {
                        bitArr.Set(code, true);
                    }
                }
            }

            cnt = 0;

            // Второй проход сканирования фог-файлов
            IEnumerable<object> objectFlow = fogfilearr.SelectMany(fog_name =>
            {
                XElement fog = XElement.Load(fog_name);
                // Перебор записей
                return fog.Elements()
                .Where(rec => rec.Name != "delete"
                        && rec.Name != "{http://fogid.net/o/}delete"
                        && rec.Name != "substitute"
                        && rec.Name != "{http://fogid.net/o/}substitute")
                .Select(rec => ConvertXElement(rec))
                .Where(record =>
                {
                    string id = record.Attribute("{http://www.w3.org/1999/02/22-rdf-syntax-ns#}about").Value;
                    //int code = Hash(id);

                    bool toprocess = false;
                    if (lastDefs.TryGetValue(id, out DateTime saved))
                    {
                        DateTime mT = DateTime.MinValue;
                        XAttribute mT_att = record.Attribute("mT");
                        if (mT_att != null) { mT = DateTime.Parse(mT_att.Value); }
                        // Оригинал если отметка времени больше или равна
                        if (mT >= saved)
                        {
                            // сначала обеспечим приоритет перед другими решениями
                            lastDefs.Remove(id);
                            lastDefs.Add(id, mT);
                            // оригинал!
                            // Обрабатываем!
                            toprocess = true;
                        }
                    }
                    else
                    {
                        // это единственное определение!
                        // Обрабатываем!
                        toprocess = true;
                    }
                    return toprocess;
                })
                .Select(record =>
                {
                    string id = record.Attribute("{http://www.w3.org/1999/02/22-rdf-syntax-ns#}about").Value;
                    int rec_type = store.CodeEntity("http://fogid.net/o/" + record.Name.LocalName);
                    int id_ent = store.CodeEntity(id);
                    object[] orecord = new object[] {
                        id_ent,
                        (new object[] { new object[] { 0, rec_type } }).Concat(
                        record.Elements().Where(el => el.Attribute(ONames.rdfresource) != null)
                            .Select(subel =>
                            {
                                int prop = store.CodeEntity(subel.Name.NamespaceName + subel.Name.LocalName);
                                return new object[] { prop, store.CodeEntity(subel.Attribute(ONames.rdfresource).Value) };
                            })).ToArray()
                        ,
                        record.Elements().Where(el => el.Attribute(ONames.rdfresource) == null)
                            .Select(subel =>
                            {
                                int prop = store.CodeEntity(subel.Name.NamespaceName + subel.Name.LocalName);
                                return new object[] { prop, subel.Value };
                            })
                            .ToArray()
                    };
                    return orecord;
                });
            });

            store.Load(objectFlow);

            store.Build();

            store.Flush();
            GC.Collect();
        }
        public override void Save(string filename)
        {
            //db.Save(filename);
            //store.Flush(); //TODO:
        }

        public override void LoadXFlowUsingRiTable(IEnumerable<XElement> xflow)
        {
            throw new NotImplementedException("in LoadXFlowUsingRiTable");
        }


        public override void FinishFillDb(Action<string> turlog)
        {
            //store.Build();
        }

        private string GetRecordId(object rec)
        {
            var tri = (object[])rec;
            return store.Decode((int)tri[0]);
        }
        private string GetRecordType(object rec)
        {
            var tri = (object[])rec;
            object[] tduple = (object[])(((object[])tri[1]).FirstOrDefault(pair => (int)((object[])pair)[0] == 0));
            if (tduple == null) return null;
            return store.DecodeEntity((int)tduple[1]);
        }
        private string GetRecordName(object rec)
        {
            var tri = (object[])rec;
            return (string)((object[])((object[])tri[2]).Cast<object[]>().FirstOrDefault(pair => (int)pair[0] == 1))[1];
        }

        // =========================== Основные выборки и поиски ===============================

        public override XElement GetItemByIdBasic(string id, bool addinverse)
        {
            int cid = store.CodeEntity(id);
            object[] tri = (object[])store.GetRecord(cid);
            if (tri == null) return null;
            object[] pair = ((object[])tri[1]).Cast<object[]>().FirstOrDefault(dup => (int)dup[0] == 0);
            XElement xres = new XElement("record",
                new XAttribute("id", id), pair==null?null: new XAttribute("type", store.DecodeEntity((int)pair[1])),
                ((object[])tri[2]).Cast<object[]>().Select(dup =>
                {
                    return new XElement("field", new XAttribute("prop", store.DecodeEntity((int)dup[0])),
                        (string)dup[1]);
                }),
                ((object[])tri[1]).Cast<object[]>().Where(dup => (int)dup[0] != 0).Select(dup =>
                {
                    return new XElement("direct", new XAttribute("prop", store.DecodeEntity((int)dup[0])),
                        new XElement("record", new XAttribute("id", store.DecodeEntity((int)dup[1]))));
                }),
                null);
            if (addinverse)
            {
                var inv = store.GetRefers(cid);
                xres.Add(inv.Cast<object[]>().Select(tr =>
                {
                    int c = (int)tr[0];
                    object[] found = ((object[])tr[1]).Cast<object[]>().FirstOrDefault(dup => (int)dup[1] == cid);
                    return new XElement("inverse", new XAttribute("prop", store.DecodeEntity((int)found[0])),
                        new XElement("record", new XAttribute("id", store.DecodeEntity(c))));
                }));
            }
            return xres;
        }

        public override XElement GetItemById(string id, XElement format)
        {
            object[] node = (object[])store.GetRecord(store.CodeEntity(id));
            return GetItemByNode(node, format);
        }
        private XElement GetItemByNode(object[] node, XElement format)
        {
            if (format == null || format.Name != "record") throw new Exception("Err: strange format");
            if (node == null || (object[])node[1] == null) { return new XElement("record"); }
            object[] type_dup = ((object[])node[1]).Cast<object[]>().FirstOrDefault(dup => (int)dup[0] == 0);
            XElement xres = new XElement("record", new XAttribute("id", store.Decode((int)node[0])),
                type_dup==null?null: new XAttribute("type", store.DecodeEntity((int)type_dup[1])),
                format.Elements().Where(fel => fel.Name == "field" || fel.Name == "direct"|| fel.Name == "inverse")
                .SelectMany(fel =>
                {
                    string prop = fel.Attribute("prop").Value;
                    int iprop = store.Code(prop);
                    int cod = (int)node[0];
                    object[] invs = null;
                    if (fel.Name == "field")
                    {
                        // Предполагаю, что значений может быть несколько
                        var query = ((object[])node[2]).Cast<object[]>()
                        .Where(dup => (int)dup[0] == iprop)
                        .Select(dup =>
                        {
                            return new XElement("field", new XAttribute("prop", prop), (string)dup[1]);
                        });
                        return query;
                    }
                    else if (fel.Name == "direct")
                    {
                        // Предполагаю, что прямых ссылок с данным свойством может быть только одна
                        // найду соответствующую пару 
                        object[] dupp = ((object[])node[1]).Cast<object[]>()
                            .FirstOrDefault(dup => (int)dup[0] == iprop);
                        if (dupp == null) return new XElement[0];
                        int itarget = (int)dupp[1];
                        
                        return Enumerable.Repeat<XElement>(
                            new XElement("direct", new XAttribute("prop", prop),
                            GetItemByNode((object[])store.GetRecord(itarget), fel.Element("record")),
                            null), 1);
                    }
                    else
                    {
                        // Предполагаю, что обратных ссылок с данным свойством может быть много 
                        // Сначала вычислю группу обратных записей, если еще не вычислена
                        if (invs == null)
                        {
                            invs = store.GetRefers(cod).ToArray(); 
                            //invs = ((object[])store.GetRefers((int)node[0])).ToArray();
                        }
                        // Теперь надо сформировать множество элементов <inverse prop="..." <record ...
                        // Теоретически можно сгруппировать множество под одним inverse, но я не буду так делать
                        // здесь нам понадобятся только записи, где есть свойство и таргет
                        // Внутренний формат:
                        XElement subformat = fel.Element("record");
                        return invs.Cast<object[]>().Where(r => ((object[])r[1])
                            .Cast<object[]>()
                            .Any(dup => (int)dup[0] == iprop && (int)dup[1] == cod))
                        .Select(r => new XElement("inverse", new XAttribute("prop", prop),
                            GetItemByNode((object[])store.GetRecord((int)r[0]), subformat)));
                    }
                }),
                null);
            return xres;
        }

        public override IEnumerable<XElement> SearchByName(string searchstring)
        {
            var query2 = store.Like(searchstring).Cast<object[]>()
                .Select(tri => 
                {
                    string id = GetRecordId(tri);
                    string type = GetRecordType(tri);
                    XElement res = new XElement("record", new XAttribute("id", id), new XAttribute("type", type),
                        new XElement("field", new XAttribute("prop", "http://fogid.net/o/name"), GetRecordName(tri)));
                    return res;
                })//.ToArray()
                ;
            return query2;
        }

        public override XElement GetItemByIdSpecial(string id)
        {
            return GetItemByIdBasic(id, true);
        }

        // ============================== Редактирование ================================
        private object CodeRecord(XElement rec)
        {
            string id = rec.Attribute("{http://www.w3.org/1999/02/22-rdf-syntax-ns#}about").Value;
            string tp = "http://fogid.net/o/" + rec.Name.LocalName;
            int cid = store.CodeEntity(id);
            return new object[] { cid,
                Enumerable.Repeat<object[]>(new object[] {0, store.CodeEntity(tp)}, 1)
                    .Concat(rec.Elements().Where(el => el.Attribute("{http://www.w3.org/1999/02/22-rdf-syntax-ns#}resource") != null)
                        .Select(el => new object[] { store.CodeEntity("http://fogid.net/o/" + el.Name.LocalName),
                                store.CodeEntity(el.Attribute("{http://www.w3.org/1999/02/22-rdf-syntax-ns#}resource").Value) })).ToArray(),
                rec.Elements().Where(el => el.Attribute("{http://www.w3.org/1999/02/22-rdf-syntax-ns#}resource") == null)
                        .Select(el => new object[] { store.CodeEntity("http://fogid.net/o/" + el.Name.LocalName), el.Value }).ToArray()
            };
        }
        public override XElement PutItem(XElement record)
        {
            store.PutRecord(CodeRecord(record));
            return record;
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

    }
}
