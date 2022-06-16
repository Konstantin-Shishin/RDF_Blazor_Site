using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace SoranCore2.Controllers
{
    //[Route("api/[controller]")]
    //[ApiController]
    public class DbController : Controller
    {
        [HttpGet]
        public ContentResult Ping()
        {
            return Content("Pong", "text/plain");
        }
        [HttpGet]
        public ContentResult Load()
        {
            OAData.OADB.Load();
            return Content("Loaded", "text/plain");
        }
        [HttpGet]
        public ContentResult Reload()
        {
            OAData.OADB.Reload();
            return Content("Reloaded", "text/plain");
        }
        [HttpGet]
        public ContentResult GetConfig()
        {
            XElement result = OAData.OADB.XConfig;
            return Content(result.ToString(), "text/xml", System.Text.Encoding.UTF8);
        }

        [HttpGet]
        public ContentResult SearchByName(string ss, string tt)
        {
            XElement results = new XElement("results");
            IEnumerable<XElement> query = OAData.OADB.SearchByName(ss);
            if (tt != null) query = query.Where(x => x.Attribute("type")?.Value == tt);
            foreach (XElement result in query) results.Add(result);
            return Content(results.ToString(), "text/xml", System.Text.Encoding.UTF8);
        }
        [HttpGet]
        public ContentResult GetItemByIdBasic(string id, string addinverse)
        {

            bool ai = (addinverse == "on" || addinverse == "true") ? true : false;
            XElement result = OAData.OADB.GetItemByIdBasic(id, ai);
            if (result == null) result = new XElement("error");
            return Content(result.ToString(), "text/xml", System.Text.Encoding.UTF8);
        }
        [HttpPost]
        public ContentResult GetItemById(string id, string format)
        {
            XElement result = OAData.OADB.GetItemById(id, XElement.Parse(format));
            if (result == null) result = new XElement("error");
            return Content(result.ToString(), "text/xml", System.Text.Encoding.UTF8);
        }
        [HttpPost]
        public ContentResult PutItem(string item)
        {
            XElement xitem = XElement.Parse(item);
            XElement result = OAData.OADB.PutItem(xitem);
            return Content(result.ToString(), "text/xml", System.Text.Encoding.UTF8);
        }
        [HttpPost]
        public ContentResult UpdateItem(string item)
        {
            XElement xitem = XElement.Parse(item);
            XElement result = OAData.OADB.UpdateItem(xitem);
            return Content(result.ToString(), "text/xml", System.Text.Encoding.UTF8);
        }

        /// <summary>
        /// Обработка на запрос портрета персоны
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        [HttpGet]
        public ContentResult GetPortrait(string id)
        {
            XElement result = OAData.OADB.GetItemById(id, fPortrait);
            if (result == null) result = new XElement("error");
            string contenttype = "text/xml";
            if (false)
            { // формируем XML
            }
            else 
            { // формируем HTML
                result = ToPortrait(result);
                contenttype = "text/html";
            }
            return Content(result.ToString(), contenttype, System.Text.Encoding.UTF8);
        }

        /// <summary>
        /// Преобразование "стандартного" портрета в более удобную форму. Сейчас будем "ваять" html типа комбинации полей и правой колонки 
        /// в раскрытии персоны
        /// </summary>
        /// <param name="xtree"></param>
        /// <returns></returns>
        private XElement ToPortrait(XElement xtree)
        {
            var ph = xtree.Elements("inverse")
                .Where(i => i.Attribute("prop").Value == "http://fogid.net/o/reflected")
                .Select(i => i.Element("record"))
                .Where(r => r.Elements("field").FirstOrDefault(f => f.Attribute("prop").Value == "http://fogid.net/o/ground")?.Value == "portrait")
                .Select(r => r.Element("direct")?.Element("record"))
                .Where(r1 => r1 != null && r1.Attribute("type").Value == "http://fogid.net/o/photo-doc")
                .OrderByDescending(r1 => r1.Elements("field").FirstOrDefault(f => f.Attribute("prop").Value == "http://fogid.net/o/from-date")?.Value)
                .ToArray()
                .FirstOrDefault()
                ;
            string ph_uri = null;
            if (ph != null)
            {
                ph_uri = ph.Elements("field").FirstOrDefault(f => f.Attribute("prop").Value == "http://fogid.net/o/uri").Value;
            }

            string lang = null;
            int nameind = -1;
            XElement[] namefields = xtree.Elements("field")
                .Where(f => f.Attribute("prop").Value == "http://fogid.net/o/name")
                .Where((f, i) => 
                {
                    string la = f.Attribute("{http://www.w3.org/XML/1998/namespace}lang")?.Value;
                    if (nameind == -1 || la == "ru")
                    {
                        nameind = i;
                        lang = la;
                    }
                    return true;
                })
                .ToArray();
            if (nameind == -1) nameind = 0;

            string fd = xtree.Elements("field").FirstOrDefault(f => f.Attribute("prop").Value == "http://fogid.net/o/from-date")?.Value;
            string td = xtree.Elements("field").FirstOrDefault(f => f.Attribute("prop").Value == "http://fogid.net/o/to-date")?.Value;
            string desc = xtree.Elements("field").FirstOrDefault(f => f.Attribute("prop").Value == "http://fogid.net/o/description")?.Value;
            XElement[] titles = xtree.Elements("inverse").Where(i => i.Attribute("prop").Value == "http://fogid.net/o/has-title")
                .Select(i => i.Element("record"))
                .OrderBy(r => r.Elements("field").FirstOrDefault(f => f.Attribute("prop").Value == "http://fogid.net/o/from-date")?.Value)
                .ToArray();
            XElement[] works = xtree.Elements("inverse").Where(i => i.Attribute("prop").Value == "http://fogid.net/o/participant")
                .Select(i => i.Element("record"))
                .Where(r => r.Element("direct")?.Element("record")?
                    .Elements("field")
                    .FirstOrDefault(f => f.Attribute("prop").Value == "http://fogid.net/o/org-classification")?.Value == "organization")
                .OrderBy(r => r.Elements("field").FirstOrDefault(f => f.Attribute("prop").Value == "http://fogid.net/o/from-date")?.Value)
                .ToArray();

            XElement result = new XElement("html",
                new XElement("head"),
                new XElement("body",
                    new XElement("div",
                        (ph_uri == null? null : new XElement("img", new XAttribute("src", Url.Content("~/Docs/GetPhoto?s=small&u=" + ph_uri)), new XAttribute("align", "right"))),
                        new XElement("div", namefields[nameind].Value),
                        namefields.Where((f, i) => i != nameind).Select(f => new XElement("div", f.Value)),
                        (fd == null && td == null) ?null: new XElement("div",
                            (fd == null ? "" : StaticObjects.DatePrinted(fd)) + (td == null ? "" : " - " + StaticObjects.DatePrinted(td))),
                        desc == null ? null : new XElement("div", desc),
                        new XElement("div", new XAttribute("style", "background-color:aquamarine;"),
                            titles.Select(r => new XElement("div", StaticObjects.DatePrinted(StaticObjects.GetField(r, "http://fogid.net/o/from-date")),
                                new XElement("span", StaticObjects.GetField(r, "http://fogid.net/o/degree"))))),
                        works.Select(parti =>
                        {
                            XElement xorg = parti.Element("direct")?.Element("record");
                            return new XElement("div", 
                                StaticObjects.GetField(parti, "http://fogid.net/o/from-date"),
                                StaticObjects.GetField(parti, "http://fogid.net/o/role"),
                                xorg == null ? null : StaticObjects.GetField(xorg, "http://fogid.net/o/name"),
                                null);
                        }),
                        null)),
                null);
            return result;
        }

        private XElement fPortrait =
            new XElement("record", new XAttribute("type", "http://fogid.net/o/person"),
                        new XElement("field", new XAttribute("prop", "http://fogid.net/o/name")),
                        new XElement("field", new XAttribute("prop", "http://fogid.net/o/from-date")),
                        new XElement("field", new XAttribute("prop", "http://fogid.net/o/to-date")),
                        new XElement("field", new XAttribute("prop", "http://fogid.net/o/description")),
                        new XElement("inverse", new XAttribute("prop", "http://fogid.net/o/has-title"),
                            new XElement("record",
                                new XElement("field", new XAttribute("prop", "http://fogid.net/o/degree")),
                                new XElement("field", new XAttribute("prop", "http://fogid.net/o/from-date")),
                            null)),
                        new XElement("inverse", new XAttribute("prop", "http://fogid.net/o/reflected"),
                            new XElement("record",
                                new XElement("field", new XAttribute("prop", "http://fogid.net/o/ground")),
                                new XElement("direct", new XAttribute("prop", "http://fogid.net/o/in-doc"),
                                    new XElement("record", new XAttribute("type", "http://fogid.net/o/photo-doc"),
                                        new XElement("field", new XAttribute("prop", "http://fogid.net/o/name")),
                                        new XElement("field", new XAttribute("prop", "http://fogid.net/o/uri")),
                                        new XElement("field", new XAttribute("prop", "http://fogid.net/o/docmetainfo")),
                                        new XElement("field", new XAttribute("prop", "http://fogid.net/o/description")),
                                        new XElement("field", new XAttribute("prop", "http://fogid.net/o/from-date")),
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
                        new XElement("inverse", new XAttribute("prop", "http://fogid.net/o/referred-sys"),
                            new XElement("record",
                                new XElement("field", new XAttribute("prop", "http://fogid.net/o/alias")),
                                new XElement("field", new XAttribute("prop", "http://fogid.net/o/from-date")),
                                new XElement("field", new XAttribute("prop", "http://fogid.net/o/to-date")),
                                null)),
                        null);
    }


}
