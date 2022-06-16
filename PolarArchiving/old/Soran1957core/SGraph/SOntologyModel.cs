using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;

namespace SGraph
{
    public class SOntologyModel : SDataModel
    {
        public T InsertNew<T>(XName id, XName sourceONodeId, XName range, bool isAbstract) where T : ROntologyDef
        {
            XElement node;
            if (typeof(T) == typeof(ROntologyClassDefinition))
            {
                node = new XElement("Class");
            }
            else
            {
                if (typeof(T) == typeof(ROntologyObjectPropertyDefinition))
                {
                    node = new XElement("ObjectProperty");
                    node.Add(new XElement("inverse-label", "(inverse " + id + ")"));
                }
                else // if (typeof(T) == typeof(ROntologyDatatypePropertyDefinition))
                {
                    node = new XElement("DatatypeProperty");
                }
                if (!isAbstract)
                    node.Add(new XElement(SNames.TagDomain,
                        new XAttribute(SNames.rdfresource, sourceONodeId)),
                        new XElement(SNames.TagRange, new XAttribute(SNames.rdfresource, range)));
            }
            node.Add(
                new XElement(SNames.TagLabel, "(" + id + ")"),
                new XAttribute(SNames.rdfabout, id),
                isAbstract
                ? new XAttribute("isAbstract", "true")
                : null);
            Include(node);
            return (T)GetItemById(id.ToString());
        }

        /// <summary>
        /// </summary>
        /// <param name="x">
        /// The x.
        /// </param>
        /// <returns>
        /// </returns>
        public override SNode CreateRecord(XElement x)
        {
            SResource resource = PrepareResource(x);
            if (x.Name == SNames.TagClass) return new ROntologyClassDefinition(x, resource, this);
            if (x.Name == SNames.TagObjectproperty) return new ROntologyObjectPropertyDefinition(x, resource, this);
            if (x.Name == SNames.TagDatatypeproperty) return new ROntologyDatatypePropertyDefinition(x, resource, this);
            return new ROntologyDef(x, resource, this);
        }

        //public void CloseModel()
        //{
        //    foreach (KeyValuePair<string, SResource> pair in this.resources)
        //    {
        //        var content = pair.Value._content;
        //        //if (content != null) content.notFull = false;
        //    }
        //}
        public ROntologyDef GetOntologyNode(XName ndName)
        {
            return (ROntologyDef)GetItemById(ndName);
        }
        public T GetOntologyNode<T>(XName ndName) where T : ROntologyDef
        {
            return (T)GetItemById(ndName.ToString());
        }
        //public void RemoveItemFromInstances(XName id)
        //{
        //    Elements("Class").Cast<ROntologyClassDefinition>().Apply(Class => Class.RemoveInstance(id));
        //}
    }

    /// <summary>
    /// </summary>
    public class ROntologyDef : SNode
    {
        public XName RDFType { get; set; }      
      
        //TODO: Эти дела мне нужны для работы с атрибутами элементов онтологии, надо переделать
        public XElement XSource { get; private set; }

        internal ROntologyDef(XElement x, SResource resource, SDataModel rDataModel) : base(x, resource, rDataModel) 
        {
            XSource = x;
            RDFType = x.Name;
        }
        public string Label { get { return TextField(SNames.TagLabel, "ru"); } }
    }
    public class ROntologyClassDefinition : ROntologyDef
    {

        /// <summary>
        /// Initializes a new instance of the <see cref="ROntologyClassDefinition"/> class.
        /// </summary>
        /// <param name="x">
        /// The x.
        /// </param>
        /// <param name="resource">
        /// The resource.
        /// </param>
        /// <param name="rDataModel">
        /// The r data model.
        /// </param>
        internal ROntologyClassDefinition(XElement x, SResource resource, SDataModel rDataModel)
            : base(x, resource, rDataModel){}

        /// <summary>
        /// таблица записей представителей этоо типа
        /// </summary>
        //private Dictionary<XName, SNode> typeSelfInstances = new Dictionary<XName, SNode>();

        ///// <summary>
        ///// Запись этого типа по индентификатору в таблице.
        ///// </summary>
        ///// <param name="id">
        ///// The id.
        ///// </param>
        ///// <returns>
        ///// </returns>
        //public SNode GetInstance(string id)
        //{
        //    return typeSelfInstances.ContainsKey(id) ? typeSelfInstances[id] : null;
        //}

        /// <summary> Выдаёт коллекцию записей представителей типа.
        /// </summary>
        /// <returns>
        /// </returns>
        //public IEnumerable<SNode> GetInstances()
        // {
        //     return typeSelfInstances.Values;
        // }
        
