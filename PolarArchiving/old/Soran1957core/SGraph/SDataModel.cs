using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Xml.Linq;

namespace SGraph
{
    public class SDataModel
    {
        //........................
        // Методы доступа к модели
        //........................

        /// <summary>
        /// Можно сказать, главный метод модели
        /// </summary>
        /// <param name="item_id">
        /// The item_id.
        /// </param>
        /// <returns>
        /// </returns>
        public SNode GetItemById(XName item_id)
        {
            SResource resource;
            if (resources.ContainsKey(item_id))
            {
                resource = resources[item_id];
                // отдельная обработка случая, когда айтем уничтожен
                if (resource.Deleted) return null;
            }
            else
            {
                // непонятная идея, лучше, чтобы побочного эффекта не было
                //resource = new SResource();
                //resources.Add(item_id, resource);
                return null;
            }
            return resource._content;
        }
        public SResource GetResourceById(XName id)
        {
            if (resources.ContainsKey(id)) return resources[id];
            return null;

        }
 
        /// <summary>
        ///  Итератор по всем элементам указанного типа. Корректно работает только для полных моделей
        /// </summary>
        /// <param name="type">
        /// The type.
        /// </param>
        /// <returns>
        /// </returns>
        public IEnumerable<SNode> Elements(XName type)
        {
          //  return Elements().Where(node => node.Definition.Id == type);
            //if (type == null || OntologyModel == null) return Elements().Where(node => node.Definition.Id == type);
            //var typeDef = OntologyModel.GetOntologyNode<ROntologyClassDefinition>(type);
            //if (typeDef == null) return Elements().Where(node => node.Definition.Id == type);
            return //typeDef.GetInstances() ??
                Elements().Where(node => node.Definition.Id == type);
        }

        /// <summary>
        /// Итератор по всем элементам указанного типа. Корректно работает только для полных моделей
        /// </summary>
        /// <returns>
        /// </returns>
        public IEnumerable<SNode> Elements()
        {
            return resources
                .Where(pair => pair.Key != null && pair.Value!=null && pair.Value._content != null && XName.ReferenceEquals(pair.Value._content.Id, pair.Key))                
                .Select(pair => pair.Value._content);
        }
        /// <summary>
        /// для поиска тонких ошибок
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public IEnumerable<XName> Resources(XName id)
        {
            if (!resources.ContainsKey(id)) return new List<XName>();
            return resources[id]._mergedIds;
        }

        // ........................
        // Методы изменения модели
        // ........................
        // Три метода пополнения модели

        /// <summary>
        /// Уничтожение айтема
        /// </summary>
        /// <param name="item_id">
        /// The item_id.
        /// </param>
        public void DeleteItem(XName item_id)
        {
            SResource resource;
            if (!resources.ContainsKey(item_id))
            {
                resource = new SResource(item_id);
                resources.Add(item_id, resource);
            }
            else
            {
                resource = resources[item_id];
                //if (resource._content != null)
                //    resource._content.Definition.RemoveInstance(item_id);
                //else OntologyModel.RemoveItemFromInstances(item_id);
                FreeContent(resource);
                // Не знаю как обработать обратные ссылки, наверное их надо открепить от соответствующих узлов
                if (resource._inverseProperties != null)
                foreach (var ol in resource._inverseProperties)
                {
                    //TODO: ??? Похоже, прямые ссылки, ведущие на ресурс то ли нужно, то ли можно оставлять без изменений Target у них будет null
                }
            }
            resource.Deleted = true;
            return; // new XElement(SNames.TagDelete, new XAttribute(SNames.AttItem_id, item_id));
        }
        public XElement DeleteElement(string item_id)
        {          
            return new XElement(SNames.TagDelete, new XAttribute(SNames.AttItem_id, item_id));
        }


        
        /// <summary>
        ///  Замена айтема
        /// </summary>
        /// <param name="oldId">
        /// The old_id.
        /// </param>
        /// <param name="newId">
        /// The new_id.
        /// </param>
        public void SubstituteItem(XName oldId, XName newId)
        {
            bool first_exists = this.resources.ContainsKey(oldId);
            bool second_exists = this.resources.ContainsKey(newId);
            if (!first_exists && !second_exists)
            {
                SResource resource = new SResource(oldId);
                resources.Add(oldId, resource);
                resources.Add(newId, resource); resource._mergedIds.Add(newId); resource._lastId = newId;
                return;
            }
            if (!first_exists) 
            { 
                SResource resource = resources[newId];
                resources.Add(oldId, resource); resource._mergedIds.Add(oldId);
                // последний идентификатор остается из ресурса
                return; 
            }
            if (!second_exists)
            {
                SResource resource = resources[oldId];
                resources.Add(newId, resource); resource._mergedIds.Add(newId);
                resource._lastId = newId; // В ресурсе меняется последний идентификатор на новый
                return;
            }
            SResource first_resource = this.resources[oldId];
            SResource second_resource = this.resources[newId];
            if (first_resource == second_resource) return;
            // перенос внешних ссылок с первого ресурса на второй
            if (first_resource._inverseProperties != null)
            foreach (SObjectLink olink in first_resource._inverseProperties)
            {
                olink._target_resource = second_resource;
                if (second_resource._inverseProperties == null) second_resource._inverseProperties = new List<SObjectLink>();
                second_resource._inverseProperties.Add(olink);
            }
            // перенос списка слияния с первого ресурса на второй
            second_resource._mergedIds.AddRange(first_resource._mergedIds);
            // перенос ссылки из входа на второй
            foreach (XName mergedId in first_resource._mergedIds)
            {
                resources[mergedId] = second_resource;
            }
            // Последний идентификатор остается из второго ресурса
            //first_resource._inverseProperties = null; // Это необязательно, поскольку первый ресурс будет откреплен
            // освобождение контента ресурса, т.е. убирание (уничтожение) прикрепленного узла и его исходящих стрелок  
            this.FreeContent(first_resource);
            // this.resources[oldId] = second_resource; // это уже выполнено в цикле
        }

