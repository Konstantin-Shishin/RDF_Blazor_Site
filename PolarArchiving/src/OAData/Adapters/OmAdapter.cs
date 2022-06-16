using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml.Linq;
using Polar.DB;
using Polar.OModel;

namespace OAData.Adapters
{
    public class OmAdapter : DAdapter
    {
        // Адаптер состоит из последовательности записей, дополнительного индекса и последовательности триплетов объектных свойств 
        private PType tp_prop;
        private PType tp_rec;
        private PType tp_triple;
        private UniversalSequence records;
        private SVectorIndex names;
        private SVectorDynaIndex additional_names;
        private UniversalSequence obj_props;
        private Func<object, bool> Isnull; 
        // Констуктор инициализирует то, что можно и не изменяется
        public OmAdapter()
        {
            tp_prop = new PTypeUnion(
                new NamedType("novariant", new PType(PTypeEnumeration.none)),
                new NamedType("field", new PTypeRecord(
                    new NamedType("prop", new PType(PTypeEnumeration.sstring)),
                    new NamedType("value", new PType(PTypeEnumeration.sstring)),
                    new NamedType("lang", new PType(PTypeEnumeration.sstring)))),
                new NamedType("objprop", new PTypeRecord(
                    new NamedType("prop", new PType(PTypeEnumeration.sstring)),
                    new NamedType("link", new PType(PTypeEnumeration.sstring)))),
                new NamedType("invlink", new PTypeRecord(
                    new NamedType("prop", new PType(PTypeEnumeration.sstring)),
                    new NamedType("sourcelink", new PType(PTypeEnumeration.sstring))))
                );
            tp_rec = new PTypeRecord(
                new NamedType("id", new PType(PTypeEnumeration.sstring)),
                new NamedType("tp", new PType(PTypeEnumeration.sstring)),
                new NamedType("props", new PTypeSequence(tp_prop)));
            tp_triple = new PTypeRecord(
                new NamedType("subj", new PType(PTypeEnumeration.sstring)),
                new NamedType("pred", new PType(PTypeEnumeration.sstring)),
                new NamedType("obj", new PType(PTypeEnumeration.sstring)));
            Isnull = ob => (string)((object[])ob)[1] == "delete"; // в поле tp находится "delete" //TODO: проверить, что delete без пространств имен 
        }

        // Главный инициализатор. Используем connectionstring 
        private string dbfolder;
        private int file_no = 0;
        public override void Init(string connectionstring)
        {
            if (connectionstring != null && connectionstring.StartsWith("om:"))
            {
                dbfolder = connectionstring.Substring("om:".Length);
            }
            if (File.Exists(dbfolder + "0.bin")) nodatabase = false;
            Func<Stream> GenStream = () =>
                new FileStream(dbfolder + (file_no++) + ".bin", FileMode.OpenOrCreate, FileAccess.ReadWrite);

            // Полключение к базе данных
            Func<object, int> hashId = obj => Hashfunctions.HashRot13(((string)(((object[])obj)[0])));
            Comparer<object> comp_direct = Comparer<object>.Create(new Comparison<object>((object a, object b) =>
            {
                string val1 = (string)((object[])a)[0];
                string val2 = (string)((object[])b)[0];
                return string.Compare(val1, val2, StringComparison.OrdinalIgnoreCase);
            }));

            // Создаем последовательность R-записей
            records = new UniversalSequence(tp_rec, GenStream, null, null,
                obj => Hashfunctions.HashRot13((string)(((object[])obj)[0])),
                comp_direct,
                new HashComp[]
                { 
                    //new HashComp { Hash = null, Comp = comp_direct } 
                });
            Func<object, IEnumerable<string>> skey = obj =>
            {
                object[] props = (object[])((object[])obj)[2];
                var query = props.Where(p => (int)((object[])p)[0] == 1)
                    .Select(p => ((object[])p)[1])
                    .Cast<object[]>()
                    .Where(f => (string)f[0] == "http://fogid.net/o/name")
                    .Select(f => (string)f[1]).ToArray();
                return query;
            };
            names = new SVectorIndex(GenStream, records, skey);
            additional_names = new SVectorDynaIndex(records, skey); 
        }


