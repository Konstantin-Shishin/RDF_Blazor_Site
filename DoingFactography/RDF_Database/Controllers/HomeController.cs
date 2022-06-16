using Konstantin2.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using RDFEngine;

namespace Konstantin2.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;

        public HomeController(ILogger<HomeController> logger)
        {
            _logger = logger;
        }

        public IActionResult Index()
        {
            return View();
        }

        public IActionResult Person(string id)
        {
            RRecord rr = Infobase.engine.GetRRecord(id);
            if (rr == null)
            {
                return View("Index");
            }
            return View("Person",rr);
        }

        public IActionResult Portrait(string id)
        {
            RRecord rr = Infobase.engine.GetRRecord(id);
            if (rr == null)
            {
                return View("Index");
            }
            return View("Portrait", rr);
        }

        public IActionResult Show(string id, string searchstring)
        {
            if (searchstring == null)
            {
                RRecord rr = Infobase.engine.GetRRecord(id);
                if (rr == null)
                {
                    return View("Index");
                }
                return View("Show", rr);
            }
            else
            {
                IEnumerable<RRecord> rrs = Infobase.engine.RSearch(searchstring);
                return View("Search", rrs);
            }
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
