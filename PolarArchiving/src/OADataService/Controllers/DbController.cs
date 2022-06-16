using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Xml.Linq;
using Microsoft.AspNetCore.Mvc;

using OAData;

namespace OADataService.Controllers
{
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
    }
}