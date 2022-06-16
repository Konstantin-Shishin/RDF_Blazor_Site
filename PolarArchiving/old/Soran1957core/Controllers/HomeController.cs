using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using System.Xml.Linq;
using Microsoft.AspNetCore.Mvc;
using Soran1957core.Models;

namespace Soran1957core.Controllers
{
    public class HomeController : Controller
    {
        public static string tilda = null;
        public IActionResult Index()
        {
            return View();
        }

        public IActionResult Privacy()
        {
            return View();
        }

        public IActionResult P()
        {
            if (tilda == null) tilda = HttpContext.Request.PathBase;
            ContentResult cr = new ContentResult() { ContentType = "text/html" };
            XElement html = new XElement("html",
                new XElement("head", 
                    new XElement("meta", new XAttribute("charset", "utf-8")),
                    new XElement("link", new XAttribute("src", tilda + "/Styles.css")),
                    null),
                new XElement("body",
                    new XElement("img", new XAttribute("src", tilda + "/logo2.gif")),
                    new XElement("img", new XAttribute("src", tilda + "/logo1.jpg")),
                    new XElement("img", new XAttribute("src", tilda + "/tmp_foto.jpg")),
                    new XElement("img", new XAttribute("src", tilda + "/logo4.gif")),
                    new XElement("img", new XAttribute("src", tilda + "/tmp_foto.jpg")),
                    new XElement("div", 

                        new XElement("h1", $"Привет: {tilda}!"),
                        null)));
            html = Pager.ConstructPage(HttpContext);
            cr.Content = "<!DOCTYPE html>\n" + html.ToString(); // (SaveOptions.DisableFormatting);
            return cr;
        }


        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
