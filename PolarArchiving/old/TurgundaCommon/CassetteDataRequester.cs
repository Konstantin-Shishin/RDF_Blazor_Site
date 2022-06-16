using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Net;
using System.Xml.Linq;

namespace CassetteData
{
    public class CassetteDataRequester : Polar.Cassettes.DocumentStorage.DbAdapter
    {
        // Источник данных или null 
        public string DataSrc { get { return _dataSrc; } }
        private string _dataSrc;
        private string host_port_contr = "http://localhost:52018/Serv/";
        public CassetteDataRequester(string src)
        {
            _dataSrc = src;
            if (src != null) host_port_contr = src + (src[src.Length - 1] != '/' ? "/" : "");
            Turgunda7.SObjects.OADataUri = host_port_contr;
        }
        public string Ping()
        {
            WebRequest request = WebRequest.Create(host_port_contr + "db/Ping");
            HttpWebResponse response = (HttpWebResponse)request.GetResponse();
            return "Pong";
        }
        public override IEnumerable<XElement> SearchByName(string searchstring)
        {
            string requeststring = host_port_contr + "db/SearchByName?ss=" + searchstring;
            XElement result = AskByRequest(requeststring);
            return result.Elements();
        }

        private static XElement AskByRequest(string requeststring)
        {
            WebRequest request = WebRequest.Create(requeststring);
            HttpWebResponse response = (HttpWebResponse)request.GetResponse();
            Stream dataStream = response.GetResponseStream();
            XElement result = XElement.Load(dataStream);
            return result;
        }

        public override XElement GetItemByIdBasic(string id, bool addinverse)
        {
            string requeststring = host_port_contr + "db/GetItemByIdBasic?id=" + id + "&addinverse=" + addinverse.ToString();
            XElement result = AskByRequest(requeststring);
            return result;
        }
        public override XElement GetItemById(string id, XElement format)
        {
            string requeststring = host_port_contr + "db/GetItemById";
            WebRequest request = WebRequest.Create(requeststring);
            request.Method = "POST";
            request.ContentType = "application/x-www-form-urlencoded";
            string contentstring = "id=" + id + "&format=" + System.Web.HttpUtility.UrlEncode(format.ToString(SaveOptions.DisableFormatting));
            byte[] buffer = System.Text.Encoding.UTF8.GetBytes(contentstring);
            request.ContentLength = buffer.Length;
            Stream requStream = request.GetRequestStream();
            requStream.Write(buffer, 0, buffer.Length);

            HttpWebResponse response = (HttpWebResponse)request.GetResponse();
            Stream dataStream = response.GetResponseStream();
            XElement result = XElement.Load(dataStream);
            return result;
        }
        public override XElement PutItem(XElement item)
        {
            string requeststring = host_port_contr + "db/PutItem";
            WebRequest request = WebRequest.Create(requeststring);
            request.Method = "POST";
            request.ContentType = "application/x-www-form-urlencoded";
            string contentstring = "item=" + System.Web.HttpUtility.UrlEncode(item.ToString(SaveOptions.DisableFormatting));
            byte[] buffer = System.Text.Encoding.UTF8.GetBytes(contentstring);
            request.ContentLength = buffer.Length;
            Stream requStream = request.GetRequestStream();
            requStream.Write(buffer, 0, buffer.Length);

            HttpWebResponse response = (HttpWebResponse)request.GetResponse();
            Stream dataStream = response.GetResponseStream();
            XElement result = XElement.Load(dataStream);
            return result;
        }

        public override XElement Add(XElement record)
        {
            throw new NotImplementedException();
        }

        public override XElement AddUpdate(XElement record)
        {
            throw new NotImplementedException();
        }

        public override XElement Delete(string id)
        {
            throw new NotImplementedException();
        }

        public override void FinishFillDb(Action<string> turlog)
        {
            throw new NotImplementedException();
        }



        public override XElement GetItemByIdSpecial(string id)
        {
            throw new NotImplementedException();
        }

        public override void Init(string connectionstring)
        {
            //throw new NotImplementedException();
        }

        public override void LoadFromCassettesExpress(IEnumerable<string> fogfilearr, Action<string> turlog, Action<string> convertlog)
        {
            throw new NotImplementedException();
        }

        public override void LoadXFlowUsingRiTable(IEnumerable<XElement> xflow)
        {
            throw new NotImplementedException();
        }

        public override void Save(string filename)
        {
            throw new NotImplementedException();
        }


        public override void StartFillDb(Action<string> turlog)
        {
            throw new NotImplementedException();
        }

    }
}
