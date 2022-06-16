using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Net;
using System.Threading.Tasks;
using System.Xml.Linq;
using Polar.Cassettes.DocumentStorage;

namespace CassetteData
{
    class CassetteDataListener
    {
        // This example requires the System and System.Net namespaces.
        internal static async Task  SimpleListener(DbAdapter engine, string[] prefixes)
        {
            // URI prefixes are required,
            // for example "http://contoso.com:8080/index/".
            if (prefixes == null || prefixes.Length == 0)
                throw new ArgumentException("prefixes");

            // Create a listener.
            HttpListener listener = new HttpListener();
            // Add the prefixes.
            foreach (string s in prefixes)
            {
                listener.Prefixes.Add(s);
            }
            listener.Start();
            bool tocontinue = true;

            while (tocontinue)
            {
                Console.WriteLine("Listening...");
                // Note: The GetContext method blocks while waiting for a request. 
                HttpListenerContext context = await listener.GetContextAsync();
                HttpListenerRequest request = context.Request;

                // Obtain a response object.
                HttpListenerResponse response = context.Response;

                string responseString = "";
                response.ContentEncoding = System.Text.Encoding.UTF8;
                response.ContentType = "text/xml";
                string comm = request.QueryString["c"];
                if (comm == "SearchByName")
                {
                    string name = request.QueryString["name"];
                    var res = engine.SearchByName(name);
                    responseString = (new XElement("sequense", res.Select(r => new XElement(r)))).ToString();
                }
                else if (comm == "GetItemByIdBasic")
                {
                    string id = request.QueryString["id"];
                    string addinverse = request.QueryString["addinverse"];
                    var res = engine.GetItemByIdBasic(id, addinverse == "true" ? true : false);
                    responseString = res.ToString();
                }
                else if (comm == "GetItemById")
                {
                    Console.WriteLine("GetItemById");
                    string id = request.QueryString["id"];
                    Console.WriteLine("id=" + id);

                    Stream rstream = request.InputStream;
                    XElement format = XElement.Load(rstream);
                    Console.WriteLine(format.ToString());
                    var res = engine.GetItemById(id, format);
                    responseString = res.ToString();
                }


                // Construct a response.
                byte[] buffer = System.Text.Encoding.UTF8.GetBytes(responseString);
                // Get a response stream and write the response to it.
                response.ContentLength64 = buffer.Length;
                System.IO.Stream output = response.OutputStream;
                output.Write(buffer, 0, buffer.Length);
                // You must close the output stream.
                output.Close();
            }

            listener.Stop();
        }
    }
}
