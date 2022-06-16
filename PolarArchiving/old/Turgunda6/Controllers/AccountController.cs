using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace Turgunda6.Controllers
{
    public class AccountController : Controller
    {
        public ActionResult Logon()
        {
            return View();
        }
        [HttpPost]
        public ActionResult Logon(string uuser, string pass)
        {
            Turgunda6.Models.UserModel umodel = new Models.UserModel(this.Request);
            umodel.ActivateUserMode(this.Response, uuser);
            return RedirectToAction("Index", "Home");
        }
        public ActionResult Logout()
        {
            Turgunda6.Models.UserModel umodel = new Models.UserModel(this.Request);
            umodel.DeactivateUserMode(this.Response);
            return RedirectToAction("Index", "Home");
        }
    }
}