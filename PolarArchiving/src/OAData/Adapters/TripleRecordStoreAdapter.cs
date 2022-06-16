using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
//using System.Text.Encodings;
using System.Xml.Linq;

using Polar.TripleStore;

namespace OAData.Adapters
{
    /// <summary>
    /// База данных на основе TripleStore-построения. База данных объединяет записи fog-документов
    /// без дублирования и цепочек эквивалентности. 
    /// </summary>
    public class TripleRecordStoreAdapter : DAdapter
    {
        // ========= Носитель базы данных ==========
        private TripleRecordStore store;

        private Action<string> errors = s => { Console.WriteLine(s); };
        string dbfolder = "D:/";
        int file_no = 0;
        //public bool firsttime = true; // Отмечает (вычисляет) ситуацию, когда базу данных обязательно нужно строить.
        // Важные коды
        //private int cod_rdftype = 0;
        //private int cod_name;
        //private int cod_delete;

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
            if (File.Exists(dbfolder + "0.bin")) nodatabase = false;
            Func<Stream> GenStream = () =>
                new FileStream(dbfolder + (file_no++) + ".bin", FileMode.OpenOrCreate, FileAccess.ReadWrite);
            store = new TripleRecordStore(GenStream, dbfolder, new string[] {
                "http://www.w3.org/1999/02/22-rdf-syntax-ns#type"
                , "http://fogid.net/o/delete"
                , "http://fogid.net/o/name"
                , "http://fogid.net/o/age"
                , "http://fogid.net/o/person"
                , "http://fogid.net/o/photo-doc"
                , "http://fogid.net/o/reflection"
                , "http://fogid.net/o/reflected"
                , "http://fogid.net/o/in-doc"
            });

            // Вычисление "важных" кодов
            //cod_rdftype = store.CodeEntity("http://www.w3.org/1999/02/22-rdf-syntax-ns#type"); // Зафиксирован? 0;
            //cod_delete = store.CodeEntity("http://fogid.net/o/delete");
            //cod_name = store.CodeEntity("http://fogid.net/o/name");

            //if (!firsttime) store.DynaBuild();
            store.Refresh();
            store.Flush();
        }
        public override void Close()
        {
            //store.Flush();
            store.Close();
        }
        // Загрузка базы данных
        public override void StartFillDb(Action<string> turlog)
        {
            //db = new XElement("db");
            // Пока каждый раз чистим базу данных
            store.Clear();
        }
        public override void LoadXFlow(IEnumerable<XElement> xflow, Dictionary<string, string> orig_ids)
        {
            IEnumerable<object> flow = xflow.Select(record =>
            {
                string id = record.Attribute(ONames.rdfabout).Value;
                // Корректируем идентификатор
                if (orig_ids.TryGetValue(id, out string idd)) id = idd;
                int rec_type = store.CodeEntity(ONames.fog + record.Name.LocalName);
                int id_ent = store.CodeEntity(id);
                object[] orecord = new object[] {
                        id_ent,
                        (new object[] { new object[] { store.cod_rdftype, rec_type } }).Concat(
                        record.Elements().Where(el => el.Attribute(ONames.rdfresource) != null)
                            .Select(subel =>
                            {
                                int prop = store.CodeEntity(subel.Name.NamespaceName + subel.Name.LocalName);
                                string resource = subel.Attribute(ONames.rdfresource).Value;
                                if (orig_ids.TryGetValue(resource, out string res)) if (res != null) resource = res;
                                return new object[] { prop, store.CodeEntity(resource) };
                            })).ToArray()
                        ,
                        record.Elements().Where(el => el.Attribute(ONames.rdfresource) == null)
                            .Select(subel =>
                            {
                                int prop = store.CodeEntity(subel.Name.NamespaceName + subel.Name.LocalName);
                                var xlang = subel.Attribute("{http://www.w3.org/XML/1998/namespace}lang");
                                string lang = xlang == null ? "" : xlang.Value;
                                return new object[] { prop, subel.Value, lang };
                            })
                            .ToArray()
                        };
                return orecord;
            });
            store.Load(flow);
        }

