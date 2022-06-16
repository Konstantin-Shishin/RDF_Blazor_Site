﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;

namespace Fogid.DocumentStorage
{
    /// <summary>
    /// Простая база данных на основе Xml-построения. Xml база данных объединяет записи fog-документов
    /// без дублирования и цепочек эквивалентности. А еще имеется словарь идентификатор-{запись, список обратных элементов}. 
    /// </summary>
    public class XmlDbAdapter : DbAdapter
    {
        private class DbNode 
        {
            public string id; // Нужен поскольку определяющая запись может быть отсутствовать
            public XElement xel; 
            public List<XElement> inverse; 
        }
        private XElement db = null;
        private Dictionary<string, DbNode> records = null;
        private Action<string> errors = s => { Console.WriteLine(s); };
        /// <summary>
        /// Инициирование базы данных
        /// </summary>
        /// <param name="connectionstring">префикс варианта базы данных xml:, больше в connectionstring ничего не существенно </param>
        public override void Init(string connectionstring) {  }
        // Загрузка базы данных
        public override void StartFillDb(Action<string> turlog)
        {
            db = new XElement("db");
        }
        public override void LoadFromCassettesExpress(IEnumerable<string> fogfilearr, Action<string> turlog, Action<string> convertlog)
        {
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
            db.Save(filename);
        }

        public override void LoadXFlowUsingRiTable(IEnumerable<XElement> xflow)
        {
            foreach (XElement xel in xflow)
            {
                if (xel.Name == "{http://fogid.net/o/}delete" || xel.Name == "{http://fogid.net/o/}substitute") continue;
                var aboutatt = xel.Attribute(Fogid.Cassettes.ONames.rdfabout);
                if (aboutatt == null) continue;
                string id = aboutatt.Value;
                // выявим Resource Info
                ResInfo ri = null;
                if (!table_ri.TryGetValue(id, out ri)) continue; // если нет, то отбрасываем вариант (это на всякий случай)
                if (ri.removed) continue; // пропускаем уничтоженные
                if (ri.id != id || ri.processed) continue; // это не оригинал или запись уже обрабатывалась
                // Выявляем временную отметку
                DateTime modificationTime_new = DateTime.MinValue;
                XAttribute mt = xel.Attribute("mT");
                if (mt != null) DateTime.TryParse(mt.Value, out modificationTime_new);
                modificationTime_new = modificationTime_new.ToUniversalTime();

                if (modificationTime_new != ri.timestamp) continue; // отметка времени не совпала

                db.Add(xel);
                ri.processed = true; // Это на случай, если будет второй оригинал, он обрабатываться не будет.
            }
        }
        public override void FinishFillDb(Action<string> turlog)
        {
            records = new Dictionary<string, DbNode>();
            DbNode node;
            foreach (XElement xel in db.Elements())
            {
                // Это есть запись, которую надо зафиксировать под ее именем
                string id = xel.Attribute(Fogid.Cassettes.ONames.rdfabout).Value;
                if (records.TryGetValue(id, out node))
                { // есть определение
                    node.xel = xel;
                }
                else
                {
                    records.Add(id, new DbNode() { id = id, xel = xel, inverse = new List<XElement>() });
                }
                // Теперь проанализируем прямые ссылки
                foreach (XElement el in xel.Elements())
                {
                    XAttribute res_att = el.Attribute(Fogid.Cassettes.ONames.rdfresource);
                    if (res_att == null) continue;
                    string resource_id = res_att.Value;
                    if (records.TryGetValue(resource_id, out node))
                    { // есть определение
                        node.inverse.Add(el);
                    }
                    else
                    {
                        records.Add(resource_id, 
                            new DbNode() 
                            { 
                                id = resource_id, 
                                inverse = Enumerable.Repeat<XElement>(el, 1).ToList() 
                            });
                    }
                }
            }
        }