        public override void StartFillDb(Action<string> turlog)
        {
            records.Clear();
            // Индексы чистятся внутри
        }
        public override void FinishFillDb(Action<string> turlog)
        {
            records.Build();
            GC.Collect();
        }

        // Загрузка потока x-элементов
        internal class OTriple { public string subj, pred, resource; }
        internal class InverseLink { public string pred, source; }
        public override void LoadXFlow(IEnumerable<XElement> xflow, Dictionary<string, string> orig_ids)
        {
            // Соберем граф через 2 сканирования
            // Проход 1: вычисление обратных объектных свойств
            Dictionary<string, InverseLink[]> inverseProps = xflow.SelectMany(record =>
            {
                string id = record.Attribute(ONames.rdfabout).Value;
                if (id == "syp2001-p-marchuk_a") { }
                // Корректируем идентификатор
                if (orig_ids.TryGetValue(id, out string idd)) id = idd;
                var directProps = record.Elements().Where(el => el.Attribute(ONames.rdfresource) != null)
                    .Select(el =>
                    {
                        string subj = id;
                        string pred = el.Name.NamespaceName + el.Name.LocalName;
                        string resource = el.Attribute(ONames.rdfresource).Value;
                        if (orig_ids.TryGetValue(resource, out string res)) if (res != null) resource = res;
                        return new OTriple { subj = subj, pred = pred, resource = resource };
                    });
                return directProps;
            }).GroupBy(triple => triple.resource, triple => new InverseLink { pred = triple.pred, source = triple.subj })
            .Select(g => new { key = g.Key, ilinks = g.Select(lnk => lnk) })
            .ToDictionary(pair => pair.key, pair => pair.ilinks.ToArray());
            //Dictionary<string, InverseLink[]> inverseProps = new Dictionary<string, InverseLink[]>();

            //var test_rec = inverseProps["syp2001-p-marchuk_a"];

            // Проход 2: вычисление графа 
            IEnumerable<object> flow = xflow.Select(record =>
            {
                string id = record.Attribute(ONames.rdfabout).Value;
                // Корректируем идентификатор
                if (orig_ids.TryGetValue(id, out string idd)) id = idd;

                string rec_type = ONames.fog + record.Name.LocalName;
                object[] orecord = new object[] {
                        id,
                        rec_type,
                        record.Elements()
                            .Select(el =>
                            {
                                string prop = el.Name.NamespaceName + el.Name.LocalName;
                                if (el.Attribute(ONames.rdfresource) != null)
                                {
                                    string resource = el.Attribute(ONames.rdfresource).Value;
                                    if (orig_ids.TryGetValue(resource, out string res)) if (res != null) resource = res;
                                    return new object[] { 2, new object[] { prop, resource } };
                                }
                                else
                                {
                                    var xlang = el.Attribute("{http://www.w3.org/XML/1998/namespace}lang");
                                    string lang = xlang == null ? "" : xlang.Value;
                                    return new object[] { 1, new object[] { prop, el.Value, lang } };
                                }
                            })
                            .Concat(inverseProps.ContainsKey(id) ? 
                                inverseProps[id].Select(ip => new object[] { 3, new object[] { ip.pred, ip.source } }) :
                                Enumerable.Empty<InverseLink[]>())
                            .ToArray()
                        };
                return orecord;
            });
            records.Load(flow);
            GC.Collect();
        }

        // Хеш-словарь для динамической таблицы
        private Dictionary<string, object> dyndic = new Dictionary<string, object>();

        public override IEnumerable<XElement> GetAll()
        {
            throw new NotImplementedException();
        }

