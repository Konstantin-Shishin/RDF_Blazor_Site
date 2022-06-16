using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Xml.Linq;

namespace ErshCassette
{
    class Program
    {
        /// <summary>
        /// Создание кассеты с материалами архива академика А.П.Ершова
        /// </summary>
        /// <param name="args">
        /// Первый параметр - место откуда будут браться CSV-файлы,
        /// второй параметр - место, куда формируется кассета, собственно кассета
        /// </param>
        static void Main(string[] args)
        {
            Random rnd = new Random();
            Console.WriteLine("Start ErshCassette");
            string dirin = @"C:\Home\data\ErshArchData\";
            string dirout = @"C:\Home\data\ErshArchCassette\";
            if (args.Length == 2)
            {
                dirin = args[0];
                dirout = args[1];
            }

            XElement db = XElement.Parse(
@"<?xml version='1.0' encoding='utf-8'?>
<rdf:RDF xmlns:rdf='http://www.w3.org/1999/02/22-rdf-syntax-ns#' xmlns='http://fogid.net/o/' >
</rdf:RDF>");
            // dbid = "ErshArchCassette" owner = "mag"
            db.Add(new XAttribute("dbid", "ErshArchCassette"),
                new XAttribute("owner", "mag"));

            XName rdfabout = XName.Get("{http://www.w3.org/1999/02/22-rdf-syntax-ns#}about");
            XName rdfresource = XName.Get("{http://www.w3.org/1999/02/22-rdf-syntax-ns#}resource");
            string f = "{http://fogid.net/o/}";
            // Попробую добавить персону
            db.Add(
                new XElement(f+"person", new XAttribute(rdfabout, "p77777"),
                    new XElement(f+"name", "Пупкин Вася Пупсиков"))
                  );

            // Организую чтение из файла lists.csv
            TextReader lists = new StreamReader(File.OpenRead(dirin + "lists.csv"));
            int cnt = 0;
            string line = lists.ReadLine(); // первая строка - заголовки
            string[] parts = line.Split('\t');
            int nparts = parts.Length;
            // Организуем словарь list_id -> list_num, folder_num, url
            Dictionary<int, Tuple<int, string, string>> photki = new Dictionary<int, Tuple<int, string, string>>();
            while (line != null)
            {
                cnt++;
                if (cnt != 1)
                {
                    parts = line.Split('\t');
                    int list_id = Int32.Parse(parts[0]);
                    int list_num = Int32.Parse(parts[1]);
                    string folder_num = parts[2];
                    string url = parts[3];
                    //if (parts.Length != nparts) { Console.WriteLine($"nom: {cnt} nparts: {parts.Length}"); }
                    photki.Add(list_id, new Tuple<int, string, string>(list_num, folder_num, url));
                }
                line = lists.ReadLine();
            }
            Console.WriteLine($"{cnt} lines; {nparts} parts");

            // Организую чтение из файла docs.csv
            TextReader docs = new StreamReader(File.OpenRead(dirin + "docs.csv"));
            cnt = 0;
            line = docs.ReadLine(); // первая строка - заголовки
            parts = line.Split('\t');
            nparts = parts.Length;
            // Организуем словарь doc_id -> doc_name, doc_number, doc_decription, doc_comment, group_id, list_ids
            Dictionary<int, Tuple<string, int, string, string, int, int[]>> documenty = 
                new Dictionary<int, Tuple<string, int, string, string, int, int[]>>();
            while (line != null)
            {
                cnt++;
                while (line[line.Length - 1] == '\\') line = line.Substring(0, line.Length - 1) + docs.ReadLine();
                if (cnt != 1)
                {
                    parts = line.Split('\t');
                    int n = parts.Length;
                    int doc_id = Int32.Parse(parts[0]);
                    string doc_name = parts[1];
                    int doc_number = parts[2] == "\\N" ? -1 : Int32.Parse(parts[2]);
                    string doc_description = n < 4 ? null : parts[3];
                    string doc_comment = n < 5 ? null : parts[4];
                    int group_id = n < 6 || parts[5] == "\\N" ? -1 : Int32.Parse(parts[5]);
                    int[] list_ids = n < 7 || parts[6] =="\\N" ? new int[0] : parts[6].Split(',').Select(s => Int32.Parse(s)).ToArray();
                    //if (parts.Length != nparts) { Console.WriteLine($"lists nom: {cnt} nparts: {parts.Length}"); }
                    documenty.Add(doc_id, new Tuple<string, int, string, string, int, int[]>(
                        doc_name, doc_number, doc_description, doc_comment, group_id, list_ids));
                }
                line = docs.ReadLine();
            }
            Console.WriteLine($"{cnt} lines; {nparts} parts");

            // Организую чтение из файла docs.csv
            TextReader cards = new StreamReader(File.OpenRead(dirin + "card.csv"));
            cnt = 0;
            line = cards.ReadLine(); // первая строка - заголовки
            parts = line.Split('\t');
            nparts = parts.Length;
            // Организуем словарь card_id -> doc_id, doc_types, authors_id, addresse_id, persons_id, keywords, date
            Dictionary<int, Tuple<int, string, int[], int[], int[], string, string>> carty =
                new Dictionary<int, Tuple<int, string, int[], int[], int[], string, string>>();
            while (line != null)
            {
                cnt++;
                while (line[line.Length - 1] == '\\') line = line.Substring(0, line.Length - 1) + cards.ReadLine();
                if (cnt != 1)
                {
                    parts = line.Split('\t');
                    //int n = parts.Length;
                    int card_id = Int32.Parse(parts[0]);
                    int doc_id = parts[1] == "\\N" ? -1 : Int32.Parse(parts[1]);
                    string doc_types = parts[2];
                    int[] authors_id = parts[3] == "\\N" ? new int[0] :
                        parts[3].Split(',').Select(s => Int32.Parse(s)).ToArray();
                    int[] addresse_id = parts[4] == "\\N" ? new int[0] :
                        parts[4].Split(',').Select(s => Int32.Parse(s)).ToArray();
                    int[] persons_id = parts[5] == "\\N" ? new int[0] :
                        parts[5].Split(',').Select(s => Int32.Parse(s)).ToArray();
                    string keywords = parts[6] == "\\N" ? null : parts[6];
                    string date = parts[7] == "\\N" ? null : parts[7];
                    //if (parts.Length != nparts) { Console.WriteLine($"lists nom: {cnt} nparts: {parts.Length}"); }
                    carty.Add(card_id, new Tuple<int, string, int[], int[], int[], string, string>(
                        doc_id, doc_types, authors_id, addresse_id, persons_id, keywords, date));
                }
                line = cards.ReadLine();
            }
            Console.WriteLine($"{cnt} lines; {nparts} parts");




            // Организую чтение из файла docs.csv
            TextReader persons = new StreamReader(File.OpenRead(dirin + "persons.csv"));
            cnt = 0;
            line = persons.ReadLine(); // первая строка - заголовки
            parts = line.Split('\t');
            nparts = parts.Length;
            // Организуем словарь person_id -> name, person_cities
            Dictionary<int, Tuple<string, string>> persony =
                new Dictionary<int, Tuple<string, string>>();
            while (line != null)
            {
                cnt++;
                while (line[line.Length - 1] == '\\') line = line.Substring(0, line.Length - 1) + persons.ReadLine();
                if (cnt != 1)
                {
                    parts = line.Split('\t');
                    //int n = parts.Length;
                    int person_id = Int32.Parse(parts[0]);
                    string person_name = parts[1] == "\\N" ? null : parts[1];
                    string person_surname = parts[2] == "\\N" ? null : parts[2];
                    string person_patronymic = parts[3] == "\\N" ? null : parts[3];

                    string name = person_surname;
                    if (person_name != null)
                    {
                        name = name + " " + person_name;
                        if (person_patronymic != null)
                        {
                            name = name + " " + person_patronymic;
                        }
                    }

                    string person_cities = parts[4] == "\\N" ? null : parts[4];
                    //if (parts.Length != nparts) { Console.WriteLine($"lists nom: {cnt} nparts: {parts.Length}"); }
                    persony.Add(person_id, new Tuple<string, string>(
                        name, person_cities));
                }
                line = persons.ReadLine();
            }
            Console.WriteLine($"{cnt} lines; {nparts} parts");

            // Прочитаю из names.csv и построю таблицу "внешних" имен
            TextReader pnames = new StreamReader(File.OpenRead(dirin + "names.csv"));
            cnt = 0;
            line = pnames.ReadLine(); // первая строка - заголовки
            parts = line.Split('\t');
            nparts = parts.Length;
            // Организуем словарь person_name -> person_idstr
            Dictionary<string, string> ext_names =
                new Dictionary<string, string>();
            while (line != null)
            {
                cnt++;
                if (true || cnt != 1)
                {
                    parts = line.Split('\t');
                    string person_idstr = parts[0];
                    string p_name = parts[1] == "\\N" ? null : parts[1];
                    ext_names.Add(p_name, person_idstr);
                }
                line = pnames.ReadLine();
            }
            Console.WriteLine($"{cnt} lines; {nparts} parts");

            // Проверим через словарь ext_names сколько есть наложений.
            int n_sames = 0;
            foreach (var pe in persony)
            {
                string name = pe.Value.Item1;
                if (ext_names.ContainsKey(name))
                {
                    Console.WriteLine($"== {name}");
                    n_sames++;
                }
            }
            Console.WriteLine($"========== {n_sames} sames");
            // их 228, это заметное число, надо выправлять. 

            // Идея состоит в том, чтобы для идентификаторов персон, испытывающих наложение,
            // ипользовать соответствующие внешние идентификаторы. Внутри мы работаем с 
            // числовым кодом и только в момент формирования RDF, переводим код в слово добавлением
            // буковки e. Надо сформировать словарь cod_ext код -> внешний_ид. Далее, финальная обработка 
            // кода будет заключаться в проверке присутствия ключа в cod_ext и если есть, то использовать 
            // таблицу. Снова проделаем манипуляции с persony и ext_names
            Dictionary<int, string> cod_ext = new Dictionary<int, string>();
            foreach (var pe in persony)
            {
                string name = pe.Value.Item1;
                if (ext_names.ContainsKey(name))
                {
                    cod_ext.Add(pe.Key, ext_names[name]);
                }
            }
            Console.WriteLine($"@@@ {cod_ext.Count} cod_ext entries");

            Func<int, string> ToStr = cod =>
            {
                if (cod_ext.ContainsKey(cod)) return cod_ext[cod];
                else return "e" + cod;
            };


            // Организую чтение из файла group.csv
            TextReader groups = new StreamReader(File.OpenRead(dirin + "group.csv"));
            cnt = 0;
            line = groups.ReadLine(); // первая строка - заголовки
            parts = line.Split('\t');
            nparts = parts.Length;
            // Организуем словарь group_id -> parent_theme, parent_group
            Dictionary<int, Tuple<string, string>> gruppy =
                new Dictionary<int, Tuple<string, string>>();
            
            while (line != null)
            {
                cnt++;
                while (line[line.Length - 1] == '\\') line = line.Substring(0, line.Length - 1) + persons.ReadLine();
                if (cnt != 1)
                {
                    parts = line.Split('\t');
                    //int n = parts.Length;
                    int group_id = Int32.Parse(parts[0]);
                    string parent_theme = parts[1] == "\\N" ? null : parts[1];
                    string parent_group = parts[2] == "\\N" ? null : parts[2];
                    gruppy.Add(group_id, new Tuple<string, string>(
                        parent_theme, parent_group));
                }
                line = groups.ReadLine();
            }
            Console.WriteLine($"{cnt} lines; {nparts} parts");

            // ==== Теперь заполняем базу данных ====

            // Добавим персоны, географию пока не используем
            foreach (var pair in persony)
            {
                int person_id = pair.Key;
                // не определяем персон из внешнего списка
                if (cod_ext.ContainsKey(person_id)) continue;
                string name = pair.Value.Item1;
                db.Add(new XElement(f + "person", new XAttribute(rdfabout, "e" + person_id),
                    new XElement(f + "name", name)));
            }

            // Добавим группы как коллекции и одну группу, как "Архив Ершова"

            string superroot = "Xc_furs_634919475472910156_1017";
            string root = "ershovarchivecollection";

            // сначала сформируем копию общей коллекции "Фонды"
            // <collection rdf:about="Xc_furs_634919475472910156_1017" mT="2013-03-20 06:27:06Z" xmlns="http://fogid.net/o/">
            //  <name xml: lang = "ru" > Фонды </ name >
            // </collection >
            db.Add(new XElement(f + "collection", new XAttribute(rdfabout, "Xc_furs_634919475472910156_1017"),
                new XElement(f + "name", "Фонды")));
            // Еще доавим члество коллекции Ершова в коллекции "Фонды"
            db.Add(new XElement(f + "collection-member", new XAttribute(rdfabout, "ershov_in_funds"),
                new XElement(f + "collection-item", new XAttribute(rdfresource, root)),
                new XElement(f + "in-collection", new XAttribute(rdfresource, superroot))));


            // теперь свое
            db.Add(new XElement(f+"collection", new XAttribute(rdfabout, root),
                new XElement(f+"name", "Архив академика А.П.Ершова")),
                gruppy.SelectMany(pair => new XElement[]
                {
                    new XElement(f+"collection", new XAttribute(rdfabout, "e"+pair.Key),
                        new XElement(f+"name", "Группа " + pair.Key)),
                    new XElement(f+"collection-member", new XAttribute(rdfabout, "e"+pair.Key+"s"+ rnd.Next()),
                        new XElement(f+"collection-item", new XAttribute(rdfresource, "e"+pair.Key)),
                        new XElement(f+"in-collection", new XAttribute(rdfresource, 
                            pair.Value.Item2 == null ? root : "e"+pair.Value.Item2)))
                }),
                null);
            // Берем карточки по очереди, создаем документ из карточки и документа
            foreach (var pair in carty)
            {
                int card_id = pair.Key;
                string date = pair.Value.Item7;
                // authors_id, addresse_id, persons_id
                int[] authors_id = pair.Value.Item3;
                int[] addresse_id = pair.Value.Item4;
                int[] persons_id = pair.Value.Item5;
                int doc_id = pair.Value.Item1;
                if (!documenty.ContainsKey(doc_id)) continue;
                var doc = documenty[doc_id];
                string doc_name = doc.Item1;
                string description = doc.Item3;
                int group_id = doc.Item5;
                int[] photos = doc.Item6.Distinct().ToArray(); // !!! Из-за повторов в данных
                
                db.Add(new XElement(f + "document", new XAttribute(rdfabout, "e" + doc_id),
                        new XElement(f + "name", doc_name),
                        new XElement(f + "from-date", date),
                        new XElement(f + "description", description),
                        null),
                    photos.SelectMany((ph, ind) =>
                    {
                        if (!photki.ContainsKey(ph)) return new XElement[0];
                        var fotka = photki[ph];
                        string url = fotka.Item3;
                        return new XElement[] 
                        { 
                            new XElement(f + "photo-doc", new XAttribute(rdfabout, "e" + ph),
                                new XElement(f + "uri", url),
                                new XElement(f + "name", "noname")),
                            new XElement(f + "DocumentPart", new XAttribute(rdfabout, "e"+doc_id+"_"+ph),
                                new XElement(f + "inDocument", new XAttribute(rdfresource, "e"+ doc_id)),
                                new XElement(f + "partItem", new XAttribute(rdfresource, "e"+ ph)),
                                new XElement(f + "pageNumbers", "" + (ind + 1)),
                                null)
                        };
                    }),
                    // авторы
                    authors_id.Select(id =>
                    {
                        return new XElement(f + "authority", new XAttribute(rdfabout, "e" + doc_id + "-" + id),
                            new XElement(f + "adoc", new XAttribute(rdfresource, "e"+doc_id)),
                            new XElement(f + "author", new XAttribute(rdfresource, ToStr(id))),
                            new XElement(f + "authority-specificator", "author"));
                    }),
                    // получатели
                    addresse_id.Select(id =>
                    {
                        return new XElement(f + "authority", new XAttribute(rdfabout, "e" + doc_id + "-" + id),
                            new XElement(f + "adoc", new XAttribute(rdfresource, "e" + doc_id)),
                            new XElement(f + "author", new XAttribute(rdfresource, ToStr(id))),
                            new XElement(f + "authority-specificator", "recipient"));
                    }),
                    // отраженные
                    persons_id.Select(id =>
                    {
                        return new XElement(f + "reflection", new XAttribute(rdfabout, "e" + doc_id + "_" + id),
                            new XElement(f + "in-doc", new XAttribute(rdfresource, "e" + doc_id)),
                            new XElement(f + "reflected", new XAttribute(rdfresource, ToStr(id))));
                    }),
                    // поместим документ в группу (коллекцию)
                    new XElement(f+"collection-member", new XAttribute(rdfabout, "e"+doc_id+"_"+group_id),
                        new XElement(f+"collection-item", new XAttribute(rdfresource, "e"+doc_id)),
                        new XElement(f + "in-collection", new XAttribute(rdfresource, "e" + group_id))),
                    null);
            }

            db.Save(dirout + "meta/ErshArchCassette_current.fog");
        }
    }
}
