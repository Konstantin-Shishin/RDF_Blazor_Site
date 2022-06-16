using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml.Linq;
using Polar.DB;
using Polar.Cells;
using Polar.CellIndexes;

namespace Polar.Cassettes.DocumentStorage
{
    public class TRecordStorage : DbAdapter
    {
        private string path;
        private PType tp_record;
        private int fno = 0;
        private TableSimple table;
        //private  
        // =========== Инициирование и построение ===========
        public override void Init(string connectionstring)
        {
            // connectionstring имеет конструкцию: trs:name=value;name2=value2
            // path - директрия для базы данных
            string[] parts = connectionstring.Substring(4).Split('=', ';');
            for (int i=0; i<parts.Length; i += 2)
            {
                if (parts[i * 2].ToLower() == "path")
                {
                    path = parts[i * 2 + 1];
                    if (path[path.Length - 1] != '/' && path[path.Length - 1] != '\\') path = path + "/";
                } 
            }
            tp_record = new PTypeRecord(
                new NamedType("s", new PType(PTypeEnumeration.sstring)),
                new NamedType("p", new PType(PTypeEnumeration.sstring)),
                new NamedType("o",
                    new PTypeUnion(
                        new NamedType("empty", new PType(PTypeEnumeration.none)),
                        new NamedType("uri", new PType(PTypeEnumeration.sstring)),
                        new NamedType("str", new PType(PTypeEnumeration.sstring)),
                        new NamedType("langstr",
                            new PTypeRecord(
                                new NamedType("lang", new PType(PTypeEnumeration.sstring)),
                                new NamedType("str", new PType(PTypeEnumeration.sstring)))))));
            Func<Stream> gen_stream = () => File.Open(path + "f" + (fno++) + ".bin", FileMode.OpenOrCreate);
            table = new TableSimple(tp_record, new int[] { 0 }, gen_stream);

        }
        public override void StartFillDb(Action<string> turlog)
        {
            turlog("StartFillDb");
        }
        public override void FinishFillDb(Action<string> turlog)
        {
            // Все делает table.Fill()
        }
        private Action<string> errors = s => { Console.WriteLine(s); };
        public override void LoadFromCassettesExpress(IEnumerable<string> fogfilearr, Action<string> turlog, Action<string> convertlog)
        {
            InitTableRI();
            foreach (string filename in fogfilearr)
            {
                XElement fog = XElement.Load(filename);
                var xflow = fog.Elements()
                    .Select(el => ConvertXElement(el));
                AppendXflowToRiTable(xflow, filename, errors);
            }
            // Использование таблицы
            foreach (string filename in fogfilearr)
            {
                XElement fog = XElement.Load(filename);
                var xflow = fog.Elements()
                    .Select(el => ConvertXElement(el)).ToArray();
                LoadXFlowUsingRiTable(xflow);
            }
        }

        public override void LoadXFlowUsingRiTable(IEnumerable<XElement> xflow)
        {
            var query = xflow.SelectMany(xe =>
            {
                List<object[]> triples = new List<object[]>();
                string id = xe.Attribute(ONames.rdfabout)?.Value;
                if (id == null) return Enumerable.Empty<object[]>();
                var nm = xe.Name;
                string t = nm.NamespaceName + nm.LocalName;
                triples.Add(new object[] { id, "http://www.w3.org/1999/02/22-rdf-syntax-ns#type", new object[] { 1, t } });
                triples.AddRange(xe.Elements().Select(se => 
                {
                    var nms = se.Name;
                    string pred = nms.NamespaceName + nms.LocalName;
                    var resource = se.Attribute(ONames.rdfresource);
                    object[] obj = resource == null ? new object[] { 2, se.Value } : new object[] { 1, resource.Value };
                    return new object[] { id, pred,  obj};
                }));
                return triples;
            }).ToArray();
            table.Fill(query);
        }

        public override void Save(string filename)
        {
            throw new NotImplementedException();
        }

        // =========== Редактирование ===========
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

        // ========== Доступ ===========
        public override XElement GetItemById(string id, XElement format)
        {
            throw new NotImplementedException();
        }

        public override XElement GetItemByIdBasic(string id, bool addinverse)
        {
            throw new NotImplementedException();
        }

        public override XElement GetItemByIdSpecial(string id)
        {
            throw new NotImplementedException();
        }

        public override IEnumerable<XElement> SearchByName(string searchstring)
        {
            //throw new NotImplementedException();
            return Enumerable.Empty<XElement>();
        }

    }
}
