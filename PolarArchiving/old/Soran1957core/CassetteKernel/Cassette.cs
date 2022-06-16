using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
//using System.Windows.Media.Imaging;
using System.Xml.Linq;
using SGraph;


namespace factograph
{
    public class Cassette
    {
        public string _folderNumber;
        public string _documentNumber;
        public string dbId { get { return db==null?null : (db.Attribute("dbid") != null ? db.Attribute("dbid").Value : null); } }
        public string owner { get { return db==null?null : db.Attribute("owner").Value; } }
        public XElement db;
        private string wastebasketcollection = "wastebasketcollection";
        public string Wastebasket { get { return wastebasketcollection; } }

        public bool NeedToCalculate = false;
        static public char[] slashes = new[] { '/', '\\' };
        // Параметры (конфигуратор) кассеты
        public XElement finfo = null;
        public static XElement finfo_default = new XElement(XName.Get("finfo"),
            new XElement("image",
                new XElement("small", new XAttribute("previewBase", "120"), new XAttribute("qualityLevel", "90")),
                new XElement("medium", new XAttribute("previewBase", "480"), new XAttribute("qualityLevel", "90")),
                new XElement("normal", new XAttribute("previewBase", "800"), new XAttribute("qualityLevel", "90"))),
            new XElement("video",
                new XElement("medium",
                    new XAttribute("videoBitrate", "400K"),
                    new XAttribute("audioBitrate", "22050"),
                    new XAttribute("rate", "10"),
                    new XAttribute("framesize", "384x288")))
            );

        public Cassette(DirectoryInfo dir, string cassName, string fn, string dn, XElement db)
        {
            Changed = false;
            this.Dir = dir;
            this._folderNumber = fn;
            this._documentNumber = dn;
            this.Name = cassName;
            LoadFinfo();
            this.db = db;
            ActivateIdGenerator();
        }
        public Cassette(string cassPath, bool toload)
        {
            this.Dir = new DirectoryInfo(cassPath);
            this.Name = cassPath.Split(slashes, StringSplitOptions.RemoveEmptyEntries).Last();
            LoadFinfo();
            Changed = false;
            if (toload)
            {
                XElement xconfig = XElement.Load(cassPath + "/meta/" + Name + "_config.fog");
                this.db = XElement.Load(cassPath + "/meta/" + Name + "_current.fog");
                this._folderNumber = xconfig.Element(ONames.TagSourcemeta).Element(ONames.TagFoldername).Value;
                this._documentNumber = xconfig.Element(ONames.TagSourcemeta).Element(ONames.TagDocumentNumber).Value;
                ActivateIdGenerator();
            }
        }
        public Cassette(string cassPath)
        {
            bool toload = true;
            this.Dir = new DirectoryInfo(cassPath);
            this.Name = cassPath.Split(slashes, StringSplitOptions.RemoveEmptyEntries).Last();
            LoadFinfo();
            Changed = false;
            if (toload)
            {
                XElement xconfig = XElement.Load(cassPath + "/meta/" + Name + "_config.fog");
                this.db = XElement.Load(cassPath + "/meta/" + Name + "_current.fog");
                this._folderNumber = xconfig.Element(ONames.TagSourcemeta).Element(ONames.TagFoldername).Value;
                this._documentNumber = xconfig.Element(ONames.TagSourcemeta).Element(ONames.TagDocumentNumber).Value;
                ActivateIdGenerator();
            }
        }