        /// <summary>
        /// Добавление записи в таблицу представителей,
        /// записей этого типа.
        /// </summary>
        /// <param name="node">
        /// The node.   Представитель типа
        /// </param>
        //public void AddInstance(SNode node)
        //{
        //    if (typeSelfInstances.ContainsKey(node.Id))
        //    {
        //        return;// false;
        //    }
        //     typeSelfInstances.Add(node.Id, node);
        //    //return true;
        //}
        //public void RemoveInstance(XName id)
        //{
        //    if(typeSelfInstances.ContainsKey(id)) 
        //        typeSelfInstances.Remove(id);
        //}
        /// <summary>
        /// Gets Superclass.
        /// </summary>
        public ROntologyClassDefinition Superclass
        {
            get
            {
                var v = DirectObjectLinks(SNames.TagSubclassof)
                    .FirstOrDefault(p => p.Target != null);
                return v == null ? null : (ROntologyClassDefinition)v.Target;
            }
        }

        /// <summary>
        /// </summary>
        /// <returns>
        /// </returns>
        public IEnumerable<ROntologyClassDefinition> Descendants()
        {
            foreach (ROntologyClassDefinition node in InverseProperties(SNames.TagSubclassof).Select(p => p.Source))
            {
                yield return node;
                foreach (var nod in node.Descendants())
                    yield return nod;
            }
        }

        /// <summary>
        /// </summary>
        /// <returns>
        /// </returns>
        public IEnumerable<ROntologyClassDefinition> DescendantsOrSelf()
        {
            yield return this;
            foreach (var v in Descendants()) yield return v;
        } 

        /// <summary>
        /// не уверен, что это правильное решение
        /// </summary>
        /// <returns>
        /// </returns>
        public IEnumerable<ROntologyClassDefinition> AncestorsOrSelf()
        {
            yield return this;
            var superclass =
                DirectObjectLinks(SNames.TagSubclassof)
                .Select(p => p.Target)
                .Cast<ROntologyClassDefinition>()
                .FirstOrDefault();
            if (superclass != null)
                foreach (var ancs in superclass.AncestorsOrSelf())
                    yield return ancs;
        }
        public bool IsSystemObject
        {
            get
            {
                if (Id == "entity") return false;
                if (Id == "sys-obj") return true;
                var superclass =
                    (ROntologyClassDefinition)DirectObjectLinks(SNames.TagSubclassof)
                    .Select(p=>p.Target)
                    .FirstOrDefault();
                return superclass != null && superclass.IsSystemObject;
            }
        }

        /// <summary>
        /// Gets a value indicating whether IsUnary.
        /// Проверка на унарное отношение. Предполагается, что класс не системный
        /// </summary>
        public bool IsUnary
        {
            get
            {
                int ndomains = AncestorsOrSelf()
                    .Select(s=>s.InverseProperties(SNames.TagDomain)
                        .Select(p => p.Source)
                        .Where(d=>d is ROntologyObjectPropertyDefinition)
                        .Cast<ROntologyObjectPropertyDefinition>()
                        .Select(d1=>d1.XSource.Attribute("essential"))
                        .Where(d2=>d2!=null && d2.Value=="yes")
                        .Count())
                    .Sum();
                return ndomains == 1;
            }
        }

        /// <summary>
        /// Gets a value indicating whether IsAbstract.
        /// </summary>
        public bool IsAbstract
        {
            get 
            {  
                var att = XSource.Attribute("abstract"); 
                if (att == null) return false;
                return att.Value == "yes";
            } 
        }
        public IOrderedEnumerable<T> DirectPropertyDefinitions<T>(ROntologyObjectPropertyDefinition externProp) where T : ROntologyDef
        {
            return DirectPropertyDefinitions<T>()
                .Where(x => x is ROntologyDatatypePropertyDefinition || (x as ROntologyObjectPropertyDefinition) != externProp)
                .OrderBy(x => x.XSource.Attribute("priority") != null ? x.XSource.Attribute("priority").Value : "zzz");
        }
        public IOrderedEnumerable<T> DirectPropertyDefinitions<T>(string externPropId) where T : ROntologyDef
        {
            return DirectPropertyDefinitions<T>()
                .Where(x => x.Id.ToString() != externPropId)
                .OrderBy(x => x.XSource.Attribute("priority") != null ? x.XSource.Attribute("priority").Value : "zzz");
        }
        public IEnumerable<T> DirectPropertyDefinitions<T>()
        {
            return AncestorsOrSelf()
                .SelectMany(x => x.InverseProperties(SNames.TagDomain))
                .Select(iprop => iprop.Source)
                .Where(directDef =>directDef is T)
                .Cast<T>();
        }

