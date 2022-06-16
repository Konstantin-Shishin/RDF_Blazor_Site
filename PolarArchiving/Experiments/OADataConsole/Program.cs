using System;
using System.Collections.Generic;
using System.Xml.Linq;
using OAData;

namespace OADataConsole
{
    partial class Program
    {
        static void Main(string[] args)
        {
            //Main1(args);
            Main2(args);
        }
        static void Main1(string[] args)
        {
            Console.WriteLine("Start OADataConsole");
            // Работаем в этой директории
            string datadir = @"C:\Home\dev2021\PolarArchiving\OADataConsole\";

            // Сформируем тестовую кассету, поместим ее в директорию
            OAData.OADB.Init(datadir);

            Console.WriteLine("By Id:");
            XElement xrecord = OADB.GetItemByIdBasic("pz001", false);
            //if (xrecord != null) Console.WriteLine(xrecord.ToString());

            XElement nitem = new XElement("{http://fogid.net/o/}person",
                //new XAttribute("{http://www.w3.org/1999/02/22-rdf-syntax-ns#}about", "Cassette_test20180311_mag111_637392365301532170_1001"),
                new XAttribute("owner", "mag111"),
                new XElement("{http://fogid.net/o/}name", "Сидор Уничтожаемый"));
            OADB.PutItem(nitem);

            //OADB.DeleteItem("Cassette_test20180311_mag111_637392365301532170_1002");
            XElement ditem = new XElement("{http://fogid.net/o/}delete",
                new XAttribute("{http://www.w3.org/1999/02/22-rdf-syntax-ns#}about", "Cassette_test20180311_mag111_637392365301532170_1004"),
                new XAttribute("id", "Cassette_test20180311_mag111_637392365301532170_1004"),
                new XAttribute("owner", "mag111"));
            OADB.PutItem(ditem);

            IEnumerable<XElement> seq = OADB.SearchByName("сидор");
            foreach (var x in seq)
            {
                Console.WriteLine(x.ToString());
            }

        }
    }
}
