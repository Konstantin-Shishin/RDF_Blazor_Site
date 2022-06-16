using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Xml.Linq;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Turgunda7.Controllers
{
    public class ServController : Controller
    {
        public ActionResult GetPhoto(string u, string s)
        {
            string filename = "/question.jpg";
            filename = SObjects.GetPhotoFileName(u, s) + ".jpg";
            //return new FilePathResult(filename, "image/jpeg");
            return new PhysicalFileResult(filename, "image/jpeg");

        }

        [HttpGet]
        public ActionResult SearchByName(string ss)
        {
            string name = ss;
            XElement res = new XElement("sequense", SObjects.SearchByName(name));
            return new ContentResult { ContentType = "text/xml", Content = res.ToString() };
        }
        [HttpGet]
        public ActionResult GetItemByIdSpecial(string id)
        {
            XElement res = SObjects.GetItemByIdSpecial(id);
            return new ContentResult { ContentType = "text/xml", Content = res.ToString() };
        }
        [HttpGet]
        public ActionResult GetItemByIdBasic(string id, bool addinverse)
        {
            XElement res = SObjects.GetItemByIdBasic(id, addinverse);
            return new ContentResult { ContentType = "text/xml", Content = res.ToString() };
        }
        [HttpGet]
        public ActionResult Delete(string id)
        {
            XElement res = SObjects.Delete(id);
            return new ContentResult { ContentType = "text/xml", Content = res.ToString() };
        }
        [HttpPost]
        public ActionResult GetItemById(string id, string format)
        {
            XElement res = SObjects.GetItemById(id, XElement.Parse(format));
            return new ContentResult { ContentType = "text/xml", Content = res.ToString(SaveOptions.DisableFormatting) };
        }
        [HttpPost]
        public ActionResult Add(XElement record)
        {
            XElement res = SObjects.Add(record);
            return new ContentResult { ContentType = "text/xml", Content = res.ToString() };
        }
        [HttpPost]
        public ActionResult AddUpdate(XElement record)
        {
            XElement res = SObjects.AddUpdate(record);
            return new ContentResult { ContentType = "text/xml", Content = res.ToString() };
        }

        // ======= Проверки ========
        [HttpGet]
        public ActionResult Ping()
        {
            return new ContentResult { ContentType = "text/plain", Content = "Pong" };
        }
        [HttpPost]
        public ActionResult PostTest(string id, string innerText)
        {
            return new ContentResult { ContentType = "text/xml", Content = $"<test>{innerText}</test>" };
        }


    }
}
