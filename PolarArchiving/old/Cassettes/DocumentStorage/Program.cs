using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Linq;

namespace Fogid.DocumentStorage
{
    public class Program
    {
        public static void Main()
        {
            string path = "../../../TestData/";
            Console.WriteLine("Start CStorage");
            DStorage storage = new DStorage();
            storage.Init(GetConfig(path));

            Console.WriteLine(storage.GetPhotoFileName("iiss://SampleOfCassette@iis.nsk.su/0001/0001/0002", "s"));
            DbAdapter adapter = new XmlDbAdapter(); //null;//new PBEAdapter();

            storage.InitAdapter(adapter);
            storage.LoadFromCassettesExpress();
            var qu = storage.SearchByName("Марчук");
            foreach (var rec in qu)
            {
                Console.WriteLine(rec.ToString());
            }
            // Марчук А.Г. id = w20070417_5_8436
            string id = "w20070417_5_8436";
            var xx = storage.GetItemByIdBasic(id, true);
            Console.WriteLine(xx.ToString());

            XElement format = new XElement("record",
                new XElement("field", new XAttribute("prop", "http://fogid.net/o/name")),
                new XElement("field", new XAttribute("prop", "http://fogid.net/o/from-date")),
                new XElement("inverse", new XAttribute("prop", "http://fogid.net/o/participant"),
                    new XElement("record",
                        new XElement("field", new XAttribute("prop", "http://fogid.net/o/role")),
                        new XElement("direct", new XAttribute("prop", "http://fogid.net/o/in-org"),
                            new XElement("record",
                                new XElement("field", new XAttribute("prop", "http://fogid.net/o/name")))))),
                null);
            var yy = storage.GetItemById(id, format);
            Console.WriteLine(yy.ToString());
        }
        public static XElement GetConfig(string path)
        {
            string config_text = @"<?xml version='1.0' encoding='utf-8' ?>
<config>

  <database dbname='polar20150825' connectionstring='polar:D:\home\FactographDatabases\polartest\'/>

  <LoadCassette regime='nodata'>" + path+"SampleOfCassette" + @"</LoadCassette>
  <!--LoadCassette write='yes'>D:\home\FactographProjects\Test20130213</LoadCassette-->
</config>    
";
            return XElement.Parse(config_text);
        }
    }
}
