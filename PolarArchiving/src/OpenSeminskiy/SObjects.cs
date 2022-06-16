using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace OpenSeminskiy
{
    public class SObjects
    {
        public static XElement look = new XElement("empty");

        private static string funds_name = "Фонды";
        public static string funds_id = null;
        public static XElement[] funds = new XElement[0];
        public static IComparer<string> comparedates;

        private static string path;
        public static void Init(string pth)
        {
            path = pth;
            OAData.Ontology.Init(path + "ontology_iis-v12-doc_ruen.xml");
            OAData.OADB.Init(path);
            //if (OAData.OADB.firsttime) OAData.OADB.Load(); // Вторая загрузка порождает ошибку!

            XElement fondy = SObjects.SearchByName(funds_name)
                .FirstOrDefault(x => SObjects.GetField(x, "http://fogid.net/o/name") == "Фонды");
            if (fondy != null)
            {
                XElement format_funds_coll = new XElement("record", new XAttribute("type", "http://fogid.net/o/collection"),
        new XElement("field", new XAttribute("prop", "http://fogid.net/o/name")),
        new XElement("inverse", new XAttribute("prop", "http://fogid.net/o/in-collection"),
            new XElement("record", new XAttribute("type", "http://fogid.net/o/collection-member"),
                new XElement("direct", new XAttribute("prop", "http://fogid.net/o/collection-item"),
                    new XElement("record", new XAttribute("type", "http://fogid.net/o/collection"),
                        new XElement("field", new XAttribute("prop", "http://fogid.net/o/name")),
                        new XElement("inverse", new XAttribute("prop", "http://fogid.net/o/reflected"),
                            new XElement("record", new XAttribute("type", "http://fogid.net/o/reflection"),
                                new XElement("direct", new XAttribute("prop", "http://fogid.net/o/in-doc"),
                                    new XElement("record", new XAttribute("type", "http://fogid.net/o/photo-doc"),
                                        new XElement("field", new XAttribute("prop", "http://fogid.net/o/uri")),
                                        new XElement("inverse", new XAttribute("prop", "http://fogid.net/o/forDocument"),
                                            new XElement("record", new XAttribute("type", "http://fogid.net/o/FileStore"),
                                                new XElement("field", new XAttribute("prop", "http://fogid.net/o/uri"))))
                                                )))),
                        null)))));
                funds_id = fondy.Attribute("id").Value;
                XElement funds_coll = SObjects.GetItemById(funds_id, format_funds_coll);
                funds = funds_coll.Elements("inverse")
                    .Select((XElement inv) => inv.Element("record").Element("direct").Element("record")).ToArray();
            }
            else
            {
                funds = new XElement[0];
            }

            //string fund_id = funds.First().Attribute("id").Value;

            comparedates = new SCompare();

        }
        public static void Init() { Init(path); }


        public static string GetField(XElement rec, string prop)
        {
            //string lang = null;
            string res = null;
            foreach (XElement f in rec.Elements("field"))
            {
                string p = f.Attribute("prop").Value;
                if (p != prop) continue;
                XAttribute xlang = f.Attribute("{http://www.w3.org/XML/1998/namespace}lang");
                res = f.Value;
                if (xlang?.Value == "ru") { break; }
            }
            return res;
        }
        private static string[] months = new[] { "янв", "фев", "мар", "апр", "май", "июн", "июл", "авг", "сен", "окт", "ноя", "дек" };
        public static string DatePrinted(string date)
        {
            if (date == null) return null;
            string[] split = date.Split('-');
            string str = split[0];
            if (split.Length > 1)
            {
                int month;
                if (Int32.TryParse(split[1], out month) && month > 0 && month <= 12)
                {
                    str += months[month - 1];
                    if (split.Length > 2) str += split[2].Substring(0, 2);
                }
            }
            return str;
        }
        public static string GetDates(XElement item)
        {
            var fd_el = item.Elements("field").FirstOrDefault(f => f.Attribute("prop").Value == "http://fogid.net/o/from-date");
            var td_el = item.Elements("field").FirstOrDefault(f => f.Attribute("prop").Value == "http://fogid.net/o/to-date");
            string res = (fd_el == null ? "" : fd_el.Value) + (td_el == null || string.IsNullOrEmpty(td_el.Value) ? "" : "—" + td_el.Value);
            return res;
        }


        public static XElement GetItemById(string id, XElement format)
        {
            return OAData.OADB.GetItemById(id, format);
        }
        public static XElement GetItemByIdBasic(string id, bool addinverse)
        {
            return OAData.OADB.GetItemByIdBasic(id, addinverse);
        }
        public static IEnumerable<XElement> SearchByName(string ss)
        {
            return OAData.OADB.SearchByName(ss);
        }

        public static IEnumerable<XElement> GetCollectionPath(string id)
        {
            //return _getCollectionPath(id);
            System.Collections.Generic.Stack<XElement> stack = new Stack<XElement>();
            XElement node = GetItemByIdBasic(id, false);
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
            XElement tree = GetItemById(id, formattoparentcollection);
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
