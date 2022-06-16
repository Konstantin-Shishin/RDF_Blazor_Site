using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace OADataService
{
    /// <summary>
    /// Класс контролирует (респределенный) репозиторий и базу данных, собранную из фог-файлов
    /// </summary>
    public class StaObject
    {
        // Пусть к конфигуратору и файлам
        private static string path;
        // Репозиторий
        //private static OARepository repository;

        public static void Init(string pth)
        {
            path = pth + ((pth[pth.Length-1]=='/' || pth[pth.Length - 1] == '\\')? "" : "/");
            XElement xconfig = XElement.Load(path + "config.xml");
            OAData.OADB.Init(path);

        }

    }
}