        //.......................
        // Вспомогательные методы
        //.......................

        /// <summary>
        /// производит (насильственное) вставление элемента в модель, перед применением нужно делать проверки, 
        /// используется в AddElement
        /// </summary>
        /// <param name="element">
        /// The element.
        /// </param>
        private void IncludeItem(XElement element)
        {
            SNode item = this.CreateRecord(element);
            // Привязать прямые ссылки
            
            foreach (SObjectLink olink in item.DirectObjectLinks())
            {
                XName target_id = olink.TargetId;
                SResource target_resource;
                
                if (resources.ContainsKey(target_id))
                {
                    target_resource = resources[target_id];
                }
                else
                {
                    target_resource = new SResource(target_id);                   
                    resources.Add(target_id, target_resource);
                }             
                olink._target_resource = target_resource;
                if (target_resource._inverseProperties == null) target_resource._inverseProperties = new List<SObjectLink>();
                target_resource._inverseProperties.Add(olink);
            }
            var resource = PrepareResource(element);
            resource._content = item;
            item._resource = resource;
            //return item;
        }

        /// <summary>
        /// Онтологическая база 
        /// </summary>
        public SOntologyModel OntologyModel { get; set; }



        // Реализационная база  
        /// <summary>
        ///  Таблица учтеных айтемов со входом через прошлый или настоящий идентификатор.
        /// </summary>
        private Dictionary<XName, SResource> resources = new Dictionary<XName, SResource>(100000);


        /// <summary>
        /// Initializes a new instance of the <see cref="SDataModel"/> class.
        /// </summary>
        public SDataModel()
        {
            OntologyModel = null;
        }

        /// <summary>
        /// Освобождение ресурса. Предполагается, что контент ресурса - нормальный
        /// </summary>
        /// <param name="resource">
        /// The resource.
        /// </param>
        protected void FreeContent(SResource resource)
        {
            SNode node = resource._content;
            // Если нет контента
            if (node == null) return;
            foreach (SObjectLink olink in node.DirectObjectLinks().Where(p => p._target_resource != null)) //TODO: возможно, проверка Where излишняя
            {
                SResource target_resource = olink._target_resource;
                target_resource._inverseProperties.Remove(olink);
                //olink.Target = null;
            }
            resource._content = null;
        }

        /// <summary>
        /// Подготовка ресурса для того, чтобы образовать запись. Если ресурс занят, то он освобождается
        /// </summary>
        /// <param name="x">
        /// The x.
        /// </param>
        /// <returns>
        /// </returns>
        protected SResource PrepareResource(XElement x)
        {
            XName item_id = SNode.Coding(x.Attribute(SNames.rdfabout).Value);
            SResource resource;
            if (this.resources.ContainsKey(item_id))
            {
                resource = this.resources[item_id];
                // Если уничтоженный, то возвращается null
                //if (resource.Deleted) return null;
                // Если есть контент, то он освобождается
                if (resource._content != null) FreeContent(resource);
            }
            else
            {
                resource = new SResource(item_id);
                resources.Add(item_id, resource);
            }
            return resource;
        }
        