        public override XElement GetItemById(string id, XElement format)
        {
            return ItemByRecord((object[])GetRecord(id), format);
        }
        private XElement ItemByRecord(object[] record, XElement format)
        {
            var xprops = format.Elements().SelectMany(fe =>
            {
                string prop = fe.Attribute("prop").Value;
                if (fe.Name.LocalName == "field")
                {
                    var ofields = ((object[])record[2]).Cast<object[]>().Where(pa => (int)pa[0] == 1)
                        .Select(pa => (object[])pa[1]).Where(ofield => (string)ofield[0] == prop)
                        .Select(ofield => new XElement("field", new XAttribute("prop", prop),
                            (ofield[2] == null ? null : new XAttribute(ONames.xmllang, ofield[2])),
                            ofield[1]));
                    return ofields;
                }
                else if (fe.Name.LocalName == "direct")
                {
                    // Прямая ссылка может быть только одна (???)
                    var pa_prop = ((object[])record[2]).Cast<object[]>()
                        .FirstOrDefault(pa => (int)pa[0] == 2 && (string)((object[])pa[1])[0] == prop);
                     
                    if (pa_prop == null || pa_prop.Length != 2) return Enumerable.Empty<XElement>();
                    // Найдем запись 
                    object[] drec = (object[])GetRecord((string)((object[])pa_prop[1])[1]);
                    if (drec == null) return Enumerable.Empty<XElement>();
                    string tp = (string)drec[1];
                    XElement f = fe.Elements("record")
                        .FirstOrDefault(xre => xre.Attribute("type") == null || xre.Attribute("type").Value == tp); 
                    // Вот это место надо бы сделать с учетом наследования типов, а пока - упрощенный вариант
                    if (f == null)
                    {
                        XElement ff = fe.Element("record");
                        if (ff == null) return Enumerable.Empty<XElement>();
                        f = ff;
                    }

                    // Мы подобрали формат к записи и можем рекурсивно применить метод
                    return Enumerable.Repeat<XElement>(new XElement("direct", ItemByRecord(drec, f)), 1);
                }
                else if (fe.Name.LocalName == "inverse")
                {
                    // обратных ссылок может быть несколько (!)
                    var pa_props = ((object[])record[2]).Cast<object[]>()
                        .Where(pa => (int)pa[0] == 3 && (string)((object[])pa[1])[0] == prop);

                    XElement f = fe.Element("record");

                    var iprops = pa_props.Select(pa => ((object[])pa[1]))
                        .Select(prop_resource => new XElement("inverse", new XAttribute("prop", prop_resource[0]),
                        ItemByRecord((object[])GetRecord((string)prop_resource[1]), f)));
                    return iprops;
                }
                return Enumerable.Empty<XElement>();
            }); 
            XElement xres = new XElement("record", new XAttribute("id", (string)record[0]),
                new XAttribute("type", (string)record[1]),
                xprops,
                null);
            return xres;
        }

        public override XElement GetItemByIdBasic(string id, bool addinverse)
        {
            object rec = GetRecord(id);
            
            if (rec == null || Isnull(rec)) return null; 
            XElement xres = ORecToXRec((object[])rec, addinverse);
            
            return xres;
        }
        private object GetRecord(string id)
        {
            object rec;
            if (dyndic.TryGetValue(id, out rec)) { }
            else
            {
                rec = records.GetByKey(new object[] { id, null, null });
            }
            return rec;
        }
        private XElement ORecToXRec(object[] ob, bool addinverse)
        {
            return new XElement("record",
                new XAttribute("id", ob[0]), new XAttribute("type", ob[1]),
                ((object[])ob[2]).Cast<object[]>().Select(uni =>
                {
                    if ((int)uni[0] == 1)
                    {
                        object[] p = (object[])uni[1];
                        XAttribute langatt = string.IsNullOrEmpty((string)p[2]) ? null : new XAttribute(ONames.xmllang, p[2]);
                        return new XElement("field", new XAttribute("prop", p[0]), langatt,
                            p[1]);
                    }
                    else if ((int)uni[0] == 2)
                    {
                        object[] p = (object[])uni[1];
                        return new XElement("direct", new XAttribute("prop", p[0]),
                            new XElement("record", new XAttribute("id", p[1])));
                    }
                    else if ((int)uni[0] == 3)
                    {
                        object[] p = (object[])uni[1];
                        return new XElement("inverse", new XAttribute("prop", p[0]),
                            new XElement("record", new XAttribute("id", p[1])));
                    }
                    return null;
                }));
        }
        private object XRecToORec(XElement xrec)
        {
            object[] orec = new object[]
            {
                xrec.Attribute(ONames.rdfabout).Value,
                xrec.Name.NamespaceName + xrec.Name.LocalName,
                xrec.Elements()
                    .Select<XElement, object>(el =>
                    {
                        string prop = el.Name.NamespaceName + el.Name.LocalName;
                        string resource = el.Attribute(ONames.rdfresource)?.Value;
                        if (resource == null)
                        {  // Поле
                            string lang = el.Attribute(ONames.xmllang)?.Value;
                            return new object[] { 1, new object[] { prop, el.Value, lang } };
                        }
                        else
                        {  // Объектная ссылка
                            return new object[] { 2, new object[] { prop, resource } };
                        }
                    }).ToArray()
            };
            return orec;
        }
        private class RecordComparer : IEqualityComparer<object>
        {
            public new bool Equals(object x, object y)
            {
                return ((string)((object[])x)[0]).Equals((string)((object[])y)[0]);
            }

