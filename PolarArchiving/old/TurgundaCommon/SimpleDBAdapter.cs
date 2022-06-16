using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
//using Polar.DB;
//using Polar.CellIndexes;

namespace Polar.Cassettes.DocumentStorage
{
    /// <summary>
    /// Простая база данных на основе Xml-построения. 
    /// </summary>
    public class SimpleDbAdapter : DbAdapter
    {
        private XElement db = null;
        private Action<string> errors = s => { Console.WriteLine(s); };
        private int totalelements = 0;
        System.Diagnostics.Stopwatch sw = new System.Diagnostics.Stopwatch();
        /// <summary>
        /// Инициирование базы данных
        /// </summary>
        /// <param name="connectionstring">префикс варианта базы данных xml:, больше в connectionstring ничего не существенно </param>
        public override void Init(string connectionstring)
        {
            db = new XElement("db");
            totalelements = 0;
        }

        // Загрузка базы данных
        public override void StartFillDb(Action<string> turlog)
        {
            sw.Start();
        }
        public override void LoadFromCassettesExpress(IEnumerable<string> fogfilearr, Action<string> turlog, Action<string> convertlog)
        {
            foreach (string filename in fogfilearr)
            {
                XElement fog = XElement.Load(filename);
                totalelements += fog.Elements().Count();
                XDocument xdoc = new XDocument();
                XElement fog2 = new XElement(XName.Get("RDF", "http://www.w3.org/1999/02/22-rdf-syntax-ns#"));
                XNamespace r = "http://www.w3.org/1999/02/22-rdf-syntax-ns#";
                XNamespace f = "http://fogid.net/o/";
                XElement root = new XElement(r + "RDF",
                    new XAttribute(XNamespace.Xmlns + "rdf", "http://www.w3.org/1999/02/22-rdf-syntax-ns#"),
                    new XAttribute(XNamespace.Xmlns + "fog", "http://fogid.net/o/"),
                    null);
                foreach (XElement xel in fog.Elements())
                {
                    XElement xel2 = xel;
                    if (xel.Name.LocalName == "delete")
                    {
                        this.count_delete++;
                        xel2 = new XElement("del", new XAttribute(r + "about", xel.Attribute("id").Value));
                    }
                    if (xel.Name.LocalName == "substitute") this.count_substitute++;

                    var pref1 = xel.GetPrefixOfNamespace("http://www.w3.org/1999/02/22-rdf-syntax-ns#");
                    if (pref1 == null) continue; // Это не фог! TODO: нужна диагностика!
                    var pref2 = xel.GetPrefixOfNamespace("http://fogid.net/o/");
                    if (pref2 == null)
                    {
                        var xel3 = DbAdapter.ConvertXElement(xel2);
                        //var xel4 = new XElement(xel3.Name, xel3.Elements()
                        //    .Where(el => el.Attribute(r+"resource") != null || string.IsNullOrEmpty(el.Value))
                        //    .Select(el => new XElement(el))
                        //    );
                        foreach (var e in xel3.Elements())
                        {
                            if (e.Attribute(r+"resource") == null && string.IsNullOrEmpty(e.Value))
                            {
                                e.Remove();
                            }
                        }
                        root.Add(xel3);    
                    }
                }
                if (root.Elements().Count() > 0)
                {
                    root.Add(
                        new XAttribute(fog.Attribute("dbid")),
                        new XAttribute(fog.Attribute("owner")),
                        new XAttribute(fog.Attribute("uri")),
                        new XAttribute(fog.Attribute("prefix")),
                        new XAttribute(fog.Attribute("counter")),
                        null);
                    root.Save(filename + ".xml");
                    fog = root;
                }
                db.Add(fog);
            }
        }
        public override void FinishFillDb(Action<string> turlog)
        {
            sw.Stop();
            Console.WriteLine($"Load ok. duration = {sw.ElapsedMilliseconds} totalelements={totalelements}");
        }


        public override void Save(string filename)
        {
            throw new Exception("23434");
        }

        public override void LoadXFlowUsingRiTable(IEnumerable<XElement> xflow)
        {
            throw new Exception("29484");
        }
        public override IEnumerable<XElement> SearchByName(string searchstring)
        {
            throw new Exception("29486");
        }
        public override XElement GetItemByIdBasic(string id, bool addinverse)
        {
            throw new Exception("29487");
        }
        public override XElement GetItemById(string id, XElement format)
        {
            throw new Exception("29488");
        }
        public override XElement GetItemByIdSpecial(string id)
        {
            return GetItemByIdBasic(id, true);
        }

        private XElement RemoveRecord(string id)
        {
            throw new Exception("29489");
        }
        public override XElement Delete(string id)
        {
            throw new Exception("29490");
        }

        public override XElement Add(XElement record)
        {
            throw new Exception("29491");
        }

        public override XElement AddUpdate(XElement record)
        {
            throw new Exception("29492");
        }

        public override XElement PutItem(XElement record)
        {
            throw new NotImplementedException();
        }
    }
}
