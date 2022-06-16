using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Linq;

namespace Polar.Cassettes
{
    public delegate void LogLine(string message);
    public class CassettesConnection : Polar.Cassettes.DocumentStorage.CC
    {
        //public Dictionary<string, Cassettes.CassetteInfo> cassettesInfo = new Dictionary<string, Cassettes.CassetteInfo>();
        public Dictionary<string, Cassettes.RDFDocumentInfo> docsInfo = new Dictionary<string, Cassettes.RDFDocumentInfo>();
        public override void ConnectToCassettes(IEnumerable<XElement> LoadCassette_elements, LogLine protocol)
        {
            cassettesInfo = new Dictionary<string, Cassettes.CassetteInfo>();
            docsInfo = new Dictionary<string, Cassettes.RDFDocumentInfo>();

            foreach (XElement lc in LoadCassette_elements)
            {
                //bool loaddata = false;
                //if (lc.Attribute("write")?.Value == "write") loaddata = true;
                bool loaddata = true;
                bool iseditable = false;
                if (lc.Attribute("regime")?.Value == "nodata") loaddata = false;
                if (lc.Attribute("write")?.Value == "yes") iseditable = true;
                string cassettePath = lc.Value;

                Cassettes.CassetteInfo ci = null;
                try
                {
                    ci = Cassettes.Cassette.LoadCassette(cassettePath, loaddata, iseditable);
                }
                catch (Exception ex)
                {
                    protocol("Ошибка при загрузке кассеты [" + cassettePath + "]: " + ex.Message);
                }
                if (ci == null || cassettesInfo.ContainsKey(ci.fullName.ToSpecialCase())) continue;
                ci.loaddata = loaddata;
                ci.iseditable = iseditable;

                cassettesInfo.Add(ci.fullName.ToSpecialCase(), ci);
                if (ci.docsInfo != null) foreach (var docInfo in ci.docsInfo)
                    {
                        docsInfo.Add(docInfo.dbId, docInfo);
                        if (loaddata)
                        {
                            try
                            {
                                docInfo.isEditable = (lc.Attribute("write") != null && docInfo.GetRoot().Attribute("counter") != null);
                                //sDataModel.LoadRDF(docInfo.Root);
                                //if (!docInfo.isEditable) docInfo.root = null; //Иногда это действие нужно закомментаривать...
                            }
                            catch (Exception ex)
                            {
                                protocol("error in document " + docInfo.uri + "\n" + ex.Message);
                            }
                        }
                    }
            }
        }
        public IEnumerable<Cassettes.RDFDocumentInfo> GetFogFiles()
        {
            foreach (var cpair in cassettesInfo)
            {
                var ci = cpair.Value;
                var toload = ci.loaddata;
                Cassettes.RDFDocumentInfo di0 = new Cassettes.RDFDocumentInfo(ci.cassette, true);
                yield return di0;
                var qu = di0.GetRoot()
                    .Elements()
                    .Where(e =>
                    {
                        if (e.Name == "document")
                        {
                            if (e.Element("iisstore")?.Attribute("documenttype")?.Value == "application/fog") return true;
                        }
                        else if (e.Name == "{http://fogid.net/o/}document")
                        {
                            string dmi = e.Element("{http://fogid.net/o/}docmetainfo")?.Value;
                            if (dmi != null && dmi.Contains("documenttype:application/fog;")) return true;
                        }
                        return false; 
                    });
                foreach (var docnode in qu)
                {
                    var di = new Cassettes.RDFDocumentInfo(docnode, ci.cassette.Dir.FullName, toload);
                    //if (toload) di.ClearRoot();
                    yield return di;
                }
                //di0.ClearRoot(); // Возможно, нехороший побочный эффект
            }
        }
        public override IEnumerable<Cassettes.RDFDocumentInfo> GetFogFiles1()
        {
            // loaddata или toload - признак того, что нужно загружать внутренние fog-документы, если они есть 
            // isEditable или write - признак того, что позволено изменять элементы кассеты (файлы, фог-документы) 
            List<Cassettes.RDFDocumentInfo> docs = new List<RDFDocumentInfo>();
            foreach (var cpair in cassettesInfo)
            {
                var ci = cpair.Value;
                var toload = ci.loaddata;
                Cassettes.RDFDocumentInfo di0 = new Cassettes.RDFDocumentInfo(ci.cassette, ci.iseditable);
                docs.Add(di0);
                //if (!toload) continue;
                var qu = di0.GetRoot()
                    .Elements()
                    .Where(e =>
                    {
                        if (e.Name == "document")
                        {
                            if (e.Element("iisstore")?.Attribute("documenttype")?.Value == "application/fog") return true;
                        }
                        else if (e.Name == "{http://fogid.net/o/}document")
                        {
                            string dmi = e.Element("{http://fogid.net/o/}docmetainfo")?.Value;
                            if (dmi != null && dmi.Contains("documenttype:application/fog;")) return true;
                        }
                        return false;
                    });
                foreach (var docnode in qu) docs.Add(new Cassettes.RDFDocumentInfo(docnode, ci.cassette.Dir.FullName, ci.iseditable));
            }
            return docs;
        }
    }
}