        public override void FinishFillDb(Action<string> turlog)
        {
            store.Build();
            GC.Collect();
        }
        // Новая реализация загрузки базы данных
        //public override void FillDb(IEnumerable<FogInfo> fogflow, Action<string> turlog)
        //{
        //    // Нулевой проход сканирования фог-файлов. Строим таблицу строка-строка с соответствием идентификатору
        //    // идентификатора оригинала или null.
        //    Dictionary<string, string> substitutes = new Dictionary<string, string>();
        //    foreach (FogInfo fi in fogflow)
        //    {
        //        // Чтение фога
        //        if (fi.vid == ".fog")
        //        {
        //            fi.fogx = XElement.Load(fi.pth);
        //            foreach (XElement xrec in fi.fogx.Elements())
        //            {
        //                XElement record = ConvertXElement(xrec);

        //                string id = record.Attribute(ONames.rdfabout)?.Value;

        //                // Обработаем delete и replace
        //                if (record.Name == ONames.fogi + "delete")
        //                {
        //                    string idd = record.Attribute(ONames.rdfabout)?.Value;
        //                    if (idd == null) idd = record.Attribute("id").Value;
        //                    if (substitutes.ContainsKey(idd))
        //                    {
        //                        substitutes.Remove(idd);
        //                    }
        //                    substitutes.Add(idd, null);
        //                    continue;
        //                }
        //                if (record.Name == ONames.fogi + "substitute")
        //                {
        //                    string idold = record.Attribute("old-id").Value;
        //                    string idnew = record.Attribute("new-id").Value;
        //                    // может old-id уже уничтожен, огда ничего не менять
        //                    if (substitutes.TryGetValue(idold, out string value))
        //                    {
        //                        if (value == null) continue;
        //                        substitutes.Remove(idold);
        //                    }
        //                    // иначе заменить в любом случае
        //                    substitutes.Add(idold, idnew);
        //                    continue;
        //                }
        //            }
        //        }
        //    }
        //    // Функция, добирающаяся до последнего определения или это и есть последнее
        //    Func<string, string> original = null;
        //    original = id => 
        //    {
        //        if (substitutes.TryGetValue(id, out string idd))
        //        {
        //            if (idd == null) return id;
        //            return original(idd);
        //        }
        //        return id;
        //    };
        //    // Обработаем словарь, формируя новый 
        //    Dictionary<string, string> orig_ids = new Dictionary<string, string>();
        //    foreach (var pair in substitutes)
        //    {
        //        string key = pair.Key;
        //        string value = pair.Value;
        //        if (value != null) value = original(value);
        //        orig_ids.Add(key, value);
        //    }


        //    // Готовим битовый массив для отметки того, что хеш id уже "попадал" в этот бит
        //    int rang = 24; // пока предполагается, что число записей (много) меньше 16 млн.
        //    int mask = ~((-1) << rang);
        //    int ba_volume = mask + 1;
        //    System.Collections.BitArray bitArr = new System.Collections.BitArray(ba_volume);
        //    // Хеш-функция будет ограничена rang разрядами, массив и функция нужны только временно
        //    Func<string, int> Hash = s => s.GetHashCode() & mask;
        //    // Словарь, фиксирующий максимальную дату для заданного идентификатора, первая запись не учитывается
        //    Dictionary<string, DateTime> lastDefs = new Dictionary<string, DateTime>();

