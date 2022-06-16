using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Text;
using System.Xml.Linq;

namespace Turgunda8
{
    public class CassetteUtils
    {
        private static int LoadFogsFromCassettes(string path)
        {
            // Начинаю с конфигуратора
            XElement config = XElement.Load(new FileStream(path + "config.xml", FileMode.Open, FileAccess.Read));
            // Множество загружаемых кассет. Атрибуты понадобятся для регулирования прав на запись
            var cassettesToLoad = config.Elements("LoadCassette").ToArray();
            // Множество фог-документов, находящихся в кассетах

            var fog_flow = cassettesToLoad
                .SelectMany(cass =>
                {
                    string cass_path = cass.Value;
                    string cass_name = cass_path.Split('/', '\\').Last();
                    var fogs_inoriginals = Directory.GetDirectories(cass.Value + "/originals")
                        .SelectMany(d => Directory.GetFiles(d, "*.fog")).ToArray();
                    return Enumerable.Repeat(cass_path + "/meta/" + cass_name + "_current.fog", 1)
                        .Concat(fogs_inoriginals)
                        ;
                }).ToArray();
            int ba_volume = 1024 * 1024;
            int mask = ~((-1) << 20);
            System.Collections.BitArray bitArr = new System.Collections.BitArray(ba_volume);
            // Хеш-функция будет ограничена 20-ю разрядами, массив и функция нужны только временно
            Func<string, int> Hash = s => s.GetHashCode() & mask;
            // Словарь
            Dictionary<string, DateTime> lastDefs = new Dictionary<string, DateTime>();
            int cnt = 0;
            // Первый проход сканирования фог-файлов
            foreach (string fog_name in fog_flow)
            {
                // Чтение фога
                XElement fog = XElement.Load(fog_name);
                // Перебор записей
                foreach (XElement record in fog.Elements())
                {
                    cnt++;
                    if (record.Name == "delete"
                        || record.Name == "{http://fogid.net/o/}delete"
                        || record.Name == "substitute"
                        || record.Name == "{http://fogid.net/o/}substitute"
                        ) continue;
                    string id = record.Attribute("{http://www.w3.org/1999/02/22-rdf-syntax-ns#}about").Value;
                    int code = Hash(id);
                    if (bitArr.Get(code))
                    {
                        // Добавляем пару в словарь
                        XAttribute mT_att = record.Attribute("mT");
                        DateTime mT = DateTime.MinValue;
                        if (mT_att != null) { mT = DateTime.Parse(mT_att.Value); }
                        if (lastDefs.TryGetValue(id, out DateTime last))
                        {
                            if (mT > last)
                            {
                                lastDefs.Remove(id);
                                lastDefs.Add(id, mT);
                            }
                        }
                        else
                        {
                            lastDefs.Add(id, mT);
                        }
                    }
                    else
                    {
                        bitArr.Set(code, true);
                    }
                }
            }

            cnt = 0;

            // Второй проход сканирования фог-файлов
            foreach (string fog_name in fog_flow)
            {
                // Чтение фога
                XElement fog = XElement.Load(fog_name);
                // Перебор записей
                foreach (XElement record in fog.Elements())
                {
                    if (record.Name == "delete"
                        || record.Name == "{http://fogid.net/o/}delete"
                        || record.Name == "substitute"
                        || record.Name == "{http://fogid.net/o/}substitute"
                        ) continue;
                    string id = record.Attribute("{http://www.w3.org/1999/02/22-rdf-syntax-ns#}about").Value;
                    //int code = Hash(id);

                    if (lastDefs.TryGetValue(id, out DateTime saved))
                    {
                        DateTime mT = DateTime.MinValue;
                        XAttribute mT_att = record.Attribute("mT");
                        if (mT_att != null) { mT = DateTime.Parse(mT_att.Value); }
                        // Оригинал если отметка времени больше или равна
                        if (mT >= saved)
                        {
                            // сначала обеспечим приоритет перед другими решениями
                            lastDefs.Remove(id);
                            lastDefs.Add(id, mT);
                            // оригинал!
                            cnt++;
                        }
                    }
                    else
                    {
                        // это единственное определение!
                        cnt++;
                    }
                }
            }

            return cnt;
        }

    }
}
