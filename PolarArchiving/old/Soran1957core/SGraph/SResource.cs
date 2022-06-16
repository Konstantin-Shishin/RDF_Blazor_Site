using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Linq;

namespace SGraph
{
    public class SResource
    {
        internal SResource(XName id)
        {
            _inverseProperties = new List<SObjectLink>();
            _mergedIds = new List<XName>(); _mergedIds.Add(id);
            _lastId = id;
        }
        internal List<SObjectLink> _inverseProperties; // список объектных ссылок, указывающих на данный ресурс
        internal List<XName> _mergedIds; // список "слитых" идентификаторов
        internal XName _lastId; // Последний по цепочке substitute идентификатор
        internal SNode _content = null;
        internal bool Deleted = false;
        /// <summary>
        /// Метод специальной работы с ресурсом, он убирает признак Deleted. Для реального восстановления записи 
        /// нужны внешние действия с редактированием RDF-документов
        /// 
        /// </summary>
        public void UndeleteForSession() { Deleted = true; }
        /// <summary>
        /// Метод для специального использования. Выдает цепочку слитых на ресурсе идентификаторов
        /// </summary>
        public List<XName> MergedIds { get { return _mergedIds; } }
        //internal bool Asked = false;
        //internal bool AskedInverse = false;
    }
}
