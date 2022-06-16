using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Xml.Linq;
using BlazorServer.Shared;
using RDFEngine;

namespace BlazorServer
{
    public class Infobase
    {
        // Движок базы данных
        public static RDFEngine.IEngine engine = null;
        // ROntology - объектная онтология
        public static ROntology ront = null;

        public static string cassPath;

        public static void Init(string path)
        {

            OAData.OADB.Init(path);
            //var xel = OAData.OADB.GetItemByIdBasic("newspaper_28156_1997_1", false);
            //var xels = OAData.OADB.SearchByName("марчук").ToArray();
           // if (xel != null) Console.WriteLine(xel.ToString());
            //OAData.OADB.Load();

            Infobase.engine = new RXEngine(); // Это новый движок!!!

            //user here
            ((RXEngine)Infobase.engine).User = "mag_1";

            //Infobase.engine.NewRecord("http://fogid.net/o/person", "Пупкин"); // Опробовал 1
            //string idd = OAData.OADB.SearchByName("пупкин").FirstOrDefault()?.Attribute("id")?.Value;

            //Infobase.engine.Build();
            Infobase.ront = new ROntology(path + "ontology_iis-v12-doc_ruen.xml"); // тестовая онтология


        }
        public static string GetTerm(string id, string lang, bool inverse = false)
        {
            string targetProp = "Label";
            if (inverse)
            {
                targetProp = "InvLabel";
            }
            var record = ront.rontology.FirstOrDefault(rec => rec.Id == id);

            if (record == null) return id;
            
            var labels = record?.Props.Where(p => p.Prop == targetProp);

            if (labels == null || ! labels.Any()) return id;

            var result = ((RField)labels.FirstOrDefault(label => ((RField)label).Lang == lang))?.Value;

            if (result == null)
            {
                result = ((RField)labels.FirstOrDefault(label => ((RField)label).Lang == MainLayout.fallbackLanguage))?.Value;
            }
            return result == null ? ((RField)labels.FirstOrDefault())?.Value : result;
        }

    }
}
