using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
//using System.Web.Mvc;
using System.Xml.Linq;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;

namespace Turgunda7.Controllers
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
            var acc = SObjects.accounts.Elements("account").FirstOrDefault(a => a.Attribute("login").Value == uuser);
            if (acc == null || acc.Attribute("pass").Value != pass) return View();

            Turgunda7.Models.UserModel umodel = new Models.UserModel(this.Request);
            umodel.ActivateUserMode(this.Response, uuser);
            return RedirectToAction("Index", "Home");
        }
        public ActionResult Logout()
        {
            Turgunda7.Models.UserModel umodel = new Models.UserModel(this.Request);
            umodel.DeactivateUserMode(this.Response);
            return RedirectToAction("Index", "Home");
        }
        public ActionResult Registration()
        {
            return View();
        }
        [HttpPost]
        public ActionResult Registration(string uuser, string pass1, string pass2)
        {
            Turgunda7.Models.UserModel umodel = new Models.UserModel(this.Request);
            //umodel.ActivateUserMode(this.Response, uuser);
            if (!string.IsNullOrEmpty(pass1) 
                && pass1 == pass2
                && SObjects.accounts.Elements("account").All(a => a.Attribute("login").Value != uuser)
                )
            {
                XElement account =
                    new XElement("account",
                        new XAttribute("login", uuser), new XAttribute("pass", pass1),
                        new XElement("role", "user"),
                        SObjects.accounts.Elements("account").Count() == 0 ? new XElement("role", "admin") : null);
                SObjects.accounts.Add(account);
                SObjects.AccountsSave();
                return RedirectToAction("Logon", "Account");
            }
            
            return View();
        }
    }
}