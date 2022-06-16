using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;

namespace Turgunda7.Models
{
    public class UserModel
    {
        private const string turgunda_string = "turgunda_user";
        private HttpRequest requ;
        public UserModel(HttpRequest requ)
        {
            this.requ = requ;
        }
        public void ActivateUserMode(HttpResponse response, string uuser)
        {
            if (string.IsNullOrEmpty(uuser)) return;
            response.Cookies.Append(turgunda_string, uuser, new CookieOptions() { Expires = new DateTime(DateTime.Now.AddHours(16).Ticks) });
        }
        public void DeactivateUserMode(HttpResponse response)
        {
            response.Cookies.Delete(turgunda_string, new CookieOptions()
            { Expires = new DateTime(DateTime.Now.Subtract(new TimeSpan(10000000)).Ticks) });
        }
        private string _uuser = null; 
        public string Uuser
        {
            get
            {
                if (_uuser == null)
                {
                    var cook = requ.Cookies[turgunda_string];
                    if (cook != null) _uuser = cook;
                }
                return _uuser;
            }
        }
        public bool IsInRole(string role)
        {
            var acc = SObjects.accounts.Elements("account").FirstOrDefault(a => a.Attribute("login").Value == Uuser);
            if (acc == null) return false;
            return acc.Elements("role").Any(r => r.Value == role);
        }
        public string ActiveCassette()
        {
            var acc = SObjects.accounts.Elements("account").FirstOrDefault(a => a.Attribute("login").Value == Uuser);
            if (acc == null) return null;
            return acc.Attribute("active")?.Value;
        }
        /// <summary>
        /// Сохраняет активную кассету в accounts и возращает истину если действительно изменение было
        /// </summary>
        /// <param name="active"></param>
        /// <returns></returns>
        public bool SetActiveCassette(string active)
        {
            var acc = SObjects.accounts.Elements("account").FirstOrDefault(a => a.Attribute("login").Value == Uuser);
            if (acc == null) return false;
            var att = acc.Attribute("active");
            if (att == null) acc.Add(new System.Xml.Linq.XAttribute("active", active));
            else
            {
                if (att.Value == active) return false;
                att.Value = active;
            }
            return true;
        }
        public void Save()
        {
            SObjects.SaveAccounts();
        }

    }
}