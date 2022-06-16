using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;

namespace ConsoleTransformer
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Start ConsoleTransformer.");
            // Создаем базу данных через конфигуратор
            OAData.OADB.directreload = false;
            OAData.OADB.Init("../../../");
            // Директория изучаемой кассеты
            string cass_dir = @"E:\FactographProjects\PA_cassettes\gi_pharchive";
            // Имя кассеты
            string cass_name = "gi_pharchive";
            // Формируемый коррекционный fog-документ
            XElement corrections = XElement.Parse(
@"<?xml version='1.0' encoding='utf-8'?>
<rdf:RDF xmlns:rdf='http://www.w3.org/1999/02/22-rdf-syntax-ns#' xmlns='http://fogid.net/o/' >
</rdf:RDF>");

            // Попробую выделить все документы и найти нужные
            var qu = OAData.OADB.GetAll()
                .Where(xrec => false 
                || xrec.Attribute("type").Value == "http://fogid.net/o/document"
                || xrec.Attribute("type").Value == "http://fogid.net/o/photo-doc"
                || xrec.Attribute("type").Value == "http://fogid.net/o/video-doc"
                || xrec.Attribute("type").Value == "http://fogid.net/o/audio-doc"
                )
                .Where(xrec =>
                {
                    var u = xrec.Elements("field").FirstOrDefault(f => f.Attribute("prop").Value == "http://fogid.net/o/uri");
                    if (u != null && u.Value.StartsWith("iiss://" + cass_name + "@")) return true; //  
                    return false;
                })
                ;
            Console.WriteLine($"Количество документов в базе данных с uri кассеты {cass_name} = {qu.Count()}");
            
            // Получилось "лишних" 15 определений

            // Создаем пару согласованных массивов: найденных uri и элементов, в которых они найдены, сортируем пару по uri
            string[] uris = qu.Select(xrec => xrec.Elements("field")
                .FirstOrDefault(f => f.Attribute("prop").Value == "http://fogid.net/o/uri").Value)
                .ToArray();
            XElement[] xels = qu.ToArray();
            if (uris.Length != xels.Length) throw new Exception("22222");
            Array.Sort(uris, xels);

            // Заводим множество для дублирующих элементов, находим дубли uri
            List<XElement> dbles = new List<XElement>();
            System.Collections.Generic.HashSet<string> hashset = new HashSet<string>();
            List<string> id_list = new List<string>();
            for (int i=0; i<uris.Length; i++)
            {
                var r = uris[i];
                if (hashset.Contains(r))
                {
                    dbles.Add(xels[i]);
                }
                else
                {
                    hashset.Add(r);
                    id_list.Add(xels[i].Attribute("id").Value);
                }
            }
            Console.WriteLine($"Накопленные описания документов: {id_list.Count}");


            // Будет хранить идентификаторы обработанных сущностей
            hashset.Clear();
            // Для каждой сущности из списка будем собирать портрет второго уровня, все записи отправляем в базу данных
            foreach (string id in id_list)
            {
                var xrecs = GenerateXFlow(id, 2, hashset);
                corrections.Add(xrecs);
            }
            Console.WriteLine(corrections.Elements().Count());

            // Сохраняем в файле
            corrections.Save("../../../correction.fog");
        }

        private static XName ToXName(string name)
        {
            int pos = name.LastIndexOf('/');
            string localname = name.Substring(pos + 1);
            string ns_name = name.Substring(0, pos + 1);
            return XName.Get(localname, ns_name);
        }
        private static IEnumerable<XElement> GenerateXFlow(string id, int level, HashSet<string> unique)
        {
            // Проверить было уже описание
            if (unique.Contains(id)) return Enumerable.Empty<XElement>();
            unique.Add(id);

            var rec = OAData.OADB.GetItemByIdBasic(id, level > 1);
            List<XElement> result = new List<XElement>();
            XElement xrec = new XElement(ToXName(rec.Attribute("type")?.Value),
                new XAttribute("{http://www.w3.org/1999/02/22-rdf-syntax-ns#}about", rec.Attribute("id").Value),
                rec.Elements().Select(el =>
                {
                string prop = el.Attribute("prop").Value;
                string variant = el.Name.LocalName;
                if (variant == "field")
                {
                    string lang = el.Attribute("{http://www.w3.org/XML/1998/namespace}lang")?.Value;
                    return new XElement(ToXName(prop),
                        (lang == null ? null : new XAttribute("{http://www.w3.org/XML/1998/namespace}lang", lang)),
                        el.Value,
                        null);
                }
                else if (variant == "direct")
                {
                    string resource = el.Element("record").Attribute("id")?.Value;
                    if (level > 0 && resource != null) result.AddRange(GenerateXFlow(resource, level - 1, unique));
                    return new XElement(ToXName(prop),
                        new XAttribute("{http://www.w3.org/1999/02/22-rdf-syntax-ns#}resource",
                            resource));
                }
                else return null;
            }));
            result.Add(xrec);
            if (level > 1)
            {
                foreach (var r in rec.Elements("inverse"))
                {
                    string source = r.Element("record")?.Attribute("id")?.Value;
                    if (source != null)
                    {
                        result.AddRange(GenerateXFlow(source, level - 1, unique));
                    }
                }
            }
            return result;
        }
    }
}
