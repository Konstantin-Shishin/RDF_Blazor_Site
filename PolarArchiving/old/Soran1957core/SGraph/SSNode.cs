using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;

namespace SGraph
{
    public static  class SSNodeExt
    {       

        public static string Name(this SNode sNode) 
        { 
            return sNode==null ? "no item" : sNode.TextField("name", "ru") ?? "no name"; 
        }
        public static string DateFirst(this SNode sNode)
        {
            var dp = sNode.DataProperty("from-date");
            string df = dp == null ? null : dp.InnerText;
            if (df != null) return df;
            return sNode.InverseProperties("referred")
                .Select((Func<SObjectLink, string>)((SObjectLink p) =>
                {
                    SDataLink dprop = p.Source.DataProperty("some-date");
                    if (dprop != null) return dprop.InnerText;
                    dprop = p.Source.DataProperty("date");
                    if (dprop != null) return dprop.InnerText;
                    return (string)null;
                }))
                .OrderBy(d => d).FirstOrDefault();
        }
        
        public static string DateLast(this SNode sNode)
        {
            return sNode.DataProperty("to-date") != null ? sNode.DataProperty("to-date").InnerText : null;
        } 
        public static string Published(this SNode sNode, string regime)
        {
            var property = sNode.DataProperty("published-" + regime);
            return property == null ? "" : property.InnerText;
        }
        public static string Uri(this SNode sNode)
            {
                //var uriProps = _directProperties.Where(p => p.PropertyName == "uri");
                //if (uriProps.Count() == 0) return "";
                //return (uriProps.Last() as SGraph.SDataLink).InnerText;
                var iisStore = (SDataLink) sNode.DirectProperties().FirstOrDefault(x => x.Definition.Id == "iisstore");
                return iisStore != null && iisStore.XOriginal.Attribute("uri")!=null ? iisStore.XOriginal.Attribute("uri").Value : "";
            }
        

        public static string Documenttype(this SNode sNode)
            {
                var iisstore = sNode.DirectProperties<SDataLink>("iisstore").FirstOrDefault();
                if (iisstore == null) return null;
                var owner = iisstore.XOriginal.Attribute("documenttype");
                if (owner == null) return null;
                return owner.Value;
            }
        public static int PhotoTransform(this SNode sNode)
        {
            var iisstore = sNode.DirectProperties<SDataLink>("iisstore").FirstOrDefault();
            if (iisstore == null) return 0;
            var transform = iisstore.XOriginal.Attribute("transform");
            if (transform == null) return 0;
            return transform.Value.ToCharArray().Where(c => c == 'r').Count() % 4;
        }
        //public static string Owner(this SNode sNode)
        //{
        //    var iisstore = sNode.DirectProperties<SDataLink>("iisstore").FirstOrDefault();
        //    if (iisstore == null) return null;
        //    var dtype = iisstore.XOriginal.Attribute("owner");
        //    if (dtype == null) return null;
        //    return dtype.Value;
        //}

        public static string CurrentUri(this SNode sNode, string size)
        {
            return sNode.Uri() == "" ? null : "?r=" + size + "&u=" + sNode.Uri();
        }
         
        /// <summary>
        /// Для обратной ссылки, источником которой является не системный объект, возвращает все остальные ссылки своего источника
        /// </summary>
        /// <typeparam name="T">
        /// </typeparam>
        /// <param name="link">
        /// </param>
        /// <returns>
        /// </returns>
        public static IEnumerable<T> AllElseProps<T>(this SObjectLink link) where T : SProperty
        {
            return link.Source.DirectProperties<T>()
                .Where(dProp => dProp is SDataLink || (dProp as SObjectLink) != link);
        }

