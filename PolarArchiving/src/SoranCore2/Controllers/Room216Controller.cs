using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;

namespace SoranCore2.Controllers
{
    public class Room216Controller : Controller
    {
        public IActionResult Load()
        {
            OAData.OADB.Close();
            OAData.OADB.Init();
            OAData.OADB.Load();
            return Redirect("~/Home/Index");
        }
    }
}