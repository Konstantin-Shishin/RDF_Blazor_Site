using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml;
using System.Xml.Linq;

namespace Polar.Cassettes.DocumentStorage
{
    /// <summary>
    /// Класс, реализующий документное хранилище (модифицированный вариант)
    /// </summary>
    public class DStorage1 : DS
    {
        public DStorage1()
        {
            connection = new CassettesConnection1();
        }
        private class CassInfo
        {
            public string name;
            public string path;
            public bool writable = false;
        }
        private CassInfo[] cassettesToLoad;
        private class FogInfo
        {
            public CassInfo cassette;
            public string pth; // координата файла
            public string owner;
            public string prefix;
            public string counter;
            public bool editable;
        }
        private FogInfo[] fogs;
        private XElement xconfig;

        public override void Init(XElement xconfig)
        {
            this.xconfig = xconfig;
            // Множество кассет
            cassettesToLoad = xconfig.Elements("LoadCassette")
                .Select(lc =>
                {
                    string path = lc.Value;
                    XAttribute wr_att = lc.Attribute("write");
                    return new CassInfo()
                    {
                        name = path.Split('/', '\\').Last(),
                        path = path,
                        writable = (wr_att != null && wr_att.Value == "yes")
                    };
                })
                .ToArray();

            XmlReaderSettings settings = new XmlReaderSettings();
            // Множество фог-документов, находящихся в кассетах
            fogs = cassettesToLoad
                .SelectMany(cass =>
                {
                    var fogs_inoriginals = Directory.GetDirectories(cass.path + "/originals")
                        .SelectMany(d => Directory.GetFiles(d, "*.fog")).ToArray();
                    return Enumerable.Repeat(cass.path + "/meta/" + cass.name + "_current.fog", 1)
                        .Concat(fogs_inoriginals)
                        .Select(pth =>
                        {
                            string owner = null;
                            string prefix = null;
                            string counter = null;
                            using (XmlReader reader = XmlReader.Create(pth, settings))
                            {
                                while (reader.Read())
                                {
                                    if (reader.NodeType == XmlNodeType.Element)
                                    {
                                        if (reader.Name != "rdf:RDF") throw new Exception($"Err: Name={reader.Name}");
                                        owner = reader.GetAttribute("owner");
                                        prefix = reader.GetAttribute("prefix");
                                        counter = reader.GetAttribute("counter");
                                        break;
                                    }
                                }
                            }
                            return new FogInfo()
                            {
                                cassette = cass,
                                pth = pth,
                                owner = owner,
                                prefix = prefix,
                                counter = counter,
                                editable = cass.writable && prefix != null && counter == null 
                            };

                        })
                        ;
                })
                .ToArray();
        }

        private DbAdapter adapter;
        public override void InitAdapter(DbAdapter adapter)
        {
            this.adapter = adapter;
            // Что-то нужное
            adapter.Init(xconfig.Element("database")?.Attribute("connectionstring")?.Value);
        }

        public override void LoadFromCassettesExpress()
        {
            // Загрузка данных из кассет
            adapter.LoadFromCassettesExpress(fogs.Select(f => f.pth), null, null);
            adapter.FinishFillDb(null);
        }

        public override string GetAudioFileName(string u)
        {
            throw new NotImplementedException();
        }

        public override string GetPhotoFileName(string u, string s)
        {
            string cass_name = u.Substring(0, u.Length - 26).Substring(7).ToLower();
            var cass = cassettesToLoad.FirstOrDefault(ci => ci.name.ToLower() == cass_name);
            string fileName = cass == null ? null : cass.path +
                    "/documents/" + s + "/" + u.Substring(u.Length - 9);
            return fileName;
        }

        public override string GetVideoFileName(string u)
        {
            throw new NotImplementedException();
        }

        public override XElement EditCommand(XElement comm)
        {
            throw new NotImplementedException();
        }


    }
}