            public int GetHashCode(object obj)
            {
                return ((string)((object[])obj)[0]).GetHashCode();
            }
        }
        private class DirectPropComparer : IEqualityComparer<object>
        {
            public new bool Equals(object x, object y)
            {
                return ((string)((object[])x)[1]).Equals((string)((object[])y)[1]) &&
                    ((string)((object[])x)[0]).Equals((string)((object[])y)[0]);
            }

            public int GetHashCode(object obj)
            {
                return ((string)((object[])obj)[1]).GetHashCode();
            }
        }
        public override IEnumerable<XElement> SearchByName(string searchstring)
        {
            var query = names.LikeBySKey(searchstring)
                .Concat(additional_names.LikeBySKey(searchstring))
                .Where(ob => !Isnull(ob))
                .ToArray();
            var comparer = new RecordComparer();
            var bigset = names.LikeBySKey(searchstring)
                .Distinct<object>(comparer)
                .ToArray();
            var littleset = additional_names.LikeBySKey(searchstring)
                .Distinct<object>(comparer)
                .ToArray();
            var difference = bigset.Except(littleset, comparer).ToArray();
            var total = difference.Concat(littleset)
                .Where(ob =>
                {
                    string id = (string)((object[])ob)[0];
                    bool isn = false;
                    if (dyndic.TryGetValue(id, out object dobj)) isn = Isnull(dobj);
                    return !isn;
                })
                .ToArray();
            return total.Cast<object[]>().Select(ob => new XElement("record",
                new XAttribute("id", ob[0]), new XAttribute("type", ob[1]),
                ((object[])ob[2]).Cast<object[]>().Select(uni =>
                {
                    if ((int)uni[0] == 1)
                    {
                        object[] p = (object[])uni[1];
                        XAttribute langatt = string.IsNullOrEmpty((string)p[2]) ? null : new XAttribute(ONames.xmllang, p[2]);
                        return new XElement("field", new XAttribute("prop", p[0]), langatt,
                            p[1]);
                    }
                    else if ((int)uni[0] == 2)
                    {
                        object[] p = (object[])uni[1];
                        return new XElement("direct", new XAttribute("prop", p[0]));
                    }
                    return null;
                })));
        }
        public override IEnumerable<XElement> SearchByWords(string searchwords) { throw new NotImplementedException(); }


