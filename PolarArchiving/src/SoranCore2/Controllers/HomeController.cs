using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using System.Xml.Linq;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using SoranCore2.Models;

namespace SoranCore2.Controllers
{
    public class HomeController : Controller
    {
        public IActionResult Index()
        {
            var model = new IndexModel();

            string sdir = HttpContext.Request.Query["sdirection"].FirstOrDefault();
            
            // Поработаем с сессией
            if (sdir == null)
            {
                sdir = HttpContext.Session.GetString("sdirection");
                if (sdir == null) sdir = "person"; 
            }
            if (sdir != null) model.TabDirection = sdir;
            HttpContext.Session.SetString("sdirection", sdir);

            string p = HttpContext.Request.Query["p"].FirstOrDefault();
            string id = HttpContext.Request.Query["id"].FirstOrDefault();
            string pg_str = HttpContext.Request.Query["pg"].FirstOrDefault();
            if (pg_str != null) model.Pg = Int32.Parse(pg_str);

            if (p == null && id == null) id = "w20070417_7_1744";
            XElement xrec = null;
            if (p == "search")
            {
                string searchstring = HttpContext.Request.Query["searchstring"].FirstOrDefault();
                IEnumerable<XElement> query = OAData.OADB.SearchByName(searchstring)
                    .Distinct(new RecordIdComparer())
                    .ToArray();
                var list = new List<object[]>();
                foreach (XElement el in query)
                {
                    string t = el.Attribute("type").Value;
                    string name = el.Elements("field").FirstOrDefault(f => f.Attribute("prop").Value == "http://fogid.net/o/name")?.Value;
                    string name1 = StaticObjects.GetField(el, "http://fogid.net/o/name");
                    if (t == "http://fogid.net/o/person")
                    {
                        list.Add(new object[] { el.Attribute("id").Value, name });
                    }
                }
                model.SearchResults = list;
            }
            else if (id != null && (xrec = OAData.OADB.GetItemByIdBasic(id, false)) != null) // Построение портрета
            {
                // Сначала надо выяснить тип сущности, для этого запрашиваем ее без формата
                model.Id = xrec.Attribute("id").Value;
                model.Type = xrec.Attribute("type").Value;
                // Заодно вычислим важные поля
                //foreach (XElement el in xrec.Elements("field"))
                //{
                //    string prop = el.Attribute("prop").Value;
                //    if (prop == "http://fogid.net/o/name") model.Name = el.Value;
                //    else if (prop == "http://fogid.net/o/from-date") model.StartDate = el.Value;
                //    else if (prop == "http://fogid.net/o/to-date") model.EndDate = el.Value;
                //    else if (prop == "http://fogid.net/o/description") model.Description = el.Value;
                //    else if (prop == "http://fogid.net/o/uri") model.Uri = el.Value;
                //}
                model.Name = StaticObjects.GetField(xrec, "http://fogid.net/o/name");
                model.StartDate = StaticObjects.GetField(xrec, "http://fogid.net/o/from-date");
                model.EndDate = StaticObjects.GetField(xrec, "http://fogid.net/o/to-date");
                model.Description = StaticObjects.GetField(xrec, "http://fogid.net/o/description");
                model.Uri = StaticObjects.GetField(xrec, "http://fogid.net/o/uri");
                model.Docmetainfo = StaticObjects.GetField(xrec, "http://fogid.net/o/docmetainfo");

                // Теперь определим формат для этого типа
                if (model.Type == "http://fogid.net/o/photo-doc")
                {
                    model.XRecord = OAData.OADB.GetItemById(id, model.Formats["http://fogid.net/o/document"]);
                    model.BuildPortrait();

                }
                else if (model.Formats.ContainsKey(model.Type))
                {
                    XElement format = model.Formats[model.Type];
                    
                    model.XRecord = OAData.OADB.GetItemById(id, format);
                    model.BuildPortrait();
                }
                else if (new string[] {"http://fogid.net/o/geosys", "http://fogid.net/o/country", "http://fogid.net/o/region",
                        "http://fogid.net/o/city", "http://fogid.net/o/geosys-special" }.Contains(model.Type))
                {
                    XElement format = new XElement("record", //new XAttribute("type", "http://fogid.net/o/person"),
                    new XElement("field", new XAttribute("prop", "http://fogid.net/o/name")),
                    new XElement("field", new XAttribute("prop", "http://fogid.net/o/from-date")),
                    new XElement("field", new XAttribute("prop", "http://fogid.net/o/to-date")),
                    new XElement("field", new XAttribute("prop", "http://fogid.net/o/description")),
                    new XElement("inverse", new XAttribute("prop", "http://fogid.net/o/reflected"),
                        new XElement("record",
                            //new XElement("field", new XAttribute("prop", "http://fogid.net/o/ground")),
                            new XElement("direct", new XAttribute("prop", "http://fogid.net/o/in-doc"),
                                new XElement("record",
                                    new XElement("field", new XAttribute("prop", "http://fogid.net/o/name")),
                                    new XElement("field", new XAttribute("prop", "http://fogid.net/o/uri")),
                                    new XElement("field", new XAttribute("prop", "http://fogid.net/o/docmetainfo")),
                                    new XElement("field", new XAttribute("prop", "http://fogid.net/o/description")),
                                    new XElement("field", new XAttribute("prop", "http://fogid.net/o/from-date")),
                                    null)))));
                    model.XRecord = OAData.OADB.GetItemById(id, format);
                    model.BuildPortrait();
                }
                else
                {
                    model.XRecord = xrec;
                }
                // Вычисление docidarr. Это имеет смысл, когда задано значение ir или ic, тогда мы сделаем выборку, дойдя до документов и превратив выборку в массив.
                string ir = HttpContext.Request.Query["ir"].FirstOrDefault();
                string ic = HttpContext.Request.Query["ic"].FirstOrDefault();
                if (!string.IsNullOrEmpty(ir))
                {
                    model.ir = ir;
                    XElement doctree = OAData.OADB.GetItemById(ir, new XElement("record",
                        new XElement("inverse", new XAttribute("prop", "http://fogid.net/o/reflected"),
                            new XElement("record",
                                new XElement("direct", new XAttribute("prop", "http://fogid.net/o/in-doc"),
                                    new XElement("record",
                                        new XElement("field", new XAttribute("prop", "http://fogid.net/o/name")),
                                        new XElement("field", new XAttribute("prop", "http://fogid.net/o/from-date")),
                                        null))))));
                    model.docidarr = doctree.Elements("inverse")
                        .Select(inv => inv.Element("record"))
                        .Distinct(new IdEqual())
                        .OrderBy(r => r.Element("direct")?.Element("record"), new PhotosFirst())
                        .Select(r => r?.Element("direct")?.Element("record")?.Attribute("id")?.Value)
                        .Where(d => d != null)
                        .ToArray();
                    model.Check_docidarr(ir, model.docidarr);
                }
                else if (!string.IsNullOrEmpty(ic))
                {
                    model.ic = ic;
                    XElement doctree = OAData.OADB.GetItemById(ic, new XElement("record",
                        new XElement("inverse", new XAttribute("prop", "http://fogid.net/o/in-collection"),
                            new XElement("record",
                                new XElement("direct", new XAttribute("prop", "http://fogid.net/o/collection-item"),
                                    new XElement("record", new XElement("field", new XAttribute("prop", "http://fogid.net/o/name"))))))));
                    model.docidarr = doctree.Elements("inverse")
                        .Select(inv => inv.Element("record")?.Element("direct")?.Element("record"))
                        //.Attribute("id")?.Value)
                        .Where(r => r != null)
                        .OrderBy(r => StaticObjects.GetField(r, "http://fogid.net/o/name"))
                        .Select(r => r.Attribute("id").Value)
                        .ToArray();
                }
            }

            return View(model);
        }

        public IActionResult UsefullLinks()
        {
            return View();
        }

        public IActionResult Show()
        {
            string id = HttpContext.Request.Query["id"].FirstOrDefault();
            string ss = HttpContext.Request.Query["ss"].FirstOrDefault();
            ShowModel model = new ShowModel(id, ss);
            return View(model);
        }

        private readonly ILogger<HomeController> _logger;

        public HomeController(ILogger<HomeController> logger)
        {
            _logger = logger;
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }

    class RecordIdComparer : IEqualityComparer<XElement>
    {
        public bool Equals(XElement r1, XElement r2)
        {
            if (r1 == null || r2 == null)
                return false;
            else if (r1.Attribute("id").Value == r2.Attribute("id").Value)
                return true;
            else
                return false;
        }

        public int GetHashCode(XElement rec)
        {
            string code = rec.Attribute("id").Value;
            return code.GetHashCode();
        }
    }

}