        public override IEnumerable<XElement> SearchByName(string searchstring)
        {
            string ss = searchstring.ToLower();
            var query = db.Elements()
                .SelectMany(xel => xel.Elements().Where(el => el.Name == "{http://fogid.net/o/}name"))
                .Where(el => el.Value.ToLower().StartsWith(ss))
                .Select(el => new XElement("record", new XAttribute("id", el.Parent.Attribute(Fogid.Cassettes.ONames.rdfabout).Value),
                    new XAttribute("type", el.Parent.Name.NamespaceName + el.Parent.Name.LocalName),
                    new XElement("field", new XAttribute("prop", "http://fogid.net/o/name"), el.Value)));
                //.Select(el => new XElement("record", new XAttribute("id", el.Parent.Attribute(Fogid.Cassettes.ONames.rdfabout).Value)));
            return query;
        }
        public override XElement GetItemByIdBasic(string id, bool addinverse)
        {
            DbNode node;
            if (records.TryGetValue(id, out node))
            {
                XElement xel = node.xel;
                string type = xel.Name.NamespaceName + xel.Name.LocalName;
                return new XElement("record", new XAttribute("id", id),
                    new XAttribute("type", type),
                    xel.Elements()
                    .Select<XElement, XElement>(el => 
                    {
                        XAttribute resource = el.Attribute(Fogid.Cassettes.ONames.rdfresource);
                        string prop = el.Name.NamespaceName + el.Name.LocalName;
                        if (resource == null)
                        {
                            return new XElement("field", new XAttribute("prop", prop), el.Value);
                        }
                        else
                        {
                            //if (el.Name == Fogid.Cassettes.ONames.rdftype) return null;
                            return new XElement("direct", new XAttribute("prop", prop),
                                new XElement("record", new XAttribute("id", resource.Value)));
                        }
                    }),
                    addinverse ?
                    node.inverse
                    .Select(inv => new XElement("inverse", new XAttribute("prop", inv.Name.NamespaceName + inv.Name.LocalName),
                        new XElement("record", new XAttribute("id", inv.Parent.Attribute(Fogid.Cassettes.ONames.rdfabout).Value)))) :
                    null); 
            }
            return null;
        }
        public override XElement GetItemById(string id, XElement format)
        {
            DbNode node;
            if (!records.TryGetValue(id, out node)) return null;
            return GetItemByNode(node, format);
        }
        private XElement GetItemByNode(DbNode node, XElement format)
        {
            XElement xel = node.xel;
            if (xel == null) return null;
            string type = xel.Name.NamespaceName + xel.Name.LocalName;
            return new XElement("record", 
                new XAttribute("id", xel.Attribute(Fogid.Cassettes.ONames.rdfabout).Value),
                new XAttribute("type", type),
                format.Elements().Where(fel => fel.Name == "field" || fel.Name == "direct" || fel.Name == "inverse")
                .SelectMany(fel =>
                {
                    string prop = fel.Attribute("prop").Value;
                    if (fel.Name == "field")
                    {
                        return xel.Elements()
                            .Where(el => el.Name.NamespaceName + el.Name.LocalName == prop)
                            .Select(el => new XElement("field", new XAttribute("prop", prop),
                                el.Value));
                    }
                    else if (fel.Name == "direct")
                    {
                        return xel.Elements()
                            .Where(el => el.Name.NamespaceName + el.Name.LocalName == prop)
                            .Select<XElement, XElement>(el => 
                            {
                                DbNode node2;
                                if (!records.TryGetValue(el.Attribute(Fogid.Cassettes.ONames.rdfresource).Value, out node2) || node2.xel == null) return null;
                                string t = node2.xel.Name.NamespaceName + node2.xel.Name.LocalName;
                                XElement f = fel.Elements("record")
                                    .FirstOrDefault(fr => 
                                    {
                                        XAttribute t_att = fr.Attribute("type");
                                        if (t_att == null) return true;
                                        return t_att.Value == t;
                                    });
                                if (f == null) return null;
                                return new XElement("direct", new XAttribute("prop", prop),
                                    GetItemByNode(node2, f));
                            });
                    }
                    else if (fel.Name == "inverse")
                    {
                        //return node.inverse
                        //    .Where(el => el.Name.NamespaceName + el.Name.LocalName == prop)
                        //    .Select(el => new XElement("inverse", new XAttribute("prop", prop),
                        //        fel.Element("record") == null ? null :
                        //        GetItemById(el.Parent.Attribute(Fogid.Cassettes.ONames.rdfabout).Value, fel.Element("record"))));
                        return node.inverse
                            .Where(el => el.Name.NamespaceName + el.Name.LocalName == prop)
                            .Select<XElement, XElement>(el =>
                            {
                                DbNode node2;
                                if (!records.TryGetValue(el.Parent.Attribute(Fogid.Cassettes.ONames.rdfabout).Value, out node2) || node2.xel == null) return null;
                                string t = node2.xel.Name.NamespaceName + node2.xel.Name.LocalName;
                                XElement f = fel.Elements("record")
                                    .FirstOrDefault(fr =>
                                    {
                                        XAttribute t_att = fr.Attribute("type");
                                        if (t_att == null) return true;
                                        return t_att.Value == t;
                                    });
                                if (f == null) return null;
                                return new XElement("inverse", new XAttribute("prop", prop),
                                    GetItemByNode(node2, f));
                            });
                    }
                    else return null;
                }));
        }