        /// <summary>
        /// Есть старое значение orec (в частном случае - нет) и есть новое значение nrec. Основной трек: 1) новое значение замещает в 
        /// dyndic то, что там есть по этому идентификатору; 2) В дополнительных именах запускается OnAddItem; 3) Множество "затронутых"
        /// прямых ссылок разбивается на "старые", "не тронутые", "новые". В старых узлах изымается "мой" обратный линк, в новых - добавляется 
        /// </summary>
        /// <param name="record"></param>
        /// <returns></returns>
        public override XElement PutItem(XElement record)
        {
            DirectPropComparer pcomparer = new DirectPropComparer();
            // Вычисляем идентификатор
            string id = record.Attribute(ONames.rdfabout).Value;

            // Вычисляем новую запись в объектном представлении
            object nrec;
            if (record.Name == "delete")
            {
                nrec = new object[] { id, "delete", new object[0] };
            }
            else
            {
                nrec = XRecToORec(record);
            }

            // Вычисляем старую запись в объектном представлении. Ее или нет, или она в динамическом наборе или она в статическом
            // Дополнительно устанавливаем признак isindyndic
            bool isindyndic = false;
            object orec = null;
            if (dyndic.TryGetValue(id, out orec)) { isindyndic = true; }
            else
            {  // или объект или null
                orec = records.GetByKey(new object[] { id, null, null });
            }

            // Соберем прямые ссылки из nrec и orec (O и N) в три множества: Те которые были removed, те которые появляются appeared
            // и остальные (сохраняемые). removed = O \ N, appeared = N \ O.
            // Делаем множества пар свойство-ссылка: 
            object[] O = orec==null? new object[0] : ((object[])((object[])orec)[2])
                .Where(x => (int)((object[])x)[0] == 2)
                .Select(x => (object[])((object[])x)[1])
                .Distinct(pcomparer)
                .ToArray();
            object[] N = ((object[])((object[])nrec)[2])
                .Where(x => (int)((object[])x)[0] == 2)
                .Select(x => (object[])((object[])x)[1])
                .Distinct(pcomparer)
                .ToArray();
            var removed = O.Except(N, pcomparer).ToArray();
            var appeared = N.Except(O, pcomparer).ToArray();

            // Если orec не нулл, перенесем из него обратные ссылки (!)
            if (orec != null)
            {
                ((object[])nrec)[2] = ((object[])((object[])nrec)[2]).Concat(((object[])(((object[])orec)[2]))
                    .Where(rprop => (int)((object[])rprop)[0] == 3))
                    .ToArray();
            }

            // Новым значением заместим старое в динамическом наборе
            if (isindyndic) 
            {
                object[] oarr = (object[])orec;
                object[] narr = (object[])nrec;
                narr[0] = oarr[0];
                narr[1] = oarr[1];
                narr[2] = oarr[2];
            }
            else
            {
                dyndic.Add(id, nrec);
            }

            // Добавим в имена
            additional_names.OnAddItem(nrec, long.MinValue);

            // Убираем обратные ссылки
            foreach (object[] proplink in removed)
            {
                string prop = (string)proplink[0];
                string target = (string)proplink[1];
                // В узле target надо убрать обратные ссылки пары {prop, id}
                bool indyn = false;
                object node;
                if (dyndic.TryGetValue(target, out node)) indyn = true;
                else
                {
                    node = records.GetByKey(new object[] { target, null, null });
                }
                if (node != null)
                {
                    ((object[])node)[2] = ((object[])((object[])node)[2]).Cast<object[]>()
                        .Where(x => (int)x[0] != 3 || (string)((object[])x[1])[1] != id || (string)((object[])x[1])[0] != prop)
                        .ToArray();
                    if (indyn)
                    {
                        // Изменения внесены, поскольку узел уже на месте
                    }
                    else
                    {
                        dyndic.Add(target, node);
                    }
                }
            }

            // Добавляем обратные ссылки
            foreach (object[] proplink in appeared)
            {
                string prop = (string)proplink[0];
                string target = (string)proplink[1];
                // В узле target надо добавить обратную ссылку пары {prop, id}
                bool indyn = false;
                object node;
                if (dyndic.TryGetValue(target, out node)) indyn = true;
                else
                {
                    node = records.GetByKey(new object[] { target, null, null });
                }
                if (node != null)
                {
                    ((object[])node)[2] = ((object[])((object[])node)[2]).Cast<object[]>()
                        .Concat(new object[] { new object[] { 3, new object[] { prop, id } } })
                        .ToArray();
                    if (indyn)
                    {
                        // Изменения внесены, поскольку узел уже на месте
                    }
                    else
                    {
                        dyndic.Add(target, node);
                    }
                }
            }

            return null;
        }
        // Уничтожать запись под заданным идентификатором и возврат null. Не используется (?), используется PutItem с delete
        public override XElement Delete(string id)
        {
            throw new NotImplementedException();
        }

        public override void Close()
        {
            throw new NotImplementedException();
        }

    }
}
