using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading.Tasks;
using System.Xml.Linq;
using Microsoft.AspNetCore.Http;


namespace SoranCore2.Models
{
    internal class PhotosFirst : IComparer<XElement>
    {
        public int Compare(XElement x, XElement y)
        {
            int cmp1 = x.Attribute("type").Value.CompareTo(y.Attribute("type").Value);
            if (cmp1 != 0) return -cmp1;
            string s1 = StaticObjects.GetField(x, "http://fogid.net/o/from-date");
            string s2 = StaticObjects.GetField(y, "http://fogid.net/o/from-date");
            if (string.IsNullOrEmpty(s1)) s1 = "_";// + x.Attribute("id").Value;
            if (string.IsNullOrEmpty(s2)) s2 = "_";// + y.Attribute("id").Value;
            int cmp2 = s1.CompareTo(s2); //string.Compare(s1, s2);
            if (cmp2 != 0) return cmp2;
            return x.Attribute("id").Value.CompareTo(y.Attribute("id").Value);
        }
        private static PhotosFirst _phf = null;
        public static PhotosFirst PhF() { if (_phf == null) _phf = new PhotosFirst(); return _phf; } 
    }
    internal class IdEqual : IEqualityComparer<XElement>
    {
        public bool Equals(XElement x, XElement y)
        {
            if (x == null || y == null) return true;
            return x.Element("direct")?.Element("record")?.Attribute("id").Value == y.Element("direct")?.Element("record")?.Attribute("id").Value;
        }
        public int GetHashCode([DisallowNull] XElement obj)
        {
            return string.GetHashCode(obj.Element("direct")?.Element("record")?.Attribute("id").Value);
        }
    }

    public class IndexModel
    {

        // Для отладки:
        public string look = "";
        public string ir = null; // база массива отражений документов через отражения
        public string ic = null; // база массива отражений документов через членство в коллекциях. Бывает либо то, либо другое

        private string sdirection = "person";
        public int page_length = 40;
        private int _pg = 0;
        public int Pg {  get { return _pg;  } set { _pg = value; } }
        public string TabDirection { get { return sdirection; } set { sdirection = value; } }
        // Поиск дает две строки: id и name
        private IEnumerable<object[]> _sresults = null;
        public IEnumerable<object[]> SearchResults { get { return _sresults; } set { _sresults = value; } }
        // Портрет состоит из идентификатора, типа, множества полей 
        public string Id { get; set; }
        public string Type { get; set; }
        public string Name { get; set; }
        public string StartDate { get; set; }
        public string EndDate { get; set; }
        public string Description { get; set; }
        public string Uri { get; set; }
        public string Docmetainfo { get; set; }

        // Главное поле
        public XElement XRecord { get; set; }

        // ============= То, что попало в вьюер, а теперь извлечено
        public Tuple<string, string>[] titles = null;
        public string portraitphotouri = null;
        public XElement[] reflected_reflections = null;
        public XElement[] work = null;
        public XElement[] parties = null;
        public XElement[] firstfaces = null;
        public XElement[] allparticipants = null;
        public Tuple<string, string, string>[] naming = null;
        public XElement[] indocreflected = null;
        public XElement[] incollectionmember = null;
        public XElement[] orgparentorgrelatives = null;
        public XElement[] adocauthority = null;

        public Func<string, string> ImgSrc = (string tp) => tp == "http://fogid.net/o/document" ? "~/img/document_s.jpg" :
            (tp == "http://fogid.net/o/photo-doc" ? "~/Docs/GetPhoto?s=small&u=" :
            (tp == "http://fogid.net/o/video" ? "~/img/video_s.jpg" :
            (tp == "http://fogid.net/o/audio" ? "~/img/audio_s.jpg" :
            (tp == "http://fogid.net/o/org-sys" ? "~/img/org_s.jpg" :
            (tp == "http://fogid.net/o/collection" ? "~/img/collection_s.jpg" : "~/img/document_s.jpg")))));

