using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace OpenSeminskiy.Models
{
    public class IndexModel
    {
        public string name;
        public IEnumerable<XElement> fonds { get { return SObjects.funds; } }
        public IndexModel()
        {
            
        }
        private static XElement format = new XElement("record", new XAttribute("type", "http://fogid.net/o/collection"),
        new XElement("field", new XAttribute("prop", "http://fogid.net/o/name")),
        new XElement("inverse", new XAttribute("prop", "http://fogid.net/o/in-collection"),
            new XElement("record", new XAttribute("type", "http://fogid.net/o/collection-member"),
                new XElement("direct", new XAttribute("prop", "http://fogid.net/o/collection-item"),
                    new XElement("record", new XAttribute("type", "http://fogid.net/o/collection"),
                        new XElement("inverse", new XAttribute("prop", "http://fogid.net/o/reflected"),
                            new XElement("record", new XAttribute("type", "http://fogid.net/o/reflection"),
                                new XElement("direct", new XAttribute("prop", "http://fogid.net/o/in-doc"),
                                    new XElement("record", new XAttribute("type", "http://fogid.net/o/photo-doc"),
                                        new XElement("field", new XAttribute("prop", "http://fogid.net/o/uri")),
                                        new XElement("inverse", new XAttribute("prop", "http://fogid.net/o/forDocument"),
                                            new XElement("record", new XAttribute("type", "http://fogid.net/o/FileStore"),
                                                new XElement("field", new XAttribute("prop", "http://fogid.net/o/uri"))))
                                                )))),
                        new XElement("field", new XAttribute("prop", "http://fogid.net/o/name")))))));

    }

    public class ExtSearchModel
    {
        public IEnumerable<XElement> funds { get { return SObjects.funds; } }
        public IComparer<string> comparedates { get { return SObjects.comparedates;  } }

        public ExtSearchModel()
        { Init(); }
        private void Init()
        {
        }
        //internal class SCompare : IComparer<string>
        //{
        //    public int Compare(string s1, string s2)
        //    {
        //        if (string.IsNullOrEmpty(s1) && string.IsNullOrEmpty(s2)) return 0;
        //        if (string.IsNullOrEmpty(s1)) return 1;
        //        if (string.IsNullOrEmpty(s2)) return -1;
        //        return s1.CompareTo(s2);
        //    }
        //    public static SCompare comparer = new SCompare();
        //}

        public string fund = null, context = null, person = null, org = null, coll = null, geo = null, fdate = null, tdate = null;
        public bool IsPost = false;
        public IEnumerable<XElement> query = Enumerable.Empty<XElement>();

        public ExtSearchModel(string fund, string context, string person, string org, string coll, string geo, string fdate, string tdate)
        {
            Init();
            //string fund = Request.Form["fund"];
            //string context = Request.Form["context"];
            //string person = Request.Form["person"];
            //string org = Request.Form["org"];
            //string coll = Request.Form["coll"];
            //string geo = Request.Form["geo"];
            //string fdate = Request.Form["fdate"];
            //string tdate = Request.Form["tdate"];
            this.fund = fund; this.context = context; this.person = person; this.org = org; this.coll = coll; this.geo = geo; this.fdate = fdate; this.tdate = tdate;
            IsPost = true;

            IEnumerable<string> searchresults = CollectAllChildDocumentIds(fund);


            var xarr = searchresults.Distinct().ToArray();
            XElement searchformat = new XElement("record",
                new XElement("field", new XAttribute("prop", "http://fogid.net/o/name")),
                new XElement("field", new XAttribute("prop", "http://fogid.net/o/from-date")),
                string.IsNullOrEmpty(context) ? null : new XElement("field", new XAttribute("prop", "http://fogid.net/o/description")),
                // Отражения
                string.IsNullOrEmpty(person) && string.IsNullOrEmpty(org) ? null : new XElement("inverse", new XAttribute("prop", "http://fogid.net/o/in-doc"),
                    new XElement("record",
                        new XElement("direct", new XAttribute("prop", "http://fogid.net/o/reflected"),
                            new XElement("record",
                                new XElement("field", new XAttribute("prop", "http://fogid.net/o/name")))))),
                // Авторство документов
                string.IsNullOrEmpty(person) && string.IsNullOrEmpty(org) ? null : new XElement("inverse", new XAttribute("prop", "http://fogid.net/o/adoc"),
                    new XElement("record",
                        new XElement("field", new XAttribute("prop", "http://fogid.net/o/authority-specificator")),
                        new XElement("direct", new XAttribute("prop", "http://fogid.net/o/author"),
                            new XElement("record",
                                new XElement("field", new XAttribute("prop", "http://fogid.net/o/name")))))),
                // Геоинформация
                string.IsNullOrEmpty(geo) ? null : new XElement("inverse", new XAttribute("prop", "http://fogid.net/o/something"),
                    new XElement("record",
                        new XElement("direct", new XAttribute("prop", "http://fogid.net/o/location-place"),
                            new XElement("record",
                                new XElement("field", new XAttribute("prop", "http://fogid.net/o/name")))))),
                // В коллекциях
                string.IsNullOrEmpty(coll) ? null : new XElement("inverse", new XAttribute("prop", "http://fogid.net/o/collection-item"),
                    new XElement("record",
                        new XElement("direct", new XAttribute("prop", "http://fogid.net/o/in-collection"),
                            new XElement("record",
                                new XElement("field", new XAttribute("prop", "http://fogid.net/o/name")))))),

                null);

            // =========== Формирование результата поиска ============
            query = searchresults
                //.Select(id => StaticObjects.Engine.GetItemByIdBasic(id, false))
                .Select(id => SObjects.GetItemById(id, searchformat))
                ;
            if (!string.IsNullOrEmpty(context))
            {
                string[] words = context.Split(' ')
                    .Where(w => !string.IsNullOrEmpty(w))
                    .Select(w => w.ToLower())
                    .ToArray();
                query = query
                    .Where(x =>
                    {
                        string text = SObjects.GetField(x, "http://fogid.net/o/name").ToLower();
                        string descr = SObjects.GetField(x, "http://fogid.net/o/description");
                        if (descr != null)
                        {
                            text = text + " " + descr.ToLower();
                        }
                        if (words.All(w => text.Contains(w))) { return true; }

                        return false;
                    });
            }
            if (!string.IsNullOrEmpty(person))
            {
                string word = person.ToLower();
                query = query
                    .Where(x =>
                    {
                        XElement xrec = x.Elements("inverse")
                            .Where(xi => xi.Attribute("prop").Value == "http://fogid.net/o/in-doc")
                            .Select(xi => xi.Element("record")?.Element("direct")?.Element("record"))
                            .FirstOrDefault();
                        if (xrec == null) { return false; }
                        string text = SObjects.GetField(xrec, "http://fogid.net/o/name").ToLower();
                        return text.StartsWith(word);
                    });
            }
            if (!string.IsNullOrEmpty(org))
            {
                string word = org.ToLower();
                query = query
                    .Where(x =>
                    {
                        XElement xrec = x.Elements("inverse")
                            .Where(xi => xi.Attribute("prop").Value == "http://fogid.net/o/in-doc")
                            .Select(xi => xi.Element("record").Element("direct").Element("record"))
                            .FirstOrDefault();
                        if (xrec == null) { return false; }
                        string text = SObjects.GetField(xrec, "http://fogid.net/o/name").ToLower();
                        return text.StartsWith(word);
                    });
            }
        }

        private static XElement formattochildfromcollection =
            new XElement("record",
                new XElement("inverse", new XAttribute("prop", "http://fogid.net/o/in-collection"),
                    new XElement("record",
                        new XElement("direct", new XAttribute("prop", "http://fogid.net/o/collection-item"),
                            new XElement("record",
                                new XElement("field", new XAttribute("prop", "http://fogid.net/o/name")))))));
        public static IEnumerable<string> CollectAllChildDocumentIds(string id)
        {
            XElement xt = SObjects.GetItemById(id, formattochildfromcollection);
            if (xt == null) return Enumerable.Empty<string>();
            var query = xt.Elements("inverse")
                .Where(xi => xi.Attribute("prop").Value == "http://fogid.net/o/in-collection")
                .SelectMany(xi =>
                {
                    XElement child = xi.Element("record")?.Element("direct")?.Element("record");
                    if (child == null) { return Enumerable.Empty<string>(); }
                    if (child.Attribute("type").Value == "http://fogid.net/o/collection") return CollectAllChildDocumentIds(child.Attribute("id").Value);
                    else if (child.Attribute("type").Value == "http://fogid.net/o/document") return new string[] { child.Attribute("id").Value };
                    else return Enumerable.Empty<string>();
                });
            return query;
        }

    }
    public class PortraitPersonModel
    {
        public string id;
        public string type_id = "http://fogid.net/o/person";
        public string name;
        public string dates;
        public string description;
        public IEnumerable<XElement> titles;
        public IEnumerable<XElement> works;
        public IEnumerable<XElement> notworks;
        public IEnumerable<XElement> livings;
        public IEnumerable<XElement> auth;
        public IEnumerable<XElement> refl_docs;

        private static XElement format = new XElement("record", new XAttribute("type", "http://fogid.net/o/person"),
            // степени, звания, награды
            new XElement("inverse", new XAttribute("prop", "http://fogid.net/o/has-title"),
                new XElement("record", new XAttribute("type", "http://fogid.net/o/titled"),
                    new XElement("field", new XAttribute("prop", "http://fogid.net/o/from-date")),
                    new XElement("field", new XAttribute("prop", "http://fogid.net/o/degree")),
                    null)),
            // Участие
            new XElement("inverse", new XAttribute("prop", "http://fogid.net/o/participant"),
                new XElement("record",
                    new XElement("field", new XAttribute("prop", "http://fogid.net/o/from-date")),
                    new XElement("field", new XAttribute("prop", "http://fogid.net/o/to-date")),
                    new XElement("field", new XAttribute("prop", "http://fogid.net/o/role-classification")),
                    new XElement("direct", new XAttribute("prop", "http://fogid.net/o/in-org"),
                        new XElement("record",
                            new XElement("field", new XAttribute("prop", "http://fogid.net/o/from-date")),
                            new XElement("field", new XAttribute("prop", "http://fogid.net/o/to-date")),
                            new XElement("field", new XAttribute("prop", "http://fogid.net/o/org-classification")),
                            new XElement("field", new XAttribute("prop", "http://fogid.net/o/name")))))),
            // Проживание
            new XElement("inverse", new XAttribute("prop", "http://fogid.net/o/something"),
                new XElement("record",
                    new XElement("direct", new XAttribute("prop", "http://fogid.net/o/location-place"),
                        new XElement("record",
                            new XElement("field", new XAttribute("prop", "http://fogid.net/o/name")))))),
            // Авторство документов
            new XElement("inverse", new XAttribute("prop", "http://fogid.net/o/author"),
                new XElement("record",
                    new XElement("field", new XAttribute("prop", "http://fogid.net/o/authority-specificator")),
                    new XElement("direct", new XAttribute("prop", "http://fogid.net/o/adoc"),
                        new XElement("record",
                            new XElement("field", new XAttribute("prop", "http://fogid.net/o/name")),
                            new XElement("field", new XAttribute("prop", "http://fogid.net/o/from-date")),
                            new XElement("field", new XAttribute("prop", "http://fogid.net/o/description")),
                            null)))),
            // Отражение в документах
            new XElement("inverse", new XAttribute("prop", "http://fogid.net/o/reflected"),
                new XElement("record",
                    new XElement("field", new XAttribute("prop", "http://fogid.net/o/ground")),
                    new XElement("direct", new XAttribute("prop", "http://fogid.net/o/in-doc"),
                        new XElement("record",
                            new XElement("field", new XAttribute("prop", "http://fogid.net/o/name")),
                            new XElement("field", new XAttribute("prop", "http://fogid.net/o/from-date")),
                            new XElement("field", new XAttribute("prop", "http://fogid.net/o/description")),
                            null)))),
            new XElement("field", new XAttribute("prop", "http://fogid.net/o/name")),
            new XElement("field", new XAttribute("prop", "http://fogid.net/o/from-date")),
            new XElement("field", new XAttribute("prop", "http://fogid.net/o/to-date")),
            new XElement("field", new XAttribute("prop", "http://fogid.net/o/description")),
            null);


        public PortraitPersonModel(string id)
        {
            this.id = id;

            XElement item = SObjects.GetItemById(id, format);
            if (item == null) { return; }
            // Вытягиваем информацию
            var name_el = item.Elements("field").FirstOrDefault(f => f.Attribute("prop").Value == "http://fogid.net/o/name");
            this.name = name_el == null ? "noname" : name_el.Value;
            // Описание
            var desc_el = item.Elements("field").FirstOrDefault(f => f.Attribute("prop").Value == "http://fogid.net/o/description");
            this.description = desc_el == null ? "" : desc_el.Value;
            // Даты
            string _dates = SObjects.GetDates(item);
            this.dates = string.IsNullOrEmpty(_dates) ? "" : "(" + _dates + ")";
            // Титулы
            titles = item.Elements("inverse")
                .Where(inv => inv.Attribute("prop").Value == "http://fogid.net/o/has-title")
                .Select(inv => inv.Element("record"));
            // Участие и работа
            var participations = item.Elements("inverse")
                .Where(inv => inv.Attribute("prop").Value == "http://fogid.net/o/participant")
                .Select(inv => inv.Element("record"));
            works = participations.Where(re =>
            {
                XElement org = re.Element("direct")?.Element("record");
                if (org == null) return false; 
                string org_classification = SObjects.GetField(org, "http://fogid.net/o/role-classification");
                //return string.IsNullOrEmpty(org_classification) || org_classification == "organization";
                return org_classification == "organization";
            });
            notworks = participations.Where(re =>
            {
                XElement org = re.Element("direct")?.Element("record");
                if (org == null) return false;
                string org_classification = SObjects.GetField(org, "http://fogid.net/o/role-classification");
                //return !string.IsNullOrEmpty(org_classification) && org_classification != "organization";
                return org_classification != "organization";
            });
            // Проживание
            livings = item.Elements("inverse")
                .Where(inv => inv.Attribute("prop").Value == "http://fogid.net/o/something")
                .Select(inv => inv.Element("record")?.Element("direct")?.Element("record"))
                .Where(r => r != null);
            // Авторство
            auth = item.Elements("inverse")
                .Where(inv => inv.Attribute("prop").Value == "http://fogid.net/o/author")
                .Select(inv => inv.Element("record"))
                //.Where(rec => OpenArchive.StaticObjects.GetField(rec, "http://fogid.net/o/authority-specificator") == "author")
                //.Select(rec => rec.Element("direct").Element("record"))
                ;
            // Документы, отражающие 
            refl_docs = item.Elements("inverse")
                .Where(inv => inv.Attribute("prop").Value == "http://fogid.net/o/reflected")
                .Select(inv => inv.Element("record"))
                .Select(rec => rec.Element("direct")?.Element("record"))
                .Where(r => r != null)
                .ToArray()
                ;

        }
    }
    public class PortraitCollectionModel
    {
        private static XElement format = new XElement("record", new XAttribute("type", "http://fogid.net/o/collection"),
            // подколлекции и документы
            new XElement("inverse", new XAttribute("prop", "http://fogid.net/o/in-collection"),
                new XElement("record",
                    new XElement("field", new XAttribute("prop", "http://fogid.net/o/pageNumbers")),
                    new XElement("direct", new XAttribute("prop", "http://fogid.net/o/collection-item"),
                        new XElement("record",
                            new XElement("field", new XAttribute("prop", "http://fogid.net/o/name")),
                            new XElement("field", new XAttribute("prop", "http://fogid.net/o/from-date")),
                            new XElement("field", new XAttribute("prop", "http://fogid.net/o/description")),
                            new XElement("inverse", new XAttribute("prop", "http://fogid.net/o/forDocument"),
                                new XElement("record",
                                    new XElement("field", new XAttribute("prop", "http://fogid.net/o/uri")))),
                            new XElement("field", new XAttribute("prop", "http://fogid.net/o/uri")),
                            // Авторы
                            new XElement("inverse", new XAttribute("prop", "http://fogid.net/o/adoc"),
                                new XElement("record",
                                    new XElement("field", new XAttribute("prop", "http://fogid.net/o/authority-specificator")),
                                    new XElement("direct", new XAttribute("prop", "http://fogid.net/o/author"),
                                        new XElement("record",
                                            new XElement("field", new XAttribute("prop", "http://fogid.net/o/name")),
                                            null)))),
                            null)))),
            new XElement("field", new XAttribute("prop", "http://fogid.net/o/description")),
            new XElement("field", new XAttribute("prop", "http://fogid.net/o/name")));

        public string id, name, description;
        public IEnumerable<XElement> subcollections, documents, multimedia;
        public XElement look = null;
        public PortraitCollectionModel(string id)
        {
            if (id == null) { return; }
            this.id = id;
            XElement item = SObjects.GetItemById(id, format);

            // Вытягиваем информацию
            var name_el = item.Elements("field").FirstOrDefault(f => f.Attribute("prop").Value == "http://fogid.net/o/name");
            name = name_el == null ? "noname" : name_el.Value;
            // Описание коллекции
            var desc_el = item.Elements("field").FirstOrDefault(f => f.Attribute("prop").Value == "http://fogid.net/o/description");
            description = desc_el == null ? "" : desc_el.Value;
            // Нахождение подколлекций и набора документов
            var c_members = item.Elements("inverse")
                .Select(inv => inv.Element("record"))
                .OrderBy(rec =>
                {
                    XElement pages = rec.Elements("field").FirstOrDefault(f => f.Attribute("prop").Value == "http://fogid.net/o/pageNumbers");
                    return pages == null ? "ZZZZZ" : pages.Value;
                })
                .ThenBy(rec =>
                {
                    XElement nm = rec.Element("direct")?.Element("record")?.Elements("field")
                        .FirstOrDefault(f => f.Attribute("prop").Value == "http://fogid.net/o/name");
                    return nm == null ? "" : nm.Value;
                })
                .Select(rec => rec.Element("direct")?.Element("record"));
            subcollections = c_members
                .Where(m => m != null && m.Attribute("type") != null && m.Attribute("type").Value == "http://fogid.net/o/collection");
            documents = c_members
                .Where(m => m != null && m.Attribute("type") != null && m.Attribute("type").Value == "http://fogid.net/o/document");
            multimedia = c_members
                .Where(m => m != null && m.Attribute("type") != null && m.Attribute("type").Value == "http://fogid.net/o/photo-doc");

            //look = item;
        }
    }
    public class PortraitOrgModel
    {
        public string id, name, description, dates;
        public bool isorg;
        public IEnumerable<XElement> participants, locations, auth, reflections;
        public PortraitOrgModel(string id)
        {
            this.id = id;

            XElement item = SObjects.GetItemById(id, format);
            if (item == null) { return; }
            // Вытягиваем информацию
            var name_el = item.Elements("field").FirstOrDefault(f => f.Attribute("prop").Value == "http://fogid.net/o/name");
            name = name_el == null ? "noname" : name_el.Value;
            // Описание
            var desc_el = item.Elements("field").FirstOrDefault(f => f.Attribute("prop").Value == "http://fogid.net/o/description");
            description = desc_el == null ? "" : desc_el.Value;
            // Даты
            string _dates = SObjects.GetDates(item);
            dates = string.IsNullOrEmpty(_dates) ? "" : "(" + _dates + ")";
            // Классификатор
            var oclassification = SObjects.GetField(item, "http://fogid.net/o/rg-classification");
            isorg = oclassification != null && oclassification == "organization";
            // Участие или работа
            participants = item.Elements("inverse")
                .Where(inv => inv.Attribute("prop").Value == "http://fogid.net/o/in-org")
                .Select(inv => inv.Element("record"));
            // Расположение
            locations = item.Elements("inverse")
                .Where(inv => inv.Attribute("prop").Value == "http://fogid.net/o/something")
                .Select(inv => inv.Element("record")?.Element("direct")?.Element("record"))
                .Where(r => r != null)
                .ToArray();
            // Авторы/абоненты  Авторство
            auth = item.Elements("inverse")
                .Where(inv => inv.Attribute("prop").Value == "http://fogid.net/o/author")
                .Select(inv => inv.Element("record")?.Element("direct")?.Element("record"));
            // Отражения
            reflections = item.Elements("inverse")
                .Where(inv => inv.Attribute("prop").Value == "http://fogid.net/o/reflected")
                .Select(inv => inv.Element("record")?.Element("direct")?.Element("record"));

        }
        private static XElement format = new XElement("record", new XAttribute("type", "http://fogid.net/o/org-sys"),
            // Участники
            new XElement("inverse", new XAttribute("prop", "http://fogid.net/o/in-org"),
                new XElement("record",
                    new XElement("field", new XAttribute("prop", "http://fogid.net/o/from-date")),
                    new XElement("field", new XAttribute("prop", "http://fogid.net/o/to-date")),
                    new XElement("field", new XAttribute("prop", "http://fogid.net/o/role-classification")),
                    new XElement("field", new XAttribute("prop", "http://fogid.net/o/role")),
                    new XElement("direct", new XAttribute("prop", "http://fogid.net/o/participant"),
                        new XElement("record",
                            new XElement("field", new XAttribute("prop", "http://fogid.net/o/name")))))),
            // Расположение
            new XElement("inverse", new XAttribute("prop", "http://fogid.net/o/something"),
                new XElement("record",
                    new XElement("direct", new XAttribute("prop", "http://fogid.net/o/location-place"),
                        new XElement("record",
                            new XElement("field", new XAttribute("prop", "http://fogid.net/o/name")))))),
            // Абонент документов
            new XElement("inverse", new XAttribute("prop", "http://fogid.net/o/author"),
                new XElement("record",
                    new XElement("field", new XAttribute("prop", "http://fogid.net/o/authority-specificator")),
                    new XElement("direct", new XAttribute("prop", "http://fogid.net/o/adoc"),
                        new XElement("record",
                            new XElement("field", new XAttribute("prop", "http://fogid.net/o/name")),
                            new XElement("field", new XAttribute("prop", "http://fogid.net/o/from-date")),
                            new XElement("field", new XAttribute("prop", "http://fogid.net/o/description")),
                            null)))),
            // Отражение в документах
            new XElement("inverse", new XAttribute("prop", "http://fogid.net/o/reflected"),
                new XElement("record",
                    new XElement("field", new XAttribute("prop", "http://fogid.net/o/ground")),
                    new XElement("direct", new XAttribute("prop", "http://fogid.net/o/in-doc"),
                        new XElement("record",
                            new XElement("field", new XAttribute("prop", "http://fogid.net/o/name")),
                            new XElement("field", new XAttribute("prop", "http://fogid.net/o/from-date")),
                            new XElement("field", new XAttribute("prop", "http://fogid.net/o/description")),
                            null)))),
            new XElement("field", new XAttribute("prop", "http://fogid.net/o/name")),
            new XElement("field", new XAttribute("prop", "http://fogid.net/o/from-date")),
            new XElement("field", new XAttribute("prop", "http://fogid.net/o/to-date")),
            new XElement("field", new XAttribute("prop", "http://fogid.net/o/description")),
            new XElement("field", new XAttribute("prop", "http://fogid.net/o/org-classification")),
            null);
    }
    public class DocPart 
    { 
        public XElement docInPart; public string pages;
        public static IEnumerable<DocPart> ExtractDocParts(XElement item)
        {
            return item.Elements("inverse")
                .Where(inv => inv.Attribute("prop").Value == "http://fogid.net/o/inDocument")
                .Select(inv => inv.Element("record"))
                .Select(rec => new DocPart()
                {
                    docInPart = rec.Element("direct")?.Element("record"),
                    pages = SObjects.GetField(rec, "http://fogid.net/o/pageNumbers")
                })
                .OrderBy(pair => pair.pages)
                .ThenBy(pair => pair.docInPart == null ? "" : SObjects.GetField(pair.docInPart, "http://fogid.net/o/name"))
                ;
        }

    }
    public class PortraitDocumentModel
    {
        public string id, typeid, name, description, startdate, enddate, doccontent, uri, contenttype;
        public string eid = null; // ид внешнего объекта, который через отношение относится к данному
        public string[] doceidarr = null; // массив ид документов, относящихся через отношение к eid

        public IEnumerable<DocPart> parts;
        public IEnumerable<XElement> reflections, authors, recipients, geoplaces, collections;
        public XElement infosource, descr_infosource;
        public Dictionary<string, string> docContentSources;
        // Временное решение - единственный сетевой источник данных или null 
        public string docSrc = null;
        public PortraitDocumentModel(string id, string eid)
        {
            this.id = id;
            this.eid = eid;

            if (!string.IsNullOrEmpty(eid))
            {
                XElement doctree = OAData.OADB.GetItemById(eid, new XElement("record",
                    new XElement("inverse", new XAttribute("prop", "http://fogid.net/o/inDocument"),
                        new XElement("record",
                            new XElement("field", new XAttribute("prop", "http://fogid.net/o/pageNumbers")),
                            new XElement("direct", new XAttribute("prop", "http://fogid.net/o/partItem"),
                                new XElement("record"))))));

                //doceidarr = doctree.Elements("inverse")
                //        .Select(inv => inv.Element("record"))
                //        .OrderBy(r => r.Element("field")?.Value)
                //        .Select(r => r.Element("direct")?.Element("record")?.Attribute("id")?.Value)
                //        .Where(d => d != null)
                //        .ToArray();
                //var dip = DocPart.ExtractDocParts(doctree).ToArray();
                doceidarr = DocPart.ExtractDocParts(doctree)
                        .Select(dp => dp.docInPart.Attribute("id")?.Value)
                        .Where(d => d != null)
                        .ToArray();
            }


            if (id == null) { return; }
            XElement item = SObjects.GetItemById(id, format);
            typeid = item.Attribute("id").Value;
            if (item == null) { return; }
            // Вытягиваем информацию
            var name_el = item.Elements("field").FirstOrDefault(f => f.Attribute("prop").Value == "http://fogid.net/o/name");
            name = name_el == null ? "noname" : name_el.Value;
            // Описание
            var desc_el = item.Elements("field").FirstOrDefault(f => f.Attribute("prop").Value == "http://fogid.net/o/description");
            description = desc_el == null ? "" : desc_el.Value;
            // Даты
            startdate = SObjects.GetField(item, "http://fogid.net/o/from-date");
            enddate = SObjects.GetField(item, "http://fogid.net/o/to-date");
            doccontent = SObjects.GetField(item, "http://fogid.net/o/doc-content");
            uri = SObjects.GetField(item, "http://fogid.net/o/uri");
            var docmetainfo = SObjects.GetField(item, "http://fogid.net/o/docmetainfo");
            contenttype = docmetainfo == null ? "" : (docmetainfo.Split(';')
                .Where(pair => pair.StartsWith("documenttype:"))
                .Select(pair => pair.Substring("documenttype:".Length)).FirstOrDefault());
            // Части документа
            parts = DocPart.ExtractDocParts(item);
            //.Select(pair => pair.docpart);
            // Отражения
            reflections = item.Elements("inverse")
                .Where(inv => inv.Attribute("prop").Value == "http://fogid.net/o/in-doc")
                .Select(inv => inv.Element("record")?.Element("direct")?.Element("record"));
            // Авторы и получатели
            var authorities = item.Elements("inverse")
                .Where(inv => inv.Attribute("prop").Value == "http://fogid.net/o/adoc")
                .Select(inv => inv.Element("record"));
            // Дополнения от Фурсенко
            authors = authorities
                .Where(rec => ((SObjects.GetField(rec, "http://fogid.net/o/authority-specificator") == "author")
                || (SObjects.GetField(rec, "http://fogid.net/o/authority-specificator") == "out-org")))
                .Select(rec => rec.Element("direct")?.Element("record"));
            recipients = authorities
                .Where(rec => ((SObjects.GetField(rec, "http://fogid.net/o/authority-specificator") == "recipient")
                || (SObjects.GetField(rec, "http://fogid.net/o/authority-specificator") == "in-org")))
                .Select(rec => rec.Element("direct")?.Element("record"));
            // Геоинформация    
            geoplaces = item.Elements("inverse")
                .Where(inv => inv.Attribute("prop").Value == "http://fogid.net/o/something")
                .Select(inv => inv.Element("record")?.Element("direct")?.Element("record"));
            // Источник поступления
            var fromarchivemember = item.Elements("inverse")
                .FirstOrDefault(inv => inv.Attribute("prop").Value == "http://fogid.net/o/archive-item");
            XElement toinfosource = fromarchivemember?.Element("record")?.Elements("direct")
                .FirstOrDefault(di => di.Attribute("prop")?.Value == "http://fogid.net/o/info-source");
            infosource = toinfosource?.Element("record");
            // Дополнительная информация про источник поступления
            descr_infosource = fromarchivemember?.Element("record")?.Elements("field")
                .FirstOrDefault(di => di.Attribute("prop").Value == "http://fogid.net/o/description");

            // Элемент коллекций    
            collections = item.Elements("inverse")
                .Where(inv => inv.Attribute("prop").Value == "http://fogid.net/o/collection-item")
                .Select(inv => inv.Element("record")?.Element("direct")?.Element("record"))
                .Where(r => r != null);

            // Это пока не нужно
            docContentSources = new Dictionary<string, string>();
            // Будем использовать это
            //docSrc = StaticObjects.Engine.DaraSrc;

        }


        private static XElement format = new XElement("record", new XAttribute("type", "http://fogid.net/o/document"),
            // Части документа
            new XElement("inverse", new XAttribute("prop", "http://fogid.net/o/inDocument"),
                new XElement("record",
                    new XElement("field", new XAttribute("prop", "http://fogid.net/o/pageNumbers")),
                    new XElement("direct", new XAttribute("prop", "http://fogid.net/o/partItem"),
                        new XElement("record", new XAttribute("type", "http://fogid.net/o/photo-doc"),
                            new XElement("field", new XAttribute("prop", "http://fogid.net/o/name")),
                            new XElement("field", new XAttribute("prop", "http://fogid.net/o/uri")),
                            new XElement("field", new XAttribute("prop", "http://fogid.net/o/docmetainfo"))
                            )),
                    null)),
            // Отражения
            new XElement("inverse", new XAttribute("prop", "http://fogid.net/o/in-doc"),
                new XElement("record",
                    new XElement("direct", new XAttribute("prop", "http://fogid.net/o/reflected"),
                        new XElement("record",
                            new XElement("field", new XAttribute("prop", "http://fogid.net/o/name")))))),
            // Авторство документов
            new XElement("inverse", new XAttribute("prop", "http://fogid.net/o/adoc"),
                new XElement("record",
                    new XElement("field", new XAttribute("prop", "http://fogid.net/o/authority-specificator")),
                    new XElement("direct", new XAttribute("prop", "http://fogid.net/o/author"),
                        new XElement("record",
                            new XElement("field", new XAttribute("prop", "http://fogid.net/o/name")))))),
            // Геоинформация
            new XElement("inverse", new XAttribute("prop", "http://fogid.net/o/something"),
                new XElement("record",
                    new XElement("direct", new XAttribute("prop", "http://fogid.net/o/location-place"),
                        new XElement("record",
                            new XElement("field", new XAttribute("prop", "http://fogid.net/o/name")))))),
            // Архив, источник
            new XElement("inverse", new XAttribute("prop", "http://fogid.net/o/archive-item"),
                new XElement("record", new XAttribute("type", "http://fogid.net/o/archive-member"),
                    new XElement("direct", new XAttribute("prop", "http://fogid.net/o/in-archive"),
                        new XElement("record",
                            new XElement("field", new XAttribute("prop", "http://fogid.net/o/name")))),
                    new XElement("direct", new XAttribute("prop", "http://fogid.net/o/info-source"),
                        new XElement("record",
                            new XElement("field", new XAttribute("prop", "http://fogid.net/o/name")))),
                    new XElement("field", new XAttribute("prop", "http://fogid.net/o/description")),
                    null)),
            // В коллекциях
            new XElement("inverse", new XAttribute("prop", "http://fogid.net/o/collection-item"),
                new XElement("record",
                    new XElement("direct", new XAttribute("prop", "http://fogid.net/o/in-collection"),
                        new XElement("record",
                            new XElement("field", new XAttribute("prop", "http://fogid.net/o/name")))))),
            new XElement("field", new XAttribute("prop", "http://fogid.net/o/name")),
            new XElement("field", new XAttribute("prop", "http://fogid.net/o/from-date")),
            new XElement("field", new XAttribute("prop", "http://fogid.net/o/to-date")),
            new XElement("field", new XAttribute("prop", "http://fogid.net/o/doc-content")),
            new XElement("field", new XAttribute("prop", "http://fogid.net/o/description")),
            new XElement("field", new XAttribute("prop", "http://fogid.net/o/uri")),
            new XElement("field", new XAttribute("prop", "http://fogid.net/o/docmetainfo")),
            null);
    }
    public class PortraitGeoModel
    {
        public string name, description, dates;
        public IEnumerable<XElement> reflections, locations, locations_person, locations_org, locations_doc;
        public PortraitGeoModel(string id)
        {
            XElement item = SObjects.GetItemById(id, format);
            if (item == null) { return; }
            // Вытягиваем информацию
            var name_el = item.Elements("field").FirstOrDefault(f => f.Attribute("prop").Value == "http://fogid.net/o/name");
            name = name_el == null ? "noname" : name_el.Value;
            // Описание
            var desc_el = item.Elements("field").FirstOrDefault(f => f.Attribute("prop").Value == "http://fogid.net/o/description");
            description = desc_el == null ? "" : desc_el.Value;
            // Даты
            string _dates = SObjects.GetDates(item);
            dates = string.IsNullOrEmpty(_dates) ? "" : "(" + _dates + ")";
            // Отражения
            reflections = item.Elements("inverse")
                .Where(inv => inv.Attribute("prop").Value == "http://fogid.net/o/reflected")
                .Select(inv => inv.Element("record").Element("direct").Element("record"));
            // Расположение
            locations = item.Elements("inverse")
                .Where(inv => inv.Attribute("prop").Value == "http://fogid.net/o/location-place")
                .Select(inv => inv.Element("record")?.Element("direct")?.Element("record")).Where(r => r != null).ToArray();
            locations_person = locations
                .Where(rec => rec.Attribute("type").Value == "http://fogid.net/o/person").ToArray();
            locations_org = locations
                .Where(rec => rec.Attribute("type").Value == "http://fogid.net/o/org-sys").ToArray();
            locations_doc = locations
                .Where(rec => rec.Attribute("type").Value == "http://fogid.net/o/document").ToArray();

        }
        private static XElement format = new XElement("record",
            // Отражение в документе
            new XElement("inverse", new XAttribute("prop", "http://fogid.net/o/reflected"),
                new XElement("record",
                    new XElement("direct", new XAttribute("prop", "http://fogid.net/o/in-doc"),
                        new XElement("record", new XAttribute("type", "http://fogid.net/o/document"),
                            new XElement("field", new XAttribute("prop", "http://fogid.net/o/name")),
                            new XElement("field", new XAttribute("prop", "http://fogid.net/o/from-date")),
                            new XElement("field", new XAttribute("prop", "http://fogid.net/o/description")),
                            null)))),
            // Расположение сущностей разного типа
            new XElement("inverse", new XAttribute("prop", "http://fogid.net/o/location-place"),
                new XElement("record",
                    new XElement("direct", new XAttribute("prop", "http://fogid.net/o/something"),
                        new XElement("record",
                            new XElement("field", new XAttribute("prop", "http://fogid.net/o/name")),
                            new XElement("field", new XAttribute("prop", "http://fogid.net/o/from-date")),
                            new XElement("field", new XAttribute("prop", "http://fogid.net/o/to-date")),
                            new XElement("field", new XAttribute("prop", "http://fogid.net/o/description")),
                            null)))),
            new XElement("field", new XAttribute("prop", "http://fogid.net/o/name")),
            new XElement("field", new XAttribute("prop", "http://fogid.net/o/from-date")),
            new XElement("field", new XAttribute("prop", "http://fogid.net/o/to-date")),
            new XElement("field", new XAttribute("prop", "http://fogid.net/o/description")),
            null);

    }
    public class DocumentImageModel
    {
        public string id, name, uri;
        public XElement item;
        public XElement[] parts;
        public int part_ind;
        public DocumentImageModel(string id, string eid)
        {
            this.id = id;
            item = SObjects.GetItemById(id, format);
            if (item == null) { return; }
            // Вытягиваем информацию
            var name_el = item.Elements("field").FirstOrDefault(f => f.Attribute("prop").Value == "http://fogid.net/o/name");
            name = name_el == null ? "noname" : name_el.Value;
            // Части документа
            parts = item.Elements("inverse")
                .Where(inv => inv.Attribute("prop").Value == "http://fogid.net/o/inDocument")
                .Select(inv => inv.Element("record"))
                .Select(rec => new
                {
                    docpart = rec.Element("direct").Element("record"),
                    page = SObjects.GetField(rec, "http://fogid.net/o/pageNumbers")
                })
                .OrderBy(pair => pair.page)
                .ThenBy(pair => SObjects.GetField(pair.docpart, "http://fogid.net/o/name"))
                .Select(pair => pair.docpart)
                .ToArray();
            var part = parts
                .Select((docpart, i) => new { dp = docpart, ind = i })
                .FirstOrDefault(pair => pair.dp.Attribute("id").Value == eid);
            if (part == null) { return; }
            part_ind = part.ind;
            var doc = parts[part_ind];
            //var fromuri = doc.Elements("inverse")
            //    .FirstOrDefault(inv => inv.Attribute("prop").Value == "http://fogid.net/o/forDocument");
            //if (fromuri == null) { return; }
            //uri = StaticObjects.GetField(fromuri.Element("record"), "http://fogid.net/o/uri");
            uri = SObjects.GetField(doc, "http://fogid.net/o/uri");
        }

        private static XElement format = new XElement("record", new XAttribute("type", "http://fogid.net/o/document"),
        // Части документа
        new XElement("inverse", new XAttribute("prop", "http://fogid.net/o/inDocument"),
            new XElement("record",
                new XElement("field", new XAttribute("prop", "http://fogid.net/o/pageNumbers")),
                new XElement("direct", new XAttribute("prop", "http://fogid.net/o/partItem"),
                    new XElement("record", new XAttribute("type", "http://fogid.net/o/photo-doc"),
                        new XElement("field", new XAttribute("prop", "http://fogid.net/o/name")),
                        new XElement("field", new XAttribute("prop", "http://fogid.net/o/uri")),
                        new XElement("inverse", new XAttribute("prop", "http://fogid.net/o/forDocument"),
                            new XElement("record",
                                new XElement("field", new XAttribute("prop", "http://fogid.net/o/uri")))))),
                null)),
        new XElement("field", new XAttribute("prop", "http://fogid.net/o/name")),
        null);
    }
}