        public override XElement GetItemByIdSpecial(string id)
        {
            return GetItemByIdBasic(id, true);
        }


        private XElement RemoveRecord(string id)
        {
            DbNode node;
            if (records.TryGetValue(id, out node))
            {
                // Удаляемый объект состоит из записи и из элементов-ссылок, попавших в "чужие" списки
                // Сначала "работаем" по чужим спискам
                XElement xel = node.xel;
                foreach (XElement el in xel.Elements())
                {
                    XAttribute resource = el.Attribute(Fogid.Cassettes.ONames.rdfresource);
                    if (resource == null) continue;
                    // находим список
                    string id2 = resource.Value;
                    DbNode node2;
                    if (records.TryGetValue(id2, out node2))
                    {
                        // Уберем из списка
                        node2.inverse.Remove(el);
                    }
                }
                // Открепляем из базы данных
                xel.Remove();
                return xel;
            }
            return null;
        }
        public override XElement Delete(string id)
        {
            XElement record = RemoveRecord(id);
            if (record == null) return null;
            records.Remove(id);
            return record;
        }

        public override XElement Add(XElement record)
        {
            // Работаем по "чужим спискам
            XElement xel = new XElement(record);
            foreach (XElement el in xel.Elements())
            {
                XAttribute resource = el.Attribute(Fogid.Cassettes.ONames.rdfresource);
                if (resource == null) continue;
                // находим список
                string id2 = resource.Value;
                DbNode node2;
                if (records.TryGetValue(id2, out node2))
                {
                    // добавляем в список
                    node2.inverse.Add(el);
                }
            }

            // Прикрепляем (зачем-то) запись к базе данных
            db.Add(xel);
            // Добавляем к словарю
            string id = record.Attribute(Fogid.Cassettes.ONames.rdfabout).Value;

            DbNode node;
            if (records.TryGetValue(id, out node))
            { // Если узел есть, то прикрепляем к полю узла
                node.xel = xel;
            }
            else
            { // Если узла нет, то создаем
                records.Add(id, new DbNode() { id = id, xel = xel, inverse = new List<XElement>() });
            }
            return xel;
        }

        public override XElement AddUpdate(XElement record)
        {
            string id = record.Attribute(Fogid.Cassettes.ONames.rdfabout).Value;
            DbNode node;
            if (records.TryGetValue(id, out node))
            {
                // Будем перебирать элементы "старого" значения и, если таких нет, добавлять в новое
                foreach (XElement old_el in node.xel.Elements())
                {
                    XAttribute l_att = old_el.Attribute("{http://www.w3.org/XML/1998/namespace}lang");
                    if (record.Elements().Any(el => el.Name == old_el.Name &&
                        (l_att == null ? true :
                            (el.Attribute("{http://www.w3.org/XML/1998/namespace}lang") != null &&
                                l_att.Value == el.Attribute("{http://www.w3.org/XML/1998/namespace}lang").Value)))) continue;
                    record.Add(old_el);
                }
                RemoveRecord(id);
            }
            Add(record);
            return record;
        }
    }
}
