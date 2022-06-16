using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace OADataConsole
{
    partial class Program
    {
        static void Main2(string[] args)
        {
            Console.WriteLine("Start Main2: creating persons name-id list");
            
            // Директория, в которой конфигуратор
            string datadir = @"C:\Home\dev2021\PolarArchiving\OADataConsole\";
            //                 C:\Home\dev2021\PolarArchiving\OADataConsole

            // Загружаем данные согласно конфигуратору
            OAData.OADB.Init(datadir);

            var query = OAData.OADB.GetAll();
            foreach (var rec in query.Take(20))
            {
                Console.WriteLine(rec.ToString());
            }
            Console.WriteLine(query.Count());
            Console.WriteLine(query.Where(x => x.Name.LocalName == "person").Count());

            Console.WriteLine("By Id:");

            FileStream fs = new FileStream(datadir + "names.csv", FileMode.OpenOrCreate, FileAccess.Write);
            TextWriter tw = new StreamWriter(fs);

            Dictionary<string, string> dic = new Dictionary<string, string>();
            Dictionary<string, Tuple<string, int>> anti_dic = new Dictionary<string, Tuple<string, int>>();
            int cnt = 0;
            foreach (var person in query.Where(x => x.Name.LocalName == "person"))
            {
                string id = person.Attribute("{http://www.w3.org/1999/02/22-rdf-syntax-ns#}about").Value;
                string name = null;
                foreach(var field in person.Elements("{http://fogid.net/o/}name"))
                {
                    if (name == null) { name = field.Value; continue; }
                    string na = field.Value.ToLower();
                    char ch = na[0];
                    if (ch >= 'а' && ch <= 'я') { name = field.Value; break; }
                }
                if (name == null) { Console.WriteLine($"no name for {id}"); continue; }
                
                //dic.Add(id, name);
                int n_arcs = OAData.OADB.GetItemByIdBasic(id, true).Elements().Count();
                if (anti_dic.ContainsKey(name))
                {
                    cnt++;
                    var t = anti_dic[name];
                    int na = t.Item2;
                    if (n_arcs > na)
                    {
                        anti_dic.Remove(name);
                        anti_dic.Add(name, new Tuple<string, int>(id, n_arcs));
                    }
                }
                else anti_dic.Add(name, new Tuple<string, int>(id, n_arcs));
            }
            foreach (var y in anti_dic)
            {
                tw.WriteLine($"{y.Value.Item1}\t{y.Key}");
            }
            
            Console.WriteLine($"total names: {anti_dic.Count()} same: {cnt}");

        }
    }
}
