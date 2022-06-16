using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace BlazorServer.Controllers
{
    public class ImageController : Controller
    {
        [HttpGet("docs/GetImage")]

        public IActionResult GetImage(string u, string s)
        {
            if (u == null) return NotFound();
            u = System.Web.HttpUtility.UrlDecode(u);
            var cass_dir = OAData.OADB.CassDirPath(u);
            if (cass_dir == null) return NotFound();
            string[] last10 = u.Substring(u.Length - 10).Split("/");
            last10[last10.Length - 1] = last10[last10.Length - 1] + ".jpg";
            string method = s;
            //if (method == null) subpath = "/originals";
            if (method != "small" && method != "medium")
            {
                method = "normal";
            }
            var root = Startup.getContentRoot();
            string[] pathToCombine = { root, cass_dir, "documents", method };
            pathToCombine = pathToCombine.Concat(last10).ToArray();
            string path = System.IO.Path.Combine(pathToCombine);

            return PhysicalFile(path, "image/jpg");
        }
    }
}