        //    // Первый проход сканирования фог-файлов. В этом проходе определяется набор повторно определенных записей и
        //    // формируется словарь lastDefs
        //    foreach (FogInfo fi in fogflow)
        //    {
        //        // Чтение фога
        //        if (fi.vid == ".fog")
        //        {
        //            fi.fogx = XElement.Load(fi.pth);
        //            foreach (XElement xrec in fi.fogx.Elements())
        //            {
        //                XElement record = ConvertXElement(xrec);
        //                // Обработаем "странные" delete и replace
        //                if (record.Name == ONames.fogi + "delete")
        //                {
        //                    string idd = record.Attribute(ONames.rdfabout)?.Value;
        //                    if (idd == null) idd = record.Attribute("id").Value;
        //                    // Если нет атрибута mT, временная отметка устанавливается максимальной, чтобы "забить" другие отметки
        //                    string mTvalue = record.Attribute("mT") == null ? DateTime.MaxValue.ToString() : record.Attribute("mT").Value;
        //                    CheckAndSet(bitArr, Hash, lastDefs, idd, mTvalue);
        //                    continue;
        //                }
        //                if (record.Name == ONames.fogi + "substitute")
        //                {
        //                    continue;
        //                }
        //                string id = record.Attribute(ONames.rdfabout).Value;
        //                CheckAndSet(bitArr, Hash, lastDefs, id, record.Attribute("mT")?.Value);
        //            }
        //            fi.fogx = null;
        //        }
        //        else if (fi.vid == ".fogp")
        //        {

        //        }
        //    }
            
        //    // Второй проход сканирования фог-файлов. В этом проходе, для каждого фога формируется поток записей в 
        //    // объектном представлении, потом этот поток добавляется в store
        //    foreach (FogInfo fi in fogflow)
        //    {
        //        // Чтение фога
        //        if (fi.vid == ".fog" || fi.vid == ".fogx")
        //        {
        //            fi.fogx = XElement.Load(fi.pth);
        //            IEnumerable<object> flow = fi.fogx.Elements()
        //            .Select(xrec => ConvertXElement(xrec))
        //            // Обработаем delete и replace. delete доминирует (по времени), но в выходной поток ничего не попадает
        //            // substitute (Может наод orig_ids?) - не обрабатываем
        //            .Where(record => record.Name != ONames.fogi + "delete" &&
        //                    record.Name != ONames.fogi + "substitute")

        //            // Отфильтруем все записи, попавшие в substitutes
        //            .Where(record => !orig_ids.ContainsKey(record.Attribute(ONames.rdfabout).Value))
                    
        //            // Отфильтруем записи с малыми временами. т.е. если идентификатор есть в словаре, но он меньше.
        //            // Если он больше, то не фильтруем, а словарный вход корректируем
        //            .Where(record =>
        //            {
        //                string id = record.Attribute(ONames.rdfabout).Value;
        //                if (lastDefs.ContainsKey(id))
        //                {
        //                    DateTime mT = DateTime.MinValue;
        //                    string mTvalue = record.Attribute("mT")?.Value;
        //                    if (mTvalue != null) mT = DateTime.Parse(mTvalue);
        //                    DateTime last = lastDefs[id];
        //                    if (mT < last) return false; // пропускаем
        //                    else if (mT == last) return true; // берем
        //                    else // if(mT > last) // берем и корректируем last
        //                    {
        //                        lastDefs.Remove(id);
        //                        lastDefs.Add(id, mT);
        //                        return true;
        //                    }
        //                }
        //                return true;
        //            })
        //            .Select(record =>
        //            {
        //                string id = record.Attribute(ONames.rdfabout).Value;
        //                // Корректируем идентификатор
        //                if (orig_ids.TryGetValue(id, out string idd)) id = idd;
        //                int rec_type = store.CodeEntity(ONames.fog + record.Name.LocalName);
        //                int id_ent = store.CodeEntity(id);
        //                object[] orecord = new object[] {
        //                id_ent,
        //                (new object[] { new object[] { cod_rdftype, rec_type } }).Concat(
        //                record.Elements().Where(el => el.Attribute(ONames.rdfresource) != null)
        //                    .Select(subel =>
        //                    {
        //                        int prop = store.CodeEntity(subel.Name.NamespaceName + subel.Name.LocalName);
        //                        string resource = subel.Attribute(ONames.rdfresource).Value;
        //                        if (orig_ids.TryGetValue(resource, out string res)) if (res != null) resource = res;
        //                        return new object[] { prop, store.CodeEntity(resource) };
        //                    })).ToArray()
        //                ,
        //                record.Elements().Where(el => el.Attribute(ONames.rdfresource) == null)
        //                    .Select(subel =>
        //                    {
        //                        int prop = store.CodeEntity(subel.Name.NamespaceName + subel.Name.LocalName);
        //                        return new object[] { prop, subel.Value };
        //                    })
        //                    .ToArray()
        //                };
        //                return orecord;
        //            });
        //            store.Load(flow);
                    
