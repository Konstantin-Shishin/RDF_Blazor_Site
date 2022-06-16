using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;

namespace SoranCore2.Controllers
{
    public class DocsController : Controller
    {
        [HttpGet("docs/GetPhoto")]
        public IActionResult GetPhoto(string u, string s)
        {
            if (u == null) return NotFound();
            u = System.Web.HttpUtility.UrlDecode(u);
            var cass_dir = OAData.OADB.CassDirPath(u);
            if (cass_dir == null) return NotFound();
            string last10 = u.Substring(u.Length - 10);
            string subpath;
            string method = s;
            //if (method == null) subpath = "/originals";
            if (method == "small") subpath = "/documents/small";
            else if (method == "medium") subpath = "/documents/medium";
            else subpath = "/documents/normal"; // (method == "n")
            string path = cass_dir + subpath + last10 + ".jpg";
            return PhysicalFile(path, "image/jpg");
        }
        [HttpGet("docs/GetVideo")]
        public IActionResult GetVideo(string u)
        {
            if (u == null) return NotFound();
            u = System.Web.HttpUtility.UrlDecode(u);
            var cass_dir = OAData.OADB.CassDirPath(u);
            if (cass_dir == null) return NotFound();
            string last10 = u.Substring(u.Length - 10);
            string dirpath = cass_dir + "/documents/medium/" + last10.Substring(0, 6);
            System.IO.DirectoryInfo dinfo = new DirectoryInfo(dirpath);
            //Console.WriteLine("GetVideo dinfo = " + dinfo);
            var finfo = dinfo.GetFiles(last10.Substring(6) + ".*").LastOrDefault();
            if (finfo == null) return NotFound();
            string path = finfo.FullName;
            //Console.WriteLine("GetVideo path = " + path);
            int lastpoint = path.LastIndexOf('.');
            //string uniquename = u.Split(':', '/').Aggregate((acc, s) => acc + s);
            return PhysicalFile(path, "video/" + path.Substring(lastpoint + 1));
        }
        [HttpGet("docs/GetPdf")]
        public IActionResult GetPdf(string u)
        {
            if (u == null) return NotFound();

            // if (true)
            // {
            //     // http://www.sbras.info/system/files/archive/archive1961-2009/1961_01.pdf
            //     return Redirect("http://www.sbras.info/system/files/archive/archive1961-2009/1961_01.pdf");
            // }
            u = System.Web.HttpUtility.UrlDecode(u);
            var cass_dir = OAData.OADB.CassDirPath(u);
            if (cass_dir == null) return NotFound();
            string last10 = u.Substring(u.Length - 10);
            string dirpath = cass_dir + "/originals/" + last10.Substring(0, 6);
            System.IO.DirectoryInfo dinfo = new DirectoryInfo(dirpath);
            var finfo = dinfo.GetFiles(last10.Substring(6) + ".*").LastOrDefault();
            if (finfo == null) return NotFound();
            string path = finfo.FullName;
            //int lastpoint = path.LastIndexOf('.');
            //string uniquename = u.Split(':', '/', '@').Aggregate((acc, s) => acc + s) + ".pdf";
            //return PhysicalFile(path, "application/octet-stream");
            return PhysicalFile(path, "application/pdf");
            //return File(System.IO.File.Open(path, FileMode.Open, FileAccess.Read), "application/pdf", uniquename);
        }
    }
}