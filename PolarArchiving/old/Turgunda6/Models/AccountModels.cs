using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Turgunda6.Models
{
    public class UserModel
    {
        private const string turgunda_string = "turgunda_user";
        private HttpRequestBase requ;
        public UserModel(HttpRequestBase requ)
        {
            this.requ = requ;
        }
        public void ActivateUserMode(HttpResponseBase response, string uuser)
        {
            if (string.IsNullOrEmpty(uuser)) return;
            response.SetCookie(new HttpCookie(turgunda_string, uuser)
            {
                Expires = new DateTime(DateTime.Now.AddHours(16).Ticks)
            });
        }
        public void DeactivateUserMode(HttpResponseBase response)
        {
            response.SetCookie(new HttpCookie(turgunda_string)
            {
                Expires = new DateTime(DateTime.Now.Subtract(new TimeSpan(10000000)).Ticks)
            });
        }
        private string _uuser = null; 
        public string Uuser
        {
            get
            {
                if (_uuser == null)
                {
                    var cook = requ.Cookies[turgunda_string];
                    if (cook != null) _uuser = cook.Value;
                }
                return _uuser;
            }
        }

    }
}