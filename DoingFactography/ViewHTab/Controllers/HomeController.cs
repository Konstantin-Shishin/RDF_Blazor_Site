using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using RDFEngine;
using ViewHTab.Models;

namespace ViewHTab.Controllers
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

        public IActionResult Portrait(string id)
        {
            var model = Infobase.engine.GetRRecord(id);
            return View("Portrait", model);
        }
        // Контроллер
        public IActionResult Portrait3(string id)
        {
            var erec = Infobase.engine.BuildPortrait(id);
            P3Model model = (new P3Model()).Build(erec);
            return View("Portrait3", model);
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