        /// <summary>
        /// Создание записи по X-элементу. Запись сразу встраивается в ресурс и таблицу ресурсов, но не в граф.
        /// </summary>
        /// <param name="x">
        /// The x.
        /// </param>
        /// <returns>
        /// </returns>
        public virtual SNode CreateRecord(XElement x)
        {
            return new SNode(x, PrepareResource(x), this);
        }

        /// <summary>
        /// </summary>
        /// <param name="xrdfFogDoc">
        /// The xrdf fog doc.
        /// </param>
        public void LoadRDF(XElement xrdfFogDoc)
        {
            foreach (XElement relement in xrdfFogDoc.Elements())
            {
                Include(relement);
            }
        }
        public Action<XElement, string> AddToUserWorkDoc;
        
        /// <summary>
        /// Добавление записи формата rdf или оператора удаления или замены в модель.
        /// </summary>
        /// <param name="xOperator">
        /// xml представление записи.
        /// </param>
        public void Include(XElement xOperator)
        {
            XName rname = xOperator.Name;
            XAttribute userDocumentId = xOperator.Attribute(SNames.AttUsersWorkRDFDocumentId);
            if (userDocumentId != null) 
            {               
               // var docUri = GetItemById(userDocumentId.Value).Uri();     
                userDocumentId.Remove();              
                AddToUserWorkDoc(xOperator, userDocumentId.Value);
               // LOG.WriteLine("\n\n" + DateTime.Now.ToString() + " " + userDocumentId.Value + "\n" + xOperator.ToString());              
            }
            if (rname == SNames.TagDelete)
            {
                if (!string.IsNullOrEmpty(xOperator.Attribute(SNames.AttItem_id).Value))
                {
                    XName id = SNode.Coding(xOperator.Attribute(SNames.AttItem_id).Value);
                   this.DeleteItem(id);
                }
            }
            else if (rname == SNames.TagSubstitute)
            {
                if (!string.IsNullOrEmpty(xOperator.Attribute(SNames.AttOld_id).Value)
                    && !string.IsNullOrEmpty(xOperator.Attribute(SNames.AttNew_id).Value))
                {
                    XName old_id = SNode.Coding(xOperator.Attribute(SNames.AttOld_id).Value);
                    XName new_id = SNode.Coding(xOperator.Attribute(SNames.AttNew_id).Value);
                    this.SubstituteItem(old_id, new_id);
                }
            }
            else
            {
                if (string.IsNullOrEmpty(xOperator.Attribute(SNames.rdfabout.ToString()).Value)) return;
                XName element_id = SNode.Coding(xOperator.Attribute(SNames.rdfabout.ToString()).Value);
                // Если этот идентификатор неопределен, то ничего не делаем
                // если нет ресурса, то вставляем айтем
                if (!this.resources.ContainsKey(element_id)) { this.IncludeItem(xOperator); return; }
                SResource resource = this.resources[element_id];
                // если ресурс уничтожен, то элемент не вставляем
                if (resource.Deleted) return;
                // смотрим контент ресурса
                SNode node = resource._content;
                // если контента нет, то вставляем элемент
                if (node == null) { this.IncludeItem(xOperator); return; }
                // если (контент есть)& 
                // определяемый ид не совпадает с идентификатором узла, 
                // НО определяемый совпадает с последним, ТО меняем контент на новый
                if (element_id != node.Id)
                { // Здесь должно осуществиться и другое условие
                    if (element_id == resource._lastId)
                    {
                        this.IncludeItem(xOperator);
                    }
                    // иначе данные устаревшие и не фиксируются
                }
                else
                { // Здесь замена осуществится только если временная отметка нового определения позже старой
                    // выявляем временную отметку у элемента
                    DateTime modificationTime = DateTime.MinValue;
                    XAttribute mt = xOperator.Attribute(SNames.AttModificationTime);
                    if (mt != null && DateTime.TryParse(mt.Value, out modificationTime) && modificationTime > node.modificationTime)
                        this.IncludeItem(xOperator);
                }
            }
        }

        /// <summary>
        /// </summary>
        /// <param name="filename">
        /// The filename.
        /// </param>
        /// <returns>
        /// </returns>
        public XElement LoadRDF(string filename)
        {
            XElement rdf = XElement.Load(filename);
            LoadRDF(rdf);
            return rdf;
        }
    }
}