        private static string[] months = new[] { "янв", "фев", "мар", "апр", "май", "июн", "июл", "авг", "сен", "окт", "ноя", "дек" };
        private static char[] dateseparator = new[] { '-' };
        public static string DatePrinted(string date)
        {
            if (date == null) return null;
            string[] split = date.Split(dateseparator);
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
        public static bool OfDocType(this ROntologyClassDefinition rdftype)
        {
            if (rdftype.Id == "document") return true;
            if (rdftype.Id == "photo-doc") return true;
            if (rdftype.Id == "video-doc") return true;
            if (rdftype.Id == "audio-doc") return true;
            if (rdftype.Id == "letter") return true;
            //TODO: if (rdftype == "") return true;
            return false;
        }
        public static bool OfCollectionType(this XName rdftype)
        {
            if (rdftype == "collection") return true;
            if (rdftype == "cassette") return true;
            //TODO: if (rdftype == "") return true;
            return false;
        }

        public static void Apply<T>(this IEnumerable<T> objects, Action<T> action)
        {
            foreach(var o in objects)
                action(o);
        }
        public static IEnumerable<T> ApplyReturn<T>(this IEnumerable<T> objects, Action<T> action)
        {
            foreach (var o in objects)
            {
                action(o);
                yield return o;
            }
            
        }
        public static string OntologyName(this ROntologyDef definition, bool isDirect)
        {
            return isDirect
                       ? definition.Label
                       : ((ROntologyObjectPropertyDefinition)definition).InverseLabel;
        }
       public static SNode TargetOrSource(this SObjectLink link, bool isDirect)
        {
            return isDirect ? link.Target : link.Source;
        }

       public static IEnumerable<ROntologyClassDefinition> TargetTypes(this ROntologyDef propDef) 
       {
           return propDef.DirectObjectLinks("range")
               .SelectMany(link => (link.Target as ROntologyClassDefinition)
                   .DescendantsOrSelf()
                   .Where(type => !type.IsAbstract));
       }
       public static IEnumerable<ROntologyClassDefinition> SourceTypes(this ROntologyDef propDef)
       {
           return propDef.DirectObjectLinks("domain")
               .SelectMany(link => (link.Target as ROntologyClassDefinition).DescendantsOrSelf())
               .Where(type => !type.IsAbstract);
       }
        public static T Previous<T>(this IEnumerable<T> query, T center)
        {
            return query.LastOrDefault(element => !element.Equals(center));
        }
        public static T Next<T>(this IEnumerable<T> query, T center)
        {
            return query.Reverse().LastOrDefault(element => !element.Equals(center));
        }

        /// <summary>
        /// </summary>
        /// <param name="func">
        /// Полезный кэш, используется в конце и расспространяется на всё linq выражение. 
        /// </param>
        /// <typeparam name="Tin">
        /// </typeparam>
        /// <typeparam name="Tout">
        /// </typeparam>
        /// <returns>
        /// </returns>
        public static Func<Tin, Tout> Cache<Tin, Tout>(this Func<Tin, Tout> func)
        {
            var store = new Dictionary<Tin, Tout>();
            return parametr =>
                       {
                           Tout found;
                           if (!store.TryGetValue(parametr, out found))
                           {
                               found = func(parametr);
                               store.Add(parametr, found);
                           }
                           return found;
                       };
        }

        /// <summary>
        /// Ещё один кэш значений функций. Испозьуется везде.
        /// </summary>
        /// <param name="func">
        /// The func.
        /// </param>
        /// <param name="parametr">
        /// The parametr.
        /// </param>
        /// <typeparam name="Tin">
        /// </typeparam>
        /// <typeparam name="Tout">
        /// </typeparam>
        /// <returns>
        /// </returns>
        public static Tout Cache<Tin, Tout>(this Func<Tin, Tout> func, Tin parametr)
        {
            object found;
            if (!cache.TryGetValue(parametr, out found))
            { 
                found = func(parametr); 
                cache.Add(parametr, found);
            }
            return (Tout)found;

        }
        private static Dictionary<object, object> cache = new Dictionary<object, object>();
    }

    //public class RSDataModel : SGraph.SDataModel
    //{
    //    //public RSDataModel(IRDataSource data_source) : base(data_source) {  }
    //    public override SGraph.SNode CreateRecord(System.Xml.Linq.XElement x)
    //    {
    //        SResource resource = PrepareResource(x);
    //        return new SSNode(x, resource, this);
    //    }
    //}
}
