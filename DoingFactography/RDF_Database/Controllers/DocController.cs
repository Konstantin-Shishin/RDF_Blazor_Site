using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Konstantin2.Controllers
{
    public class DocController : Controller
    {
        public IActionResult Index(string u, bool size)
        {
            //по uri получаем имя кассеты
            string cas_name = u.Remove(0, 7); // удалили iiss://
            cas_name = cas_name.Substring(0, cas_name.Length - 26); // удалили @iis.nsk.su/0001/0001/0006

            string filename = null;
            if (size) // маленькая картинка
            {
                filename = Infobase.cassettes_dict[cas_name] + $"/documents/small/0001/{u.Substring(u.Length - 4)}.jpg";
            }
            else  // большая картинка
            {
                filename = Infobase.cassettes_dict[cas_name] + $"/documents/normal/0001/{u.Substring(u.Length - 4)}.jpg";
            }           
            return new PhysicalFileResult(filename, "image/jpg");
        }

    }
}
