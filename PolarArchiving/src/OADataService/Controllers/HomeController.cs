using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;

using OAData;

namespace OADataService.Controllers
{
    public class HomeController : Controller
    {
        //public IActionResult Index() { return View(); }
        public IActionResult Index0()
        {
            ContentResult cr = new ContentResult() { ContentType = "text/html" };
            string message = "OK! ";// + OAData.CassDirPath("iiss://SypCassete@iis.nsk.su");
            cr.Content = 
@"<html><head><meta charset='utf-8'/></head>
<body>
<h1>Сервис работает!</h1>
<div>" + message + @"</div>
<pre>
" + OAData.OADB.look + @"
</pre>
<form method='post' action='/db/GetItemById'>
  <input type='text' name='id' ></input>
  <textarea name='format' ></input>
</form>
</body></html>";
            return cr;
        }
        public ActionResult Test()
        {
            return View();
        }

    }
}