        public string[] docidarr = null; // массив идентификаторов документов в reflected_reflections 
        internal static Dictionary<string, string[]> ir_dic = new Dictionary<string, string[]>(); 
        public void BuildPortrait()
        {
            adocauthority = this.XRecord.Elements("inverse")
               .Where(i => i.Attribute("prop").Value == "http://fogid.net/o/adoc")
               .Select(i => i.Element("record"))
               .ToArray();

            reflected_reflections = this.XRecord.Elements("inverse")
               .Where(i => i.Attribute("prop").Value == "http://fogid.net/o/reflected")
               .Select(i => i.Element("record"))
               .Distinct(new IdEqual())
               .OrderBy(r => r.Element("direct")?.Element("record"), PhotosFirst.PhF())
               .ToArray();

            portraitphotouri = reflected_reflections
                .Where(re => re.Elements("field").FirstOrDefault(f => f.Attribute("prop").Value == "http://fogid.net/o/ground")?.Value == "portrait")
                .Select(re => re.Element("direct")?.Element("record"))
                .Where(r => r?.Attribute("type").Value == "http://fogid.net/o/photo-doc")
                .FirstOrDefault()
                ?.Elements("field")
                .FirstOrDefault(f => f.Attribute("prop").Value == "http://fogid.net/o/uri")?.Value;

            titles = this.XRecord.Elements("inverse")
                .Where(i => i.Attribute("prop").Value == "http://fogid.net/o/has-title")
                .Select(i => i.Element("record"))
                .Select<XElement, Tuple<string, string>>(r => Tuple.Create<string, string>(
                    r.Elements("field").FirstOrDefault(f => f.Attribute("prop")?.Value == "http://fogid.net/o/degree")?.Value,
                    r.Elements("field").FirstOrDefault(f => f.Attribute("prop")?.Value == "http://fogid.net/o/from-date")?.Value
                    ))
                .OrderBy(t => t.Item2)
                .ToArray();

            XElement[] participant_participation = this.XRecord.Elements("inverse")
               .Where(i => i.Attribute("prop").Value == "http://fogid.net/o/participant")
               .Select(i => i.Element("record"))
               .ToArray();
            work = participant_participation
               .Where(r => r.Element("direct")?.Element("record")?.Elements("field")
                    .FirstOrDefault(f => f.Attribute("prop").Value == "http://fogid.net/o/org-classification")?.Value == "organization")
               .ToArray();
            parties = participant_participation
               .Where(r => r.Element("direct")?.Element("record")?.Elements("field")
                    .FirstOrDefault(f => f.Attribute("prop").Value == "http://fogid.net/o/org-classification")?.Value != "organization")
               .ToArray();


            naming = this.XRecord.Elements("inverse")
                .Where(i => i.Attribute("prop").Value == "http://fogid.net/o/referred-sys")
                .Select(i => i.Element("record"))
                .Select<XElement, Tuple<string, string, string>>(r => Tuple.Create<string, string, string>(
                    r.Elements("field").FirstOrDefault(f => f.Attribute("prop")?.Value == "http://fogid.net/o/alias")?.Value,
                    r.Elements("field").FirstOrDefault(f => f.Attribute("prop")?.Value == "http://fogid.net/o/from-date")?.Value,
                    r.Elements("field").FirstOrDefault(f => f.Attribute("prop")?.Value == "http://fogid.net/o/to-date")?.Value
                    ))
                .OrderBy(t => t.Item2)
                .ToArray();
            var inorg_participation = this.XRecord.Elements("inverse")
               .Where(i => i.Attribute("prop").Value == "http://fogid.net/o/in-org")
               .Select(i => i.Element("record"))
               .ToArray();
            firstfaces = inorg_participation
                .Where(r => StaticObjects.GetField(r, "http://fogid.net/o/role-classification") == "director")
                .ToArray();
            allparticipants = inorg_participation
                .Where(r => StaticObjects.GetField(r, "http://fogid.net/o/role-classification") != "director")
                .ToArray();
            //inorg_participation = this.XRecord.Elements("inverse")
            //    .Select(i => i.Element("record"))
            //    .OrderBy(r => StaticObjects.GetField(r.Element("direct").Element("record"), "http://fogid.net/o/from-date"))
            //    .ToArray();
            indocreflected = this.XRecord.Elements("inverse")
                .Where(i => i.Attribute("prop").Value == "http://fogid.net/o/in-doc")
                .Select(i => i.Element("record"))
                .ToArray();
            incollectionmember = this.XRecord.Elements("inverse")
                .Where(i => i.Attribute("prop").Value == "http://fogid.net/o/in-collection")
                .Select(i => i.Element("record")?.Element("direct")?.Element("record"))
                .Where(r => r != null)
                .OrderBy(r => StaticObjects.GetField(r, "http://fogid.net/o/name"))
                .ToArray();
            orgparentorgrelatives = this.XRecord.Elements("inverse")
                .Where(i => i.Attribute("prop").Value == "http://fogid.net/o/org-parent")
                .Select(i => i.Element("record")?.Element("direct")?.Element("record"))
                .Where(r => r != null)
                .ToArray();
        }

