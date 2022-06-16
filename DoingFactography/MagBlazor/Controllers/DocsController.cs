using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MagBlazor.Controllers
{
    public class DocsController : Controller
    {
        [HttpGet("docs/GetImage")]
        public IActionResult GetImage(string u, string s)
        {
            string path = OAData.OADB.GetFilePath(u, s);
            return PhysicalFile(path, "image/jpg");
        }

        [HttpGet("docs/GetVideo")]
        public IActionResult GetVideo(string u)
        {
            string path = OAData.OADB.GetFilePath(u, "normal");
            if (path == null) return NotFound();
            int pos = path.LastIndexOf('.');
            if (pos == -1) return NotFound();
            return PhysicalFile(path, "video/" + path.Substring(pos + 1));
        }
        [HttpGet("docs/GetPdf")]
        public IActionResult GetPdf(string u)
        {
            string path = OAData.OADB.GetFilePath(u, null);
            if (path == null) return NotFound();
            return PhysicalFile(path, "application/pdf");
        }

    }
}