        private void LoadFinfo()
        {
            if (File.Exists(Dir.FullName + "/cassette.finfo"))
            {
                finfo = XElement.Load(Dir.FullName + "/cassette.finfo");
                if (finfo.Elements().Count() == 0) // TODO: временное решение! 
                {
                    finfo_default.Save(Dir.FullName + "/cassette.finfo");
                    finfo = finfo_default;
                }
            }
        }
        /// <summary>
        /// Создатель новой кассеты
        /// </summary>
        /// <param name="cassPath"></param>
        /// <returns></returns>
        static public Cassette Create(string cassPath)
        {
            string cass_name = cassPath.Split(Cassette.slashes).Last();
            if (Directory.Exists(cassPath) && File.Exists(cassPath + "/cassette.finfo"))
                return new Cassette(cassPath);
            else
            {
                DirectoryInfo dir = Directory.Exists(cassPath) ? new DirectoryInfo(cassPath) : Directory.CreateDirectory(cassPath);
                // Специальная отметка для кассеты
                Cassette.finfo_default.Save(dir.FullName + "/cassette.finfo");

                DirectoryInfo meta = Directory.CreateDirectory(cassPath + "/meta");
                DirectoryInfo originals = Directory.CreateDirectory(cassPath + "/originals");
                DirectoryInfo preview = Directory.CreateDirectory(cassPath + "/documents");
                Directory.CreateDirectory(originals.FullName + "/0001");
                DirectoryInfo small = Directory.CreateDirectory(preview.FullName + "/small/0001");
                DirectoryInfo medium = Directory.CreateDirectory(preview.FullName + "/medium/0001");
                DirectoryInfo normal = Directory.CreateDirectory(preview.FullName + "/normal/0001");
                XElement xconfig =
                    Cassette.RDFdb("configdb_" + cass_name + "_1",
                        new XElement(ONames.TagSourcemeta,
                            new XAttribute(SNames.rdfabout, "sourcemeta_" + cass_name + "_1"),
                            new XElement(ONames.TagFoldername, "0001"),
                            new XElement(ONames.TagDocumentNumber, "0001")));
                xconfig.Save(meta.FullName + "/" + cass_name + "_config.fog");
                string owner = "mag_9347";
                string uri = "iiss://" + cass_name + "@iis.nsk.su/meta";
                XElement xmetadb =
                    Cassette.RDFdb(cass_name + "_current",
                        new XAttribute(ONames.AttUri, uri),
                        new XAttribute(ONames.AttOwner, owner),
                        new XAttribute(ONames.AttPrefix, cass_name + "_"),
                        new XAttribute(ONames.AttCounter, "1001"),
                        new XElement(ONames.TagCassette,
                            new XAttribute(SNames.rdfabout, cass_name + "_cassetteId"),
                            new XElement(ONames.TagName, cass_name),
                            new XElement(ONames.TagCassetteUri, uri)),
                        new XElement(ONames.TagCollectionmember,
                            new XAttribute(SNames.rdfabout, cass_name + "_cassetteId_cmId"),
                            new XElement(ONames.TagIncollection,
                                new XAttribute(SNames.rdfresource, "cassetterootcollection")),
                            new XElement(ONames.TagCollectionitem,
                                new XAttribute(SNames.rdfresource, cass_name + "_cassetteId"))));
                xmetadb.Save(meta.FullName + "/" + cass_name + "_current.fog");
                return new Cassette(dir, cass_name, "0001", "0001", xmetadb);
            }
        }
        // Генератор идентификаторов
        private string prefix = "";
        private int counter = 1001;
        private XAttribute counta;
        private void ActivateIdGenerator()
        {
            XAttribute pref = db.Attribute(ONames.AttPrefix);
            if (pref == null)
            {
                pref = new XAttribute(ONames.AttPrefix, db.Attribute(ONames.AttUri).Value + "/");
                db.Add(pref);
            }
            this.prefix = pref.Value;
            this.counta = db.Attribute(ONames.AttCounter);
            if (counta == null)
            {
                counter = 1001;
                counta = new XAttribute(ONames.AttCounter, "" + counter);
                db.Add(counta);
            }
            else
            {
                if (!Int32.TryParse(counta.Value, out counter))
                {
                    counter = 1001;
                    counta = new XAttribute(ONames.AttCounter, "" + counter);
                    db.Add(counta);
                }
            }
        }
        public string GenerateNewId()
        {
            string nId = prefix + counter;
            counter++;
            counta.SetValue("" + counter);
            return nId;
        }
        public static CassetteInfo LoadCassette(string cassetteFolder) { return LoadCassette(cassetteFolder, true); }
        public static CassetteInfo LoadCassette(string cassetteFolder, bool loaddata)
        {
            //var cassetteFolder = cassettePath.Value;
            if (!Directory.Exists(cassetteFolder) || (!File.Exists(cassetteFolder + "cassette.finfo") && false)) return null;
            var cassette = new Cassette(cassetteFolder, loaddata);
            var cassetteInfo =
                new CassetteInfo
                {
                    fullName = "iiss://" + cassette.Name + "@iis.nsk.su",
                    cassette = cassette,
                    url = cassette.Dir.FullName + '/',
                    docsInfo = !loaddata ? new List<factograph.RDFDocumentInfo>() : 
                        cassette
                            .DataDocuments()
                            .Select(xDoc => new RDFDocumentInfo(xDoc, cassetteFolder))
                            .ToList()
                };
            if (loaddata) cassetteInfo.docsInfo.Add(new RDFDocumentInfo(cassette));
            return cassetteInfo;
        }
        