        internal void Check_docidarr(string id, string[] arr)
        {
            if (ir_dic.ContainsKey(id))
            {
                string[] saved = ir_dic[id];
                if (arr.Length != saved.Length) throw new Exception("Длины не равны");
                for (int i = 0; i < arr.Length; i++)
                    if (arr[i] != saved[i]) throw new Exception("Значения не равны");
            }
            else ir_dic.Add(id, arr);
        }

        // =============

        // Множество форматов
        private static Dictionary<string, XElement> _formats = null;
        public Dictionary<string, XElement> Formats { get { return _formats; } set { _formats = value; } }

        // Конструктор иногда вычисляет
        public IndexModel()
        {
            if (_formats == null)
            {
                XElement xformats = new XElement("formats", 
                    new XElement("record", new XAttribute("type", "http://fogid.net/o/person"), 
                        new XElement("field", new XAttribute("prop", "http://fogid.net/o/name")),
                        new XElement("field", new XAttribute("prop", "http://fogid.net/o/from-date")),
                        new XElement("field", new XAttribute("prop", "http://fogid.net/o/to-date")),
                        new XElement("field", new XAttribute("prop", "http://fogid.net/o/description")),
                        new XElement("inverse", new XAttribute("prop", "http://fogid.net/o/reflected"),
                            new XElement("record",
                                new XElement("field", new XAttribute("prop", "http://fogid.net/o/ground")),
                                new XElement("direct", new XAttribute("prop", "http://fogid.net/o/in-doc"),
                                    new XElement("record",
                                        new XElement("field", new XAttribute("prop", "http://fogid.net/o/name")),
                                        new XElement("field", new XAttribute("prop", "http://fogid.net/o/uri")),
                                        new XElement("field", new XAttribute("prop", "http://fogid.net/o/docmetainfo")),
                                        new XElement("field", new XAttribute("prop", "http://fogid.net/o/description")),
                                        new XElement("field", new XAttribute("prop", "http://fogid.net/o/from-date")),
                                        new XElement("inverse", new XAttribute("prop", "http://fogid.net/o/in-doc"),
                                            new XElement("record",
                                                new XElement("field", new XAttribute("prop", "http://fogid.net/o/ground")))), 
                                        null)))),
                        new XElement("inverse", new XAttribute("prop", "http://fogid.net/o/participant"),
                            new XElement("record",
                                new XElement("field", new XAttribute("prop", "http://fogid.net/o/role-classification")),
                                new XElement("field", new XAttribute("prop", "http://fogid.net/o/role")),
                                new XElement("field", new XAttribute("prop", "http://fogid.net/o/from-date")),
                                new XElement("direct", new XAttribute("prop", "http://fogid.net/o/in-org"),
                                    new XElement("record",
                                        new XElement("field", new XAttribute("prop", "http://fogid.net/o/name")),
                                        new XElement("field", new XAttribute("prop", "http://fogid.net/o/org-classification")),
                                        null)))),
                        new XElement("inverse", new XAttribute("prop", "http://fogid.net/o/has-title"),
                            new XElement("record",
                                new XElement("field", new XAttribute("prop", "http://fogid.net/o/degree")),
                                new XElement("field", new XAttribute("prop", "http://fogid.net/o/from-date")),
                            null)),
                        new XElement("inverse", new XAttribute("prop", "http://fogid.net/o/referred-sys"),
                            new XElement("record",
                                new XElement("field", new XAttribute("prop", "http://fogid.net/o/alias")),
                                new XElement("field", new XAttribute("prop", "http://fogid.net/o/from-date")),
                                new XElement("field", new XAttribute("prop", "http://fogid.net/o/to-date")),
                                null)),
                        null),
                    new XElement("record", new XAttribute("type", "http://fogid.net/o/org-sys"),
                        new XElement("field", new XAttribute("prop", "http://fogid.net/o/name")),
                        new XElement("field", new XAttribute("prop", "http://fogid.net/o/from-date")),
                        new XElement("field", new XAttribute("prop", "http://fogid.net/o/to-date")),
                        new XElement("field", new XAttribute("prop", "http://fogid.net/o/description")),
                        new XElement("inverse", new XAttribute("prop", "http://fogid.net/o/reflected"),
                            new XElement("record",
                                new XElement("field", new XAttribute("prop", "http://fogid.net/o/ground")),
                                new XElement("direct", new XAttribute("prop", "http://fogid.net/o/in-doc"),
                                    new XElement("record",
                                        new XElement("field", new XAttribute("prop", "http://fogid.net/o/name")),
                                        new XElement("field", new XAttribute("prop", "http://fogid.net/o/uri")),
                                        new XElement("field", new XAttribute("prop", "http://fogid.net/o/docmetainfo")),
                                        new XElement("field", new XAttribute("prop", "http://fogid.net/o/description")),
                                        new XElement("field", new XAttribute("prop", "http://fogid.net/o/from-date")),
                                        null)))),
                        new XElement("inverse", new XAttribute("prop", "http://fogid.net/o/in-org"),
                            new XElement("record",
                                new XElement("field", new XAttribute("prop", "http://fogid.net/o/role-classification")),
                                new XElement("field", new XAttribute("prop", "http://fogid.net/o/role")),
                                new XElement("field", new XAttribute("prop", "http://fogid.net/o/from-date")),
                                new XElement("direct", new XAttribute("prop", "http://fogid.net/o/participant"),
                                    new XElement("record",
                                        new XElement("field", new XAttribute("prop", "http://fogid.net/o/name")),
                                        null)))),
                        new XElement("inverse", new XAttribute("prop", "http://fogid.net/o/org-parent"),
                            new XElement("record",
                                new XElement("field", new XAttribute("prop", "http://fogid.net/o/from-date")),
                                new XElement("direct", new XAttribute("prop", "http://fogid.net/o/org-child"),
                                    new XElement("record",
                                        new XElement("field", new XAttribute("prop", "http://fogid.net/o/name")),
                                        null)))),
                        new XElement("inverse", new XAttribute("prop", "http://fogid.net/o/org-child"),
                            new XElement("record",
                                new XElement("field", new XAttribute("prop", "http://fogid.net/o/from-date")),
                                new XElement("direct", new XAttribute("prop", "http://fogid.net/o/org-parent"),
                                    new XElement("record",
                                        new XElement("field", new XAttribute("prop", "http://fogid.net/o/name")),
                                        null)))),
                        new XElement("inverse", new XAttribute("prop", "http://fogid.net/o/referred-sys"),
                            new XElement("record",
                                new XElement("field", new XAttribute("prop", "http://fogid.net/o/alias")),
                                new XElement("field", new XAttribute("prop", "http://fogid.net/o/from-date")),
                                new XElement("field", new XAttribute("prop", "http://fogid.net/o/to-date")),
                                null)),
                        null),
                    new XElement("record", new XAttribute("type", "http://fogid.net/o/document"),
                        new XElement("field", new XAttribute("prop", "http://fogid.net/o/name")),
                        new XElement("field", new XAttribute("prop", "http://fogid.net/o/from-date")),
                        new XElement("field", new XAttribute("prop", "http://fogid.net/o/description")),
                        new XElement("field", new XAttribute("prop", "http://fogid.net/o/uri")),
                        new XElement("field", new XAttribute("prop", "http://fogid.net/o/docmetainfo")),
                        new XElement("inverse", new XAttribute("prop", "http://fogid.net/o/in-doc"),
                            new XElement("record",
                                new XElement("field", new XAttribute("prop", "http://fogid.net/o/ground")),
                                new XElement("direct", new XAttribute("prop", "http://fogid.net/o/reflected"),
                                    new XElement("record",
                                        new XElement("field", new XAttribute("prop", "http://fogid.net/o/name")),
                                        null)))),
                        new XElement("inverse", new XAttribute("prop", "http://fogid.net/o/adoc"),
                            new XElement("record",
                                new XElement("field", new XAttribute("prop", "http://fogid.net/o/authority-specificator")),
                                new XElement("direct", new XAttribute("prop", "http://fogid.net/o/author"),
                                    new XElement("record",
                                        new XElement("field", new XAttribute("prop", "http://fogid.net/o/name")),
                                        null)))),
                        new XElement("inverse", new XAttribute("prop", "http://fogid.net/o/reflected"),
                            new XElement("record",
                                new XElement("field", new XAttribute("prop", "http://fogid.net/o/position")),
                                new XElement("direct", new XAttribute("prop", "http://fogid.net/o/in-doc"),
                                    new XElement("record",
                                        new XElement("field", new XAttribute("prop", "http://fogid.net/o/uri")),
                                        new XElement("field", new XAttribute("prop", "http://fogid.net/o/docmetainfo")),
                                        new XElement("field", new XAttribute("prop", "http://fogid.net/o/name")))),
                                null)),
                        null),
                    new XElement("record", new XAttribute("type", "http://fogid.net/o/collection"),
                        new XElement("field", new XAttribute("prop", "http://fogid.net/o/name")),
                        new XElement("field", new XAttribute("prop", "http://fogid.net/o/from-date")),
                        new XElement("field", new XAttribute("prop", "http://fogid.net/o/description")),
                        new XElement("inverse", new XAttribute("prop", "http://fogid.net/o/in-collection"),
                            new XElement("record",
                                new XElement("field", new XAttribute("prop", "http://fogid.net/o/ground")),
                                new XElement("direct", new XAttribute("prop", "http://fogid.net/o/collection-item"),
                                    new XElement("record",
                                        new XElement("field", new XAttribute("prop", "http://fogid.net/o/name")),
                                        new XElement("field", new XAttribute("prop", "http://fogid.net/o/from-date")),
                                        new XElement("field", new XAttribute("prop", "http://fogid.net/o/uri")),
                                        new XElement("field", new XAttribute("prop", "http://fogid.net/o/docmetainfo")),
                                        new XElement("field", new XAttribute("prop", "http://fogid.net/o/description")),
                                        null)))),
                        null),
                    null);

                _formats = xformats.Elements("record").ToDictionary<XElement, string, XElement>(x => x.Attribute("type").Value, x => x);
            }
        }

    }
}
