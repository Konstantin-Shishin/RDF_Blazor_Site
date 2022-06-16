using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RDFEngine
{
    public class RRecord
    {
        public string Id { get; set; }
        public string Tp { get; set; }
        public RProperty[] Props { get; set; }
        //public string Label { get; set; } // поле понадобится для хранения метки
        //public override string ToString()
        //{
        //    var query = Props.Select(p =>
        //    {
        //        string prop = p.Prop;
        //        if (p is RField)      return "f^{<" + prop + ">, \"" + ((RField)p).Value + "\"}";
        //        else if (p is RLink) return "l^{<" + prop + ">, <" + ((RLink)p).Resource + ">}";
        //        // Добавленный вариант обратной ссылки
        //        else if (p is RInverseLink) return "il^{<" + prop + ">, <" + ((RInverseLink)p).Source + ">}";
        //        else if (p is RDirect) return "d^{<" + prop + ">, " + ((RDirect)p).DRec.ToString() + "}";
        //        /*else if (p is RDirect)*/ return "i^{<" + prop + ">, " + ((RInverse)p).IRec.ToString() + "}";
        //    }).Aggregate((a, s) => a + ", " + s);
        //    return "{ <" + Id + ">, <" + Tp + ">, " + "[" +       query      + "]}";
        //}
        public string GetField(string propName)
        {
            return ((RField)this.Props.FirstOrDefault(p => p is RField && p.Prop == propName))?.Value;
        }
        public RRecord GetDirectResource(string propName)
        {
            return ((RDirect)this.Props.FirstOrDefault(p => p is RDirect && p.Prop == propName))?.DRec;
        }
        public string GetName()
        {
            return ((RField)this.Props.FirstOrDefault(p => p is RField && p.Prop == REngine.propName))?.Value;
        }
        public string GetDates()
        {
            string df = GetField("http://fogid.net/o/from-date");
            string dt = GetField("http://fogid.net/o/to-date");
            return (df == null ? "" : df) + (string.IsNullOrEmpty(dt) ? "" : "-" + dt);
        }
    }
    public abstract class RProperty
    {
        public string Prop { get; set; }
    }
    public class RField : RProperty 
    {
        public string Value { get; set; }
        public string Lang { get; set; }
    }
    public class RLink : RProperty, IEquatable<RLink>
    {
        public string Resource { get; set; }

        public bool Equals(RLink other)
        {
            return this.Prop == other.Prop && this.Resource == other.Resource;
        }

        public int GetHashCode([DisallowNull] RLink obj)
        {
            return obj.Prop.GetHashCode() ^ obj.Resource.GetHashCode();
        }
    }

    // Расширение вводится на странице 11 пособия "Делаем фактографию"
    public class RInverseLink : RProperty
    {
        public string Source { get; set; }
    }

    // Новое расширение
    public class RDirect : RProperty
    {
        public RRecord DRec { get; set; }
    }
    public class RInverse : RProperty
    {
        public RRecord IRec { get; set; }
    }

    // Специальное расширение для описателей перечислимых  
    public class RState : RProperty
    {
        public string Key { get; set; }
        public string Value { get; set; }
        public string lang { get; set; }
    }




}