        public string Name { get; private set; }
        public DirectoryInfo Dir { get; private set; }
        public string CollectionId { get { return Name + "_cassetteId"; } }
        public bool Changed { get;  set; }
        /// <summary>
        /// Возвращает Path оригинала документа относительно базы (корневой директории кассеты)
        /// </summary>
        /// <returns></returns>
        public string GetOriginalDocumentPath(string uri)
        {
            if (uri.EndsWith("/meta"))
            {
                return "meta/" + Name + "_current.fog";
            }
            var iisstore = db.Elements().Elements(ONames.TagIisstore).Where(x=>x.Attribute(ONames.AttUri).Value==uri).FirstOrDefault();
            if (iisstore == null) return null;
            string oname = iisstore.Attribute(ONames.AttOriginalname).Value;
            int lastpoint = oname.LastIndexOf('.');
            return "originals/" + uri.Substring(uri.Length - 9, 9) + oname.Substring(lastpoint);
        }
        public string GetPreviewPhotodocumentPath(string uri, string regime)
        {
            return "documents/" + regime + "/" + uri.Substring(uri.Length - 9, 9) + ".jpg";
        }
        public IEnumerable<XElement> DataDocuments()
        {
            return db.Elements("document")
                    .Where(doc => doc.Element("iisstore").Attribute("documenttype").Value == "application/fog");
                    //.Select(docElem => docElem.Element("iisstore").Attribute("uri").Value);
        }
       public static XElement RDFdb(string dbid, params object[] content)
        {
            return new XElement(SNames.rdfRDF,
                new XAttribute(SGraph.SNames.AttRdf, SNames.rdfnsstring),
                new XAttribute(ONames.AttDbid, dbid), content);
        }
        public void Save()
        {
            db.Save(Dir.FullName + "/meta/" + Name + "_current.fog");
            XElement xconfig = Cassette.RDFdb(Name + "_cassette_1",
                new XElement(ONames.TagSourcemeta,
                    new XAttribute(SNames.rdfabout, Name + "config_1"),
                    new XElement(ONames.TagFoldername, _folderNumber),
                    new XElement(ONames.TagDocumentNumber, _documentNumber)));
            xconfig.Save(Dir.FullName + "/meta/" + Name + "_config.fog");
        }
        public XElement GetXItemById(string id)
        {
            return db.Elements().FirstOrDefault((XElement e) =>
                        {
                            var about = e.Attribute(SNames.rdfabout);
                            return (about != null && about.Value == id);
                        });
        }
        public IEnumerable<XElement> GetInverseXItems(string id)
        {
            return db.Elements()
                .Where(el =>
                    el.Elements().Any(sub_el => 
                    {
                        var r = sub_el.Attribute(SGraph.SNames.rdfresource);
                        return r != null && r.Value == id;
                    })
                );
        }
        public IEnumerable<XElement> GetInverseXItems(string id, XName inverseProp)
        {
            return db.Elements()
                .Where(el =>
                    el.Elements(inverseProp).Any(sub_el =>
                    {
                        var r = sub_el.Attribute(SGraph.SNames.rdfresource);
                        return r != null && r.Value == id;
                    })
                );
        }
        public XElement GetRelation(string firstItem, XName firstPredicate, XName relationType, XName secondPredicate, string secondItem)
        {
            return db.Elements(relationType)
                .FirstOrDefault(el =>
                {
                    var v1 = el.Element(firstPredicate);
                    if (v1 == null || v1.Attribute(SGraph.SNames.rdfresource).Value != firstItem) return false;
                    var v2 = el.Element(secondPredicate);
                    if (v2 == null || v2.Attribute(SGraph.SNames.rdfresource).Value != secondItem) return false;
                    return true;
                });
        }
        public XElement GetXItemByUri(string uri)
        {
            return db.Elements().FirstOrDefault((XElement e) =>
            {
                var iisstore = e.Element(ONames.TagIisstore);
                return (iisstore != null && iisstore.Attribute(ONames.AttUri).Value == uri);
            });
        }
        public void AddToDb(XElement element)
        {
            this.db.Add(element);
        }
        public void IncrementDocumentNumber()
        {
            this.Changed = true;
            int dn = System.Int32.Parse(this._documentNumber) + 1;
            if (dn == 1000)
            {
                this._documentNumber = "0001";
                int fn = System.Int32.Parse(this._folderNumber) + 1;
                this._folderNumber = fn.ToString("D4");
                Directory.CreateDirectory(Dir.FullName + "/originals/" + _folderNumber);
                Directory.CreateDirectory(Dir.FullName + "/documents/small/" + _folderNumber);
                Directory.CreateDirectory(Dir.FullName + "/documents/medium/" + _folderNumber);
                Directory.CreateDirectory(Dir.FullName + "/documents/normal/" + _folderNumber);
            }
            else
                this._documentNumber = dn.ToString("D4");
        }
        static public DocType[] docTypes = new[] {
            new DocType(".png", "image/png", ONames.TagPhotodoc),
            new DocType(".tif", "image/tiff", ONames.TagPhotodoc),
            new DocType(".jpg", "image/jpeg", ONames.TagPhotodoc),
            new DocType(".gif", "image/gif", ONames.TagPhotodoc),
            new DocType(".mp3", "audio/mpeg", ONames.TagAudio),
            new DocType(".wav", "audio/x-wav", ONames.TagAudio),
            new DocType(".mpg", "video/mpeg", ONames.TagVideo),
            new DocType(".mpeg", "video/mpeg", ONames.TagVideo),
            new DocType(".avi", "video/x-msvideo", ONames.TagVideo),
            new DocType(".swf", "video/swf", ONames.TagVideo),
            new DocType(".flv", "video/flv", ONames.TagVideo),
            new DocType(".wmv", "video/wmv", ONames.TagVideo),
            new DocType(".mp4", "video/mp4", ONames.TagVideo),
            new DocType(".mov", "video/mov", ONames.TagVideo),
            new DocType(".txt", "application/text", ONames.TagDocument),
            new DocType(".doc", "application/msword", ONames.TagDocument),
            new DocType(".rtf", "application/rtf", ONames.TagDocument),
            new DocType(".pdf", "application/pdf", ONames.TagDocument),
            new DocType(".xls", "application/excel", ONames.TagDocument),
            new DocType(".xml", "application/xml", ONames.TagDocument),
            new DocType(".fog", "application/fog", ONames.TagDocument),
            new DocType(".htm", "application/html", ONames.TagDocument),
            new DocType(".html", "application/html", ONames.TagDocument),
            new DocType("", "", ONames.TagDocument),
            new DocType("unknown", "type/unknown", ONames.TagDocument)
        };
        // Кодирование строк, содержащих русские буквы и пробелы. TODO: НАдо бы найти более удачное решение
        static string[] rus_chars = new string[] {
            "a", "b", "v", "g", "d", "e", "g", "z", "i", "i", "k", "l", "m", "n", "o", "p", "r", "s", "t", "u", "f", "h", "c", "ch", "sh", "sch", "x", "y", "x", "e", "yu", "ya", "e"};
        static string MyCoding(string str)
        {
            // не преобразовывать если выполнено некоторое условие
            if (str.ToCharArray().All(c => c != ' ' && c >= '!' && c <= '~')) return str;
            var s = str.ToLower().ToCharArray()
                .Select(c =>
                {
                    string n;
                    if (c == ' ') n = "_";
                    else if (c >= 'а' && c <= 'ё') n = rus_chars[c - 'а'];
                    else n = c.ToString();
                    return n;
                })
                .Aggregate((s1,s2)=> s1 + s2);
            return s;
        }
        /// <summary>
        /// Добавляет новую подколлекцию с заданным именем в указанную коллекцию, возвращает добавленные в базу данных элементы.
        /// Если в коллекции уже есть подколлекция с указанным именем (зафиксированным в поле name), но другая подколлекция не создается
        /// Также, через out-выход возвращает идентификатор подколлекции
        /// </summary>
        /// <param name="collectionId"></param>
        /// <returns></returns>
        public IEnumerable<XElement> AddCollection(string subCollectionName, string collectionId, out string subCollectionId)
        {
            List<XElement> addedElements = new List<XElement>();
            // Поищу существующую подколлекцию с указанным именем. Если подколлекция сущестует, то надо только вывести ее идентификатор 
            var sameCollection = db
                .Elements(ONames.TagCollectionmember)
                .Where(cm => cm.Element(ONames.TagIncollection).Attribute(SNames.rdfresource).Value == collectionId)
                .Select(cm => GetXItemById(cm.Element(ONames.TagCollectionitem).Attribute(SNames.rdfresource).Value))
                .FirstOrDefault(ci => ci.Element(ONames.TagName).Value == subCollectionName);
            if (sameCollection != null)
            {
                subCollectionId = sameCollection.Attribute(SNames.rdfabout).Value;
                return addedElements;
            }

            var coding = new System.Text.ASCIIEncoding();
            var newcoll_id = "collection_" + Name + "|" + MyCoding(subCollectionName); 
            // Поищу, нет ли такой коллекции в базе данных, а то может получиться совпадение идентификаторов
            // если все же совпадение произошло, то найду уникальный идентификатор. - ОЧЕНЬ НЕ ИЗЯЩНОЕ РЕШЕНИЕ
            int count = 0;
            while (
                db.Elements(ONames.TagCollection)
                    .FirstOrDefault((XElement e) =>
                    {
                        var xabout = e.Attribute(SGraph.SNames.rdfabout);
                        return (xabout != null && xabout.Value == newcoll_id);
                    }) != null)
            {
                int pos = newcoll_id.LastIndexOf('|');
                newcoll_id = newcoll_id.Substring(0, pos) + count;
                count++;
            }
            // Создам коллекцию
            XElement coll =
                new XElement(ONames.TagCollection,
                    new XAttribute(SGraph.SNames.rdfabout, newcoll_id),
                    new XElement(ONames.TagName, subCollectionName));
            db.Add(coll);
            addedElements.Add(coll);
            // Присоединим коллекцию к вышестоящей
            XElement collmem =
                new XElement(ONames.TagCollectionmember,
                    new XAttribute(SGraph.SNames.rdfabout, "cm_" + newcoll_id),
                    new XElement(ONames.TagCollectionitem,
                        new XAttribute(SGraph.SNames.rdfresource, newcoll_id)),
                    new XElement(ONames.TagIncollection,
                        new XAttribute(SGraph.SNames.rdfresource, collectionId)));
            db.Add(collmem);
            addedElements.Add(collmem);
            subCollectionId = newcoll_id;
            return addedElements;
        }
        /// <summary>
        /// Предоставляет параметр (атрибут) просмотровых решений с указанным именем для указанного размера.
        /// Параметр берется или из iisstore или, если там нет, из дефолтного набора 
        /// </summary>
        /// <param name="iisstore"></param>
        /// <param name="previewSize"></param>
        /// <param name="previewAttribute"></param>
        /// <returns></returns>
        public string GetPreviewParameter(XElement iisstore, string previewSize, string previewAttribute)
        {
            string dt = iisstore.Attribute(ONames.AttDocumenttype).Value;
            int p = dt.IndexOf('/');
            string dtype = dt.Substring(0, p);
            XAttribute att = iisstore.Attribute(dtype + "." + previewSize + "." + previewAttribute);
            if (att == null)
            {
                att = finfo.Element(dtype).Element(previewSize).Attribute(previewAttribute);
            }
            return att.Value;
        }
        /// <summary>
        /// Возвращает расширение файла-оригинала по iisstore. Результат с точкой и в нижнем регистре 
        /// </summary>
        /// <param name="iisstore"></param>
        /// <returns></returns>
        public static string GetOriginalExtensionFromIisstore(XElement iisstore)
        {
            XAttribute originalname;
            if (iisstore == null || (originalname=iisstore.Attribute(ONames.AttOriginalname)) == null) return "";
            string ov = originalname.Value;
            return ov.Substring(ov.LastIndexOf('.')).ToLower();
        }
        //public void MakePhotoPreview(int small, int medium, int normal)
        //{
        //    foreach (XElement photoDoc in db.Elements(ONames.TagPhotodoc))
        //    {
        //        var uri = photoDoc.Element(ONames.TagIisstore).Attribute(ONames.AttUri).Value;
        //        //printfn "%s" uri
        //        var parts = uri.Split(new char[] { '/' });
        //        var dn = parts[parts.Length - 1];
        //        var fn = parts[parts.Length - 2];
        //        var file = Dir.GetFiles("originals/" + fn + "/" + dn + ".*")[0];
        //        var fname = file.FullName;
        //        Uri _source = new Uri(fname);
        //        BitmapImage bi = new BitmapImage(_source);
        //        double width = (double)bi.PixelWidth;
        //        double height = (double)bi.PixelHeight;
        //        double dim = (width > height ? width : height);
        //        double sm = (double)small, md = (double)medium, nm = (double)normal;
        //        ImageLab.TransformSave(bi, sm / dim, 90, Dir.FullName + "/documents/small/" + fn + "/" + dn + ".jpg");
        //        ImageLab.TransformSave(bi, md / dim, 90, Dir.FullName + "/documents/medium/" + fn + "/" + dn + ".jpg");
        //        ImageLab.TransformSave(bi, nm / dim, 90, Dir.FullName + "/documents/normal/" + fn + "/" + dn + ".jpg");
        //    }
        //}
        public IEnumerable<XElement> GetSubCollections(string collectionId)
        {
            return db.Elements(ONames.TagCollectionmember)
                .Where((XElement e) =>
                    e.Element(ONames.TagIncollection).Attribute(SNames.rdfresource).Value == collectionId)
                .Select((XElement e) => this.GetXItemById(e.Element(ONames.TagCollectionitem).Attribute(SNames.rdfresource).Value))
                .Where((XElement el) => el.Name == ONames.TagCollection);
        }
        public IEnumerable<XElement> GetSubItems(string collectionId)
        {
            return db.Elements(ONames.TagCollectionmember)
                .Where((XElement e) =>
                    e.Element(ONames.TagIncollection).Attribute(SNames.rdfresource).Value == collectionId)
                .Select((XElement e) => this.GetXItemById(e.Element(ONames.TagCollectionitem).Attribute(SNames.rdfresource).Value))
                //.Where((XElement el) => el.Name == ONames.TagCollection)
                ;
        }
        /// <summary>
        /// Убирает запись в мусорную корзину
        /// </summary>
        /// <param name="itemId">string</param>
        public void RemoveItemToWastebasket(string itemId)
        {
            foreach (var xitem in db.Elements(ONames.TagCollectionmember).Where(el => el.Element(ONames.TagCollectionitem).Attribute(SGraph.SNames.rdfresource).Value == itemId))
            {
                xitem.Element(ONames.TagIncollection).Attribute(SGraph.SNames.rdfresource).SetValue(wastebasketcollection);
            }
        }
        public void RecalculateDocsType() { } // Это заглушка, реальный метод надо взять из дома
    }
    public struct DocType {
        public string ext, content_type; public XName tag;
        public DocType(string ext, string content_type, XName tag)
        {
            this.ext = ext; this.content_type = content_type; this.tag = tag;
        }
    }
    public class RDFDocumentInfo
    {
        /// <summary>
        /// Контструктор для документа в папке оригиналов  по записи в данных
        /// </summary>
        /// <param name="docNode">запись о документе, должна содержать iisstore со всеми атрибутами</param>
        ///<param name="cassetteFolder">путь к кассете</param>
        public RDFDocumentInfo(XElement docNode, string cassetteFolder)
        {
            dbId = docNode.Attribute(SNames.rdfabout).Value;
            this.uri = docNode.Element("iisstore").Attribute("uri").Value;
            owner = docNode.Element("iisstore").Attribute("owner") != null
                ? docNode.Element("iisstore").Attribute("owner").Value
                : "admin";
            cassetteFolder = cassetteFolder.Replace('\\', '/');
            if (cassetteFolder[cassetteFolder.Length - 1] != '/') cassetteFolder += '/';
            this.filePath = cassetteFolder + "originals/" + uri.Substring(uri.Length - 9) + ".fog";
            this.root = XElement.Load(filePath);
            var pAtt = root.Attribute("prefix");
            var cAtt = root.Attribute("counter");
            if (pAtt != null && cAtt != null && Int32.TryParse(cAtt.Value, out counter))
            {
                prefix = pAtt.Value;
                //isEditable = true;
            }
        }
        /// <summary>
        /// Конструктор для основного документа кассеты
        /// </summary>
        /// <param name="cassette"></param>
        public RDFDocumentInfo(Cassette cassette)
        {
            this.uri = "iiss://" + cassette.Name + "@iis.nsk.su/meta";
            filePath = cassette.Dir.FullName + "/meta/" + cassette.Name + "_current.fog";
            dbId = cassette.dbId ?? (cassette.Name + "Id");
            owner = cassette.owner;
            this.root =
                //File.Exists(filePath+".xml") ?  XElement.Load(filePath+".xml") : 
                XElement.Load(filePath);
            var pAtt = root.Attribute("prefix");
            var cAtt = root.Attribute("counter");
            if (pAtt != null && cAtt != null && Int32.TryParse(cAtt.Value, out counter))
            {
                prefix = pAtt.Value;
                //isEditable = true;
            }
        }
        public string dbId, uri, owner;//iiss://.....
        public XElement root;
        public XElement Root { set { root = value; } get { if (root == null) root = XElement.Load(filePath); return root; } }
        private string filePath;
        private string prefix;
        private int counter;
        public string NextId() { string id = prefix + counter; counter++; isChanged = true; return id; }
        public void Save()
        {
            if (isChanged)
            {
                root.SetAttributeValue("counter", "" + counter);
                try
                {
                    root.Save(filePath);
                }
                catch (Exception)
                {
                  //  File.Move(filePath, filePath+".xml");
                  //  root.Save(filePath + ".xml");
                }
                isChanged = false;
            }
        }
        public bool isChanged;
        //Нужно для выбора документа пользователю.
        public bool isEditable = false;
        public DateTime ModificationTime { get { return File.GetLastWriteTime(filePath); } }

    }
    public class CassetteInfo
    {
        public Cassette cassette;
        public string fullName, url;
        public string ShortName { get { return fullName.Replace("iiss://", "").Replace("@iis.nsk.su", ""); } }
        public List<RDFDocumentInfo> docsInfo;

      
    }

}