        //            fi.fogx = null;
        //        }
        //        else if (fi.vid == ".fogp")
        //        {

        //        }
        //    }
        //    store.Build();
        //    store.Flush();
        //    GC.Collect();
        //}
        /// <summary>
        /// Если идентификатор еще не отмеченный, то отметим его, если идентификатор уже отмеченный, то поместим значение mT в словарь
        /// </summary>
        /// <param name="bitArr"></param>
        /// <param name="Hash"></param>
        /// <param name="lastDefs"></param>
        /// <param name="id"></param>
        /// <param name="mTval">отметка времени. null - минимальное время</param>
        private static void CheckAndSet(System.Collections.BitArray bitArr, Func<string, int> Hash, Dictionary<string, DateTime> lastDefs, string id, string mTval)
        {
            int code = Hash(id);
            if (bitArr.Get(code))
            {
                // Добавляем пару в словарь
                DateTime mT = DateTime.MinValue;
                if (mTval != null) { mT = DateTime.Parse(mTval); }
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

        //public override void LoadFromCassettesExpress(IEnumerable<string> fogfilearr, Action<string> turlog, Action<string> convertlog)
        //{
        //    store.Clear();

        //    // Нужно для чтения в кодировке windows-1251. Нужен также Nuget System.Text.Encoding.CodePages
        //    //var v = System.Text.Encoding.CodePagesEncodingProvider.Instance;
        //    //System.Text.Encoding.RegisterProvider(v);

        //    // Готовим битовый массив для отметки того, что хеш id уже "попадал" в этот бит
        //    int ba_volume = 1024 * 1024;
        //    int mask = ~((-1) << 20);
        //    System.Collections.BitArray bitArr = new System.Collections.BitArray(ba_volume);
        //    // Хеш-функция будет ограничена 20-ю разрядами, массив и функция нужны только временно
        //    Func<string, int> Hash = s => s.GetHashCode() & mask;
        //    // Словарь
        //    Dictionary<string, DateTime> lastDefs = new Dictionary<string, DateTime>();
        //    int cnt = 0;
        //    // Первый проход сканирования фог-файлов
        //    foreach (string fog_name in fogfilearr)
        //    {
        //        // Чтение фога
        //        XElement fog = XElement.Load(fog_name);
        //        // Перебор записей
        //        foreach (XElement rec in fog.Elements())
        //        {
        //            XElement record = ConvertXElement(rec);
        //            cnt++;
        //            if (record.Name == "delete"
        //                || record.Name == "{http://fogid.net/o/}delete"
        //                || record.Name == "substitute"
        //                || record.Name == "{http://fogid.net/o/}substitute"
        //                ) continue;
        //            string id = record.Attribute("{http://www.w3.org/1999/02/22-rdf-syntax-ns#}about").Value;
        //            CheckAndSet(bitArr, Hash, lastDefs, id, record.Attribute("mT")?.Value);
        //        }
        //    }

        //    cnt = 0;

        //    // Второй проход сканирования фог-файлов
        //    IEnumerable<object> objectFlow = fogfilearr.SelectMany(fog_name =>
        //    {
        //        XElement fog = XElement.Load(fog_name);
        //        // Перебор записей
        //        return fog.Elements()
        //        .Where(rec => rec.Name != "delete"
        //                && rec.Name != "{http://fogid.net/o/}delete"
        //                && rec.Name != "substitute"
        //                && rec.Name != "{http://fogid.net/o/}substitute")
        //        .Select(rec => ConvertXElement(rec))
        //        .Where(record =>
        //        {
        //            string id = record.Attribute("{http://www.w3.org/1999/02/22-rdf-syntax-ns#}about").Value;
        //            //int code = Hash(id);

        //            bool toprocess = false;
        //            if (lastDefs.TryGetValue(id, out DateTime saved))
        //            {
        //                DateTime mT = DateTime.MinValue;
        //                XAttribute mT_att = record.Attribute("mT");
        //                if (mT_att != null) { mT = DateTime.Parse(mT_att.Value); }
        //                // Оригинал если отметка времени больше или равна
        //                if (mT >= saved)
        //                {
        //                    // сначала обеспечим приоритет перед другими решениями
        //                    lastDefs.Remove(id);
        //                    lastDefs.Add(id, mT);
        //                    // оригинал!
        //                    // Обрабатываем!
        //                    toprocess = true;
        //                }
        //            }
        //            else
        //            {
        //                // это единственное определение!
        //                // Обрабатываем!
        //                toprocess = true;
        //            }
        //            return toprocess;
        //        })
        //        .Select(record =>
        //        {
        //            string id = record.Attribute("{http://www.w3.org/1999/02/22-rdf-syntax-ns#}about").Value;
        //            int rec_type = store.CodeEntity("http://fogid.net/o/" + record.Name.LocalName);
        //            int id_ent = store.CodeEntity(id);
        //            object[] orecord = new object[] {
        //                id_ent,
        //                (new object[] { new object[] { cod_rdftype, rec_type } }).Concat(
        //                record.Elements().Where(el => el.Attribute(ONames.rdfresource) != null)
        //                    .Select(subel =>
        //                    {
        //                        int prop = store.CodeEntity(subel.Name.NamespaceName + subel.Name.LocalName);
        //                        return new object[] { prop, store.CodeEntity(subel.Attribute(ONames.rdfresource).Value) };
        //                    })).ToArray()
        //                ,
        //                record.Elements().Where(el => el.Attribute(ONames.rdfresource) == null)
        //                    .Select(subel =>
        //                    {
        //                        int prop = store.CodeEntity(subel.Name.NamespaceName + subel.Name.LocalName);
        //                        return new object[] { prop, subel.Value };
        //                    })
        //                    .ToArray()
        //            };
        //            return orecord;
        //        });
        //    });

        //    store.Load(objectFlow);

        //    store.Build();

        //    store.Flush();
        //    GC.Collect();
        //}


        private string GetRecordId(object rec)
        {
            var tri = (object[])rec;
            return store.Decode((int)tri[0]);
        }
        private string GetRecordType(object rec)
        {
            var tri = (object[])rec;
            object[] tduple = (object[])(((object[])tri[1]).FirstOrDefault(pair => (int)((object[])pair)[0] == store.cod_rdftype));
            if (tduple == null) return null;
            return store.DecodeEntity((int)tduple[1]);
        }
        private string GetRecordName(object rec)
        {
            var tri = (object[])rec;
            return (string)((object[])((object[])tri[2]).Cast<object[]>().FirstOrDefault(pair => (int)pair[0] == store.cod_name))[1];
        }

        // =========================== Основные выборки и поиски ===============================

        public override XElement GetItemByIdBasic(string id, bool addinverse)
        {
            int cid = store.CodeEntity(id);
            object[] tri = (object[])store.GetRecord(cid);
            if (tri == null) return null;
            object[] pair = ((object[])tri[1]).Cast<object[]>().FirstOrDefault(dup => (int)dup[0] == store.cod_rdftype);
            XElement xres = new XElement("record",
                new XAttribute("id", id), pair==null?null: new XAttribute("type", store.DecodeEntity((int)pair[1])),
                ((object[])tri[2]).Cast<object[]>().Select(dup =>
                {
                    return new XElement("field", new XAttribute("prop", store.DecodeEntity((int)dup[0])),
                        ((string)dup[2] == ""?null: new XAttribute("{http://www.w3.org/XML/1998/namespace}lang", (string)dup[2])),
                        (string)dup[1]);
                }),
                ((object[])tri[1]).Cast<object[]>().Where(dup => (int)dup[0] != store.cod_rdftype).Select(dup =>
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
            object[] type_dup = ((object[])node[1]).Cast<object[]>().FirstOrDefault(dup => (int)dup[0] == store.cod_rdftype);
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
                            //return new XElement("field", new XAttribute("prop", prop), (string)dup[1]);
                            return new XElement("field", new XAttribute("prop", store.DecodeEntity((int)dup[0])),
                        ((string)dup[2] == "" ? null : new XAttribute("{http://www.w3.org/XML/1998/namespace}lang", (string)dup[2])),
                        (string)dup[1]);
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
                        object[] target_node = (object[])store.GetRecord(itarget);
                        if (target_node == null) return new XElement[0]; // цели нет!
                        string rec_type = GetRecordType(target_node);
                        var rec_format = fel.Elements("record").FirstOrDefault(r => r.Attribute("type")?.Value == rec_type);
                        if (rec_format == null) rec_format = fel.Element("record");
                        return Enumerable.Repeat<XElement>(
                            new XElement("direct", new XAttribute("prop", prop),
                            GetItemByNode(target_node, rec_format),
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
                .ToArray()
                ;
            return query2;
        }
        public override IEnumerable<XElement> SearchByWords(string searchwords) { throw new NotImplementedException(); }

        public override IEnumerable<XElement> GetAll()
        {
            var query = store.GetRecords()
                .Cast<object[]>()
                .Select(tri => // Это базовая тройка записи: ид, массив прямых ссылок, массив полей
                {
                    string id = store.DecodeEntity((int)tri[0]);
                    // Типовая пара среди прямых ссылок (свойство rdf:type закодировано как cod_rdftype
                    object[] pair = ((object[])tri[1]).Cast<object[]>().FirstOrDefault(dup => (int)dup[0] == store.cod_rdftype);
                    XElement xres = new XElement("record",
                        new XAttribute("id", id), pair == null ? null : new XAttribute("type", store.DecodeEntity((int)pair[1])),
                        ((object[])tri[2]).Cast<object[]>().Select(dup =>
                        {
                            return new XElement("field", new XAttribute("prop", store.DecodeEntity((int)dup[0])),
                                (string)dup[1]);
                        }),
                        ((object[])tri[1]).Cast<object[]>().Where(dup => (int)dup[0] != store.cod_rdftype).Select(dup =>
                        {
                            return new XElement("direct", new XAttribute("prop", store.DecodeEntity((int)dup[0])),
                                new XElement("record", new XAttribute("id", store.DecodeEntity((int)dup[1]))));
                        }),
                        null);
                    return xres;
                });
            return query;
        }


        // ============================== Редактирование ================================
        private object CodeRecord(XElement rec)
        {
            string id = rec.Attribute("{http://www.w3.org/1999/02/22-rdf-syntax-ns#}about").Value;
            string tp = "http://fogid.net/o/" + rec.Name.LocalName;
            int cid = store.CodeEntity(id);
            return new object[] { cid,
                Enumerable.Repeat<object[]>(new object[] { store.cod_rdftype, store.CodeEntity(tp)}, 1)
                    .Concat(rec.Elements().Where(el => el.Attribute("{http://www.w3.org/1999/02/22-rdf-syntax-ns#}resource") != null)
                        .Select(el => new object[] { store.CodeEntity("http://fogid.net/o/" + el.Name.LocalName),
                                store.CodeEntity(el.Attribute("{http://www.w3.org/1999/02/22-rdf-syntax-ns#}resource").Value) })).ToArray(),
                rec.Elements().Where(el => el.Attribute("{http://www.w3.org/1999/02/22-rdf-syntax-ns#}resource") == null)
                        .Select(el => new object[] { store.CodeEntity("http://fogid.net/o/" + el.Name.LocalName), el.Value, el.Attribute(ONames.xmllang)==null?"": el.Attribute(ONames.xmllang).Value }).ToArray()
            };
        }
        public override XElement PutItem(XElement record)
        {
            store.PutRecord(CodeRecord(record));
            return record;
        }
        public override XElement Delete(string id)
        {
            throw new NotImplementedException();
        }

    }
}
