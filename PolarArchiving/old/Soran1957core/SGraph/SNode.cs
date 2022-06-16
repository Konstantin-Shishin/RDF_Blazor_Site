namespace SGraph
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Xml.Linq;

    /// <summary>
    /// Запись
    /// </summary>
    public class SNode
    {
        //...........................
        // Публичные методы и доступы
        //...........................
       // public XName RDFType { get; private set; }
        private readonly XName _id;
        public XName Id { get { return _id; } }
        public IEnumerable<SProperty> DirectProperties() { return _directProperties; }
        public ROntologyClassDefinition Definition { get; set; }

        public SDataLink DataProperty(XName propName)
        {
            return DirectProperties<SDataLink>(propName).FirstOrDefault();
        }
        public SDataLink DataProperty(XName propName, XName lang)
        {
            SDataLink result = null;
            SDataLink somelangresult = null;
            foreach (var dprop in DirectProperties<SDataLink>(propName))
            {
                if (dprop.Lang == lang)
                {
                    result = dprop;
                    break;
                }
                if (dprop.Lang == "en")
                {
                    result = dprop;
                }
                else somelangresult = dprop;
            }
            if (result == null && somelangresult != null) result = somelangresult;
            return result;
        }
        public string DataLink(ROntologyDatatypePropertyDefinition prop)
        {
            SDataLink result = null;
            SDataLink somelangresult = null;
            foreach (var dprop in DirectProperties<SDataLink>().Where(p=>p.Definition==prop))
            {
                if (dprop.Lang == "ru")
                {
                    result = dprop;
                    break;
                }
                if (dprop.Lang == "en")
                {
                    result = dprop;
                }
                else somelangresult = dprop;
            }
            if (result == null && somelangresult != null) result = somelangresult;
            return result==null ? null : result.InnerText;
        }
        public IEnumerable<SNode> ObjectLinksTarget(ROntologyObjectPropertyDefinition def)
        {
            return _directProperties.Where(prop => prop.Definition == def && (prop as SObjectLink).Target != null).Select(prop => (prop as SObjectLink).Target)
                .Union(InverseProperties().Where(prop => prop.Definition == def).Select(prop=>prop.Target));
        }

        public  IEnumerable<T> DirectProperties<T>() where T : SProperty
        {
            return DirectProperties().Where(link => link is T).Cast<T>();
        }
        public  IEnumerable<T> DirectProperties<T>(XName type) where T : SProperty
        {
            return DirectProperties<T>().Where(link => link.Definition.Id == type);
        }
        public string DataField(XName propName) { var p = DataProperty(propName); return p == null ? null : p.InnerText; }
        //public string DataField(XName propName, string lang) { var p = DataProperty(propName, lang); return p == null ? null : p.InnerText; }
        public string TextField(XName propName, string lang) { var p = DataProperty(propName, lang); return p == null ? null : p.InnerText; }
        public IEnumerable<SObjectLink> DirectObjectLinks()
        {
            return DirectProperties<SObjectLink>();
            //_directProperties.Where(dprop => typeof(SObjectLink) == dprop.GetType()).Select(dprop => dprop as SObjectLink);
        }
        public IEnumerable<SObjectLink> DirectObjectLinks(XName name)
        {
            return DirectProperties<SObjectLink>(name);
            //DirectObjectLinks().Where(dolink => dolink.Definition.Id == name);
        }
        // Работа с обратными свойствами
        public IEnumerable<SObjectLink> InverseProperties()
        {
            //if (notFull) { notFull = false; if (!_resource.AskedInverse) { _resource.AskedInverse = true; _rDataModel._data_source.LoadInverseItems(_id); } }
            return _resource._inverseProperties;
        }
        public IEnumerable<SObjectLink> InverseProperties(XName nameProp)
        {
            return _resource._inverseProperties==null ? new List<SObjectLink>() : InverseProperties().Where(iprop => iprop.Definition.Id == nameProp);
        }

        // Конструктор
        /// <summary>
        /// Initializes a new instance of the <see cref="SNode"/> class.
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
        internal protected SNode(XElement x, SResource resource, SDataModel rDataModel)
        {
            _rDataModel = rDataModel;
            _resource = resource;
            
            _id = Coding(x.Attribute(SNames.rdfabout).Value);
           // RDFType = x.Name;
            //notFull = true;
           // _directProperties = new List<SProperty>();
            if (_rDataModel == null) return;
            Definition = _rDataModel is SOntologyModel
                ? new ROntologyClassDefinition(
                    new XElement("Class", 
                        new XAttribute(SNames.rdfabout, x.Name)), null, null)
                : _rDataModel.OntologyModel.GetOntologyNode<ROntologyClassDefinition>(x.Name.ToString())
                    ?? _rDataModel.OntologyModel.InsertNew<ROntologyClassDefinition>(x.Name, null, null, false);
           // if (!(_rDataModel is SOntologyModel)) Definition.AddInstance(this);
            foreach (var prop in x.Elements())
                if (prop.Attribute(SNames.rdfresource) == null)
                        _directProperties.Add(new SDataLink(prop, this));
                    else _directProperties.Add(new SObjectLink(prop, this));
        }
        internal SDataModel _rDataModel;
        internal SResource _resource;
        internal DateTime modificationTime = DateTime.MinValue;

        private List<SProperty> _directProperties = new List<SProperty>();
        //public IEnumerable<RObjectLink> EmptyTargetLinks { get { return DirectObjectLinks().Where(dprop => dprop._target_resource == null); } }

        //internal void RemoveInverseLink(SObjectLink property)
        //{
        //    _resource._inverseProperties.Remove(property);
        //}
        //internal void RemoveDirectLink(SProperty property)
        //{
        //    _directProperties.Remove(property);
        //}
        //internal void AddInverseLink(SObjectLink item)
        //{
        //    _resource._inverseProperties.Add(item);
        //}

        //internal bool notFull = true;
        //internal bool Asked = false;
        //internal bool AskedInverse = false;

        //public IEnumerable<String> TextFields()
        //{
        //    return _directProperties.Where(p => p.GetType() == typeof(RDataLink) && p.Definition.Id != "name").Select(p => (p as RDataLink).InnerText);
        //}
        //public IEnumerable<String> DataFields(XName nameProp)
        //{
        //    return _directProperties.Where(p => p.GetType() == typeof(SDataLink) && p.Definition.Id == nameProp).Select(p => (p as SDataLink).InnerText);
        //}
        //public IEnumerable<String> DataFields()
        //{
        //    return _directProperties.Where(p => p.GetType() == typeof(SDataLink)).Select(p => (p as SDataLink).InnerText);
        //}
        //public string TextField(XName fieldName, XName lang)
        //{
        //    string result = null;
        //    string somelangresult = null;
        //    foreach (SDataLink dprop in DirectProperties().Where(p => p.GetType() == typeof(SDataLink) && p.Definition.Id == fieldName).Cast<SDataLink>())
        //    {
        //        if (dprop.Lang == lang) { result = dprop.InnerText; break; }
        //        if (dprop.Lang == "en") { result = dprop.InnerText; }
        //        else somelangresult = dprop.InnerText;
        //    }
        //    if (result == null && somelangresult != null) result = somelangresult;
        //    return result;
        //}
        //TODO: надо переделать и изменить интерфейс, поскольку реализован весьма частный случай 
        //public void SetTextField(string name, string value)
        //{
        //    (_directProperties.Where(rprop => rprop.Definition.Id == name).FirstOrDefault() as SDataLink).InnerText = value;

        //}

        public SProperty DirectProperty(XName prop_id)
        {
            return _directProperties.FirstOrDefault(dp => dp.Definition.Id == prop_id);
        }
        public bool CanDelete()
        {
            return InverseProperties().Count() == 0;
        }

        public IEnumerable<SProperty> DirectProperties(string prop_id)
        {
            return _directProperties.Where(dp => dp.Definition.Label == prop_id);
        }
        /// <summary>
        /// Метод для тонкой отладки данных и программ. Выдает список объединенных через substitute идентификаторов 
        /// </summary>
        /// <returns></returns>
        public IEnumerable<XName> AllMergedIds()
        {
            return _resource._mergedIds;
        }

        public static XName Coding(string origin)
        {
            return XName.Get(origin.Replace("/", "").Replace("|", "").Replace(@"\", "").Replace(" ", "_").Replace(":", "").Replace("@", "")
                .Replace("'", "").Replace("\"", "").Replace("?", "").Replace(",", ""));
        }
    }
}
