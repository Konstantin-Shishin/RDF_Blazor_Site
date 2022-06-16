using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Xml.Linq;
using RDFEngine;

namespace MagBlazor
{
    public class Infobase
    {
        // Движок и онтология
        public static RDFEngine.IEngine engine = null; //TODO: Надо его убрать!!! 
        public static RDFEngine.ROntology rontology = null;
        
        // Параметры приложения
        //public static bool toedit = false;

        // Инициирование приложения, включая чтение и активирование онтологии, пристоединение к базе данных
        private static string path;
        public static void Init(string path)
        {
            //Infobase.engine = new RDFEngine.REngine();

            OAData.OADB.Init(path);
            var xel = OAData.OADB.GetItemByIdBasic("newspaper_28156_1997_1", false);
            var xels = OAData.OADB.SearchByName("марчук").ToArray();
            if (xel != null) Console.WriteLine(xel.ToString());

            Infobase.engine = new RDFEngine.RXEngine(); // Это новый движок!!!

            Infobase.rontology = new RDFEngine.ROntology(path + "ontology_iis-v13.xml");
        }

        //public static void Init0(string pth)
        //{
        //    path = pth;

        //    //OAData.Ontology.Init(path + "ontology_iis-v12-doc_ruen.xml"); // старая онтология

        //    // Чтение онтологии
        //    //rontology = new RDFEngine.ROntology(); // Это тестовая онтология 
        //    rontology = new ROntology(path + "ontology_iis-v12-doc_ruen.xml");
            






        //    OAData.OADB.Init(path);
        //    var xel = OAData.OADB.GetItemByIdBasic("newspaper_28156_1997_1", false);
        //    var xels = OAData.OADB.SearchByName("марчук").ToArray();
        //    if (xel != null) Console.WriteLine(xel.ToString());
        //    //OAData.OADB.Load();

        //    RDFEngine.IEngine engine1 = new RDFEngine.REngine();
        //    ((RDFEngine.REngine)engine1).Load();
        //    engine1.Build();

        //    RDFEngine.IEngine engine2 = new RDFEngine.RXEngine();


        //    //engine = engine1;


        //}

        // Утилита получения имени по записи или null если имени нет   
        public static string GetName(RDFEngine.RRecord rec) =>
    ((RDFEngine.RField)rec.Props.FirstOrDefault(p => p is RDFEngine.RField &&
        (p.Prop == "name" || p.Prop == "http://fogid.net/o/name")))?.Value;

    }
}
