using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Linq;

namespace SGraph
{
    public static class SNames
    {
        public static string rdfnsstring = "http://www.w3.org/1999/02/22-rdf-syntax-ns#";
        public static XNamespace rdfns = "http://www.w3.org/1999/02/22-rdf-syntax-ns#";
        //public static XNamespace rdfns = XNamespace.Get(rdfnsstring);
        public static XName xmllang = "{http://www.w3.org/XML/1998/namespace}lang";
        public static XName rdfabout = "{http://www.w3.org/1999/02/22-rdf-syntax-ns#}about";
        //public static XName rdfabout = XName.Get("about", rdfnsstring);
        public static XName rdfresource = "{http://www.w3.org/1999/02/22-rdf-syntax-ns#}resource";
        //public static XName rdfresource = XName.Get("resource", rdfnsstring);
        public static XName rdfRDF = XName.Get("RDF", rdfnsstring);
        static public XName AttRdf = XName.Get("rdf", XNamespace.Xmlns.NamespaceName);

        // Онтологические имена
        public static XName TagClass = "Class";
        public static XName TagDatatypeproperty = "DatatypeProperty";
        public static XName TagObjectproperty = "ObjectProperty";
        public static XName TagLabel = "label";
        public static XName TagSubclassof = "SubClassOf";
        public static XName TagDomain = "domain";
        public static XName TagRange = "range";
        //public static XName TagModificationTime = "modificationTime";

        // Внутренние для SGraph теги и аттрибуты
        public static XName TagDelete = "delete";
        public static XName TagSubstitute = "substitute";
        public static XName AttItem_id = "id";
        public static XName AttOld_id = "old-id";
        public static XName AttNew_id = "new-id";
        public static XName AttModificationTime = "mT";
        public static XName AttUsersWorkRDFDocumentId = "UD";

        /// <summary> 
        /// modificationTime=DateTime.Now.ToString("s")
        /// </summary>
        /// <returns>
        /// </returns>
        public static XAttribute TimeModificationAttribute()
        {
            return new XAttribute(AttModificationTime, DateTime.Now.ToString("s"));
        }
        public static DateTime TimeModification(XElement x) 
        {
            DateTime time=new DateTime(1);
            if(x.Attribute(AttModificationTime)==null)
                return time;
             var timeString = x.Attribute(AttModificationTime).Value;
             DateTime.TryParse(timeString, out time);
            return time;
        }
        ///// <summary>
        ///// UD='userDocumentID'
        ///// </summary>
        ///// <param name="UDID"></param>
        ///// <returns></returns>
        //public static XAttribute UserDocument(string UDID)
        //{
        //    return new XAttribute(AttUsersWorkRDFDocumentId, UDID);
        //}
        ///// <summary>
        ///// Xml шаблон нового элемента (записи или оператора) с аттрибутами Id пользовательского документа и временем создания        
        ///// </summary>
        ///// <param name="type">тип элемента</param>
        ///// <param name="UDID">Id  пользовательского документа, куда будет сохранена запись</param>
        ///// <param name="content">вложенные атрибуты и эелементы</param>
        ///// <returns></returns>
        //public static XElement NewElement(XName type, string UDID, params object[] content)
        //{
        //    return new XElement(type, UserDocument(UDID), TimeModificationAttribute(), content);
        //}
    }
}
