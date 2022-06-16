using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace SoranCore2
{
    public class StaticObjects
    {
        private static string path;
        public static void Init(string pth)
        {
            path = pth;
            OAData.Ontology.Init(path + "ontology_iis-v12-doc_ruen.xml");
            OAData.OADB.Init(path);
            var xel = OAData.OADB.GetItemByIdBasic("newspaper_28156_1997_1", false);
            if (xel != null) Console.WriteLine(xel.ToString());
            //OAData.OADB.Load();
        }

        public static string GetField(XElement rec, string prop)
        {
            if (rec == null) return null;
            //string lang = null;
            string res = null;
            foreach (XElement f in rec.Elements("field"))
            {
                string p = f.Attribute("prop").Value;
                if (p != prop) continue;
                XAttribute xlang = f.Attribute("{http://www.w3.org/XML/1998/namespace}lang");
                res = f.Value;
                if (xlang?.Value == "ru") { break; }
            }
            return res;
        }
        private static string[] months = new[] { "янв", "фев", "мар", "апр", "май", "июн", "июл", "авг", "сен", "окт", "ноя", "дек" };
        public static string DatePrinted(string date)
        {
            if (date == null) return null;
            string[] split = date.Split('-');
            string str = split[0];
            if (split.Length > 1)
            {
                int month;
                if (Int32.TryParse(split[1], out month) && month > 0 && month <= 12)
                {
                    str += months[month - 1];
                    if (split.Length > 2) str += split[2].Substring(0,2);
                }
            }
            return str;
        }
    }

}
