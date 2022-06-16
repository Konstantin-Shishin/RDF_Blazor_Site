namespace SGraph
{
    using System.Linq;
    using System.Xml.Linq;

     /// <summary>
    /// Обобщение Свойства записи
    /// </summary>
    ///// <typeparam name="T">
    ///// Класс Онтологического представления свойства
    ///// </typeparam>
    public class SProperty 
    {
        /// <summary>
        /// Gets Source.
        /// </summary>
        public SNode Source { get; protected set; } 
        
        /// <summary>
        /// Gets ontology Definition.
        /// </summary>
        public ROntologyPropertyDefinition Definition { get; protected set; }
    }
    
    //public class SProperty<T> : SProperty
    //{
    //   // public XName Definition.Id { get; protected set; }   

    //    
    //}

    /// <summary>
    /// Объектное Свойство записи, указывающее на другую запись
    /// </summary>
    public class SObjectLink : SProperty//<ROntologyObjectPropertyDefinition>
    { 
        /// <summary>
        /// Initializes a new instance of the <see cref="SObjectLink"/> class.
        /// </summary>
        /// <param name="x">
        /// The x.
        /// </param>
        /// <param name="source">
        /// The source.
        /// </param>
        public SObjectLink(XElement x, SNode source)
        {
            // _Definition.Id = x.Name;
            Source = source;
            _targetId = SNode.Coding(x.Attribute(SNames.rdfresource).Value);
            System.DateTime tt1 = System.DateTime.Now;
            if (source._rDataModel is SOntologyModel)
            {
                Definition = ROntologyPropertyDefinition.Abstract(x.Name);
            }
            else
            {
                var def = source._rDataModel.OntologyModel.GetOntologyNode(x.Name.ToString()) as SGraph.ROntologyObjectPropertyDefinition;
                if (def != null) Definition = def;
                else
                {
                    Definition = source.Definition.DirectPropertyDefinitions<ROntologyObjectPropertyDefinition>()
                          .FirstOrDefault(propDef => propDef.Id == x.Name.ToString())
                          ?? source._rDataModel
                          .OntologyModel
                          .InsertNew<ROntologyObjectPropertyDefinition>(x.Name,
                                                                        source.Definition.Id,
                                                                        Target != null
                                                                        ? Target.Definition.Id
                                                                        : XName.Get("entity"),
                                                                        false);
                }
            }
            //Definition = source._rDataModel is SOntologyModel
            //                 ? ROntologyPropertyDefinition.Abstract(x.Name) 
            //                 : source.Definition.DirectPropertyDefinitions<ROntologyObjectPropertyDefinition>()
            //                       .FirstOrDefault(propDef => propDef.Id == x.Name.ToString()) 
            //                       ?? source._rDataModel
            //                       .OntologyModel
            //                       .InsertNew<ROntologyObjectPropertyDefinition>(x.Name,
            //                                                                     source.Definition.Id, 
            //                                                                     Target != null 
            //                                                                     ? Target.Definition.Id
            //                                                                     : "entity",
            //                                                                     false);
        }
        internal SResource _target_resource = null;
        public SNode Target 
        {
            get
            {
                if (_target_resource != null)
                {
                    if (_target_resource.Deleted) return null;
                    if (_target_resource._content != null) return _target_resource._content;
                }
                // Этот вариант на случай, когда ресурс (уже) есть, но нет контента. Надо его запросить
                // При асинхронной реализации, нужно отметить, что айтем запрошен
                return null;
            }
            //set { _target = value; }
        }
        private XName _targetId;
        public XName TargetId { get { return _targetId; } set { _targetId = value; } }
    }

    /// <summary>
    /// Текстовое Свойство записи
    /// </summary>
    public class SDataLink : SProperty//<ROntologyDatatypePropertyDefinition>
    {
        // Этот раздел нужен для случая, когда в поле данных есть другие, кроме xml:lang атрибуты, например, для XML-данных 
        /// <summary>
        /// Gets XOriginal.
        /// </summary>
        public XElement XOriginal { get; private set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="SDataLink"/> class. 
        /// вместо статической "пустышки" делаю конструктор с параметрами null, вдруг пригодиться ещё где-то
        /// </summary>
        /// <param name="x">
        /// </param>
        /// <param name="source">
        /// </param>
        public SDataLink(XElement x, SNode source)
        {
            // _Definition.Id = x.Name; // что делать с именем по умолчанию? XName.Get("") не работает
            Source = source;
            _innerText = x.Value;
            XAttribute langatt = x.Attribute(SNames.xmllang);
            if(langatt!=null) _lang = langatt.Value;
            //TODO:  Следующий оператор должен был что-то означать, но за ним следовал XOriginal = x;
            //if (x.Attributes().Where(a => a.Name != SNames.xmllang).FirstOrDefault() != null) XOriginal = x; 
            if (x.Attributes().Any(att=>att.Name!=SNames.xmllang)) XOriginal = XElement.Parse(x.ToString());
            System.DateTime tt1 = System.DateTime.Now;
            if (source._rDataModel is SOntologyModel)
            {
                Definition = ROntologyPropertyDefinition.AbstractDataProp(x.Name);
            }
            else
            {
                var def = source._rDataModel.OntologyModel.GetOntologyNode(x.Name.ToString()) as SGraph.ROntologyDatatypePropertyDefinition;
                if (def != null) Definition = def;
                else
                {
                    Definition = source.Definition.DirectPropertyDefinitions<ROntologyDatatypePropertyDefinition>()
                                   .FirstOrDefault(propDef => propDef.Id == x.Name.ToString())
                                   ?? source._rDataModel.OntologyModel
                                   .InsertNew<ROntologyDatatypePropertyDefinition>(x.Name, source.Definition.Id, "text", false);
                }
            }
            //Definition = source._rDataModel is SOntologyModel
            //                 ? ROntologyPropertyDefinition.AbstractDataProp(x.Name)
            //                 : source.Definition.DirectPropertyDefinitions<ROntologyDatatypePropertyDefinition>()
            //                       .FirstOrDefault(propDef => propDef.Id == x.Name.ToString())
            //                       ?? source._rDataModel.OntologyModel
            //                       .InsertNew<ROntologyDatatypePropertyDefinition>(x.Name, source.Definition.Id, "text", false);
        }
        private XName _lang;
        public XName Lang { get { return _lang; } }
        private string _innerText;
        public string InnerText { set { _innerText = value; } get { return _innerText; } }
        public override string ToString()
        {
            return InnerText;
        }
    }
}