        public IEnumerable<InversePair> InversesDefinitions()
        {         
            return AncestorsOrSelf()
                 .SelectMany(tp => tp.InverseProperties(SNames.TagRange)
                     .Select(p => p.Source) // Это определение обратного свойства ip
                     .Where(ip0 => ((ROntologyPropertyDefinition)ip0).IsEssential
                         && ip0.InverseProperties("includes")
                         .Select(inc => inc.Source as ROntologyDef)
                         .Where(thd => thd.RDFType == "Theme")
                         .Any())
                     .SelectMany(ip => ip.DirectObjectLinks(SNames.TagDomain)
                         .Where(ol => ol.Target != null)
                         .Select(ol2 => ol2.Target as ROntologyClassDefinition)
                         .SelectMany(ol2Target => ol2Target.DescendantsOrSelf() // Это определения обратного типа
                             .Where(tp2 => !tp2.IsSystemObject && !tp2.IsAbstract)
                             .Select(tp4 => 
                                 new InversePair
                                     {
                                         InvProp = (ROntologyObjectPropertyDefinition)ip,
                                         InvType = tp4
                                     }))));
        }  
    }
    public class ROntologyPropertyDefinition : ROntologyDef
    {
        public string PropertyName { get; set; }
        internal ROntologyPropertyDefinition(XElement x, SResource resource, SDataModel rDataModel) : base(x, resource, rDataModel) { }
        public bool IsEssential
        {
            get
            {
                XAttribute att = XSource.Attribute("essential");
                return att == null ? false : att.Value == "yes";
            }
        }
        private static ROntologyObjectPropertyDefinition domain =
          new ROntologyObjectPropertyDefinition(
              new XElement("ObjectProperty",
                  new XAttribute(SNames.rdfabout, "domain")),
              null, null);
        private static ROntologyObjectPropertyDefinition objRange =
        new ROntologyObjectPropertyDefinition(
            new XElement("ObjectProperty",
                new XAttribute(SNames.rdfabout, "range")),
            null, null);
        private static ROntologyObjectPropertyDefinition includes =
                new ROntologyObjectPropertyDefinition(
            new XElement("ObjectProperty",
                new XAttribute(SNames.rdfabout, "includes")),
            null, null);
        private static ROntologyObjectPropertyDefinition SubClassOf =
               new ROntologyObjectPropertyDefinition(
           new XElement("ObjectProperty",
               new XAttribute(SNames.rdfabout, "SubClassOf")),
           null, null);
        public static ROntologyObjectPropertyDefinition Abstract(XName name) 
        {
            if (name == "domain") return domain;
            if (name == "range") return objRange;
            if (name == "includes") return includes;
            if (name == "SubClassOf") return SubClassOf;
            return new ROntologyObjectPropertyDefinition(
            new XElement("ObjectProperty",
                new XAttribute(SNames.rdfabout, name)),
            null, null);
        }
        static ROntologyDatatypePropertyDefinition label =
             new ROntologyDatatypePropertyDefinition(
            new XElement("DatatypeProperty",
                new XAttribute(SNames.rdfabout, "label")),
            null, null);
        static ROntologyDatatypePropertyDefinition inverseLabel =
     new ROntologyDatatypePropertyDefinition(
    new XElement("DatatypeProperty",
        new XAttribute(SNames.rdfabout, "inverse-label")),
    null, null);
        static ROntologyDatatypePropertyDefinition state =
new ROntologyDatatypePropertyDefinition(
new XElement("DatatypeProperty",
new XAttribute(SNames.rdfabout, "state")),
null, null);
        public static ROntologyDatatypePropertyDefinition AbstractDataProp(XName name)
        {
            if (name == "label") return label;
            if (name == "inverse-label") return inverseLabel;
            if (name == "state") return state;
            return new ROntologyDatatypePropertyDefinition(
            new XElement("DatatypeProperty",
                new XAttribute(SNames.rdfabout, name)),
            null, null);
        }
    }
    public class ROntologyObjectPropertyDefinition : ROntologyPropertyDefinition
    { 
         internal ROntologyObjectPropertyDefinition(XElement x, SResource resource, SDataModel rDataModel) : base(x, resource, rDataModel) { }
         public string InverseLabel { get { return TextField("inverse-label", "ru"); } }
    }
    public class ROntologyDatatypePropertyDefinition : ROntologyPropertyDefinition
    {
        internal ROntologyDatatypePropertyDefinition(XElement x, SResource resource, SDataModel rDataModel) : base(x, resource, rDataModel) 
        {
        }
    }
    public class InversePair
    {
        public ROntologyObjectPropertyDefinition InvProp { get; set; }
        public ROntologyClassDefinition InvType { get; set; }
    }
    
}
