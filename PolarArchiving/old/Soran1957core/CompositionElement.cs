using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using SGraph;
using factograph;

namespace Soran1957core
{
    public class CompositionElement
    {
        public CompositionVariants Variant { get; private set; }
        public SNode FocusNode { get; private set; }
        public SObjectLink ObjectLink { get; private set; }
        private readonly SObjectLink pair_link;
        public SNode PairNode { get { return pair_link == null ? null : pair_link.Target; } }
        public CompositionElement(CompositionVariants variant, SObjectLink olink)
        {
            this.Variant = variant;
            this.ObjectLink = olink;
            FocusNode = variant == CompositionVariants.DirectSystem ? olink.Target : olink.Source;
            if (variant == CompositionVariants.BinaryRelation)
                pair_link = FocusNode.DirectObjectLinks()
                    .FirstOrDefault(lnk => lnk != olink && lnk.Definition.IsEssential);
        }
        public CompositionElement(SNode node)
        {
            ObjectLink = null;
            Variant = CompositionVariants.SystemObject;
            FocusNode = node;
        }

        private static XName GroupId(SObjectLink p)
        {
            // Здесь можно добавить всякие особые случаи группирования
            XName pname = p.Definition.Id;
            if (pname == "father" || pname == "mother" || pname == "husband" || pname == "wife") return "family";
            if (pname == "in-collection" && p.Source
                                                .DirectObjectLinks("collection-item").Any(d => d.Target != null && d.Target.Definition.OfDocType())) return "docincollection";
            if (pname == "reflected" && 
                p.Source
                .DirectObjectLinks("in-doc")
                .Any(d => d.Target != null && d.Target.Definition.OfDocType())) 
                return "docinreflection";
            if (pname == "in-org"
                && p.Source.DirectProperty("role-classification") != null
                && ((SDataLink)p.Source.DirectProperty("role-classification")).InnerText == "director")
                return "first-faces";
            if (pname == "participant"
                && (p.Source.Definition.Id == "participation"
                    && p.Source.DirectObjectLinks("in-org").Any(dol =>
                        dol.Target != null
                        && dol.Target.DirectProperty("org-classification") != null
                        && ((SDataLink)dol.Target.DirectProperty("org-classification")).InnerText == "organization")))
                return "work-inorg";
            return p.Definition.Id;
        }
        public static Dictionary<XName, CompositionGroup> ExtractCompositionGroups(SNode item)
        {
            // Создадим набор групп в которые будем складывать элементы композиции
            var ce_groups = new Dictionary<XName, CompositionGroup>
                {
                    {"description", new CompositionGroup()},
                    {"family", new CompositionGroup {Label = "Семья"}}
                };

            // Создание групп, отличающихся от имен обратных элементов

            // Специальные элементы прямых свойств нужно поместить в соответствующие группы
            foreach (SProperty prop in item.DirectProperties())
            {
                if (prop.Definition.Id == "description")
                    ce_groups["description"].Elements.Add(new CompositionElement(item));
                //if (prop.Definition.Id == "description" || prop.Definition.Id == "doc-content")
                //    ce_groups["description"].Elements.Add(new CompositionElement(CEVid.DirectField, prop, "aaa", visualcontext.ontology_model));
                //else 
                else if (prop.Definition.Id == "father" || prop.Definition.Id == "mother")
                    ce_groups["family"].Elements.Add(new CompositionElement(CompositionVariants.DirectSystem, (SObjectLink)prop));
                //    else if (prop.Definition.Id == "mother") 
                //        ce_groups["family"].Elements.Add(new CompositionElement(CompositionVariants.DirectSystem, (SGraph.SObjectLink)prop));
            }
            // Обработка обратных ссылок
            IEnumerable<IGrouping<XName, SObjectLink>> groupedInverseQuery = item.InverseProperties().
                GroupBy(GroupId, p => p);
            foreach (IGrouping<XName, SObjectLink> inverseGroup in groupedInverseQuery)
            {
                XName group_id = inverseGroup.Key;
                // формируем список элементов группы
                List<CompositionElement> group_elements = inverseGroup
                    .Select(inverseProp =>
                        inverseProp.Source.Definition.IsSystemObject ?
                            new CompositionElement(CompositionVariants.InverseSystem, inverseProp) :
                        inverseProp.Source.Definition.IsUnary ?
                            new CompositionElement(CompositionVariants.UnaryRelation, inverseProp) :
                            new CompositionElement(CompositionVariants.BinaryRelation, inverseProp))
                    .OrderBy(ce => ce.PairNode.Name())//.sorting) //TODO: Надо будет вернуться к проблеме сортировки
                    .ToList();
              
                // Добавим вход в словарь групп
                if (ce_groups.ContainsKey(group_id)) ce_groups[group_id].Elements.AddRange(group_elements);
                else
                {
                    string label = "Группа";
                    //CompositionElement celement = group_elements.First();
                    //label = celement.inv_c_def.Label; TODO: надо правильно установить метку группы
                    ce_groups.Add(group_id, new CompositionGroup { Elements = group_elements, Label = label });
                }
            }
            if (ce_groups["family"].Elements.Count == 0) ce_groups.Remove("family");
            if (ce_groups["description"].Elements.Count == 0) ce_groups.Remove("description");
            ce_groups = ce_groups.OrderBy(ceElement => ceElement.Key.LocalName)
                .ToDictionary(ceElement=>ceElement.Key, ceElemnt=>ceElemnt.Value);
            return ce_groups;
        }

        //TODO: надо бы перенести процедуру в другой класс
        public static IEnumerable<ROntologyDatatypePropertyDefinition> EssentialFields(ROntologyClassDefinition type_def)
        {
            return type_def == null
                       ? new List<ROntologyDatatypePropertyDefinition>()
                       : type_def
                             .InverseProperties("domain")
                             .Select(p => p.Source)
                             .Where(c2 => c2.GetType() == typeof(ROntologyDatatypePropertyDefinition))
                             .Cast<ROntologyDatatypePropertyDefinition>()
                             .Where(c3 => c3.XSource.Attribute("essential") != null && c3.XSource.Attribute("essential").Value == "yes")
                             .Concat(EssentialFields(type_def.Superclass));
        }

        public static XObject DmDataField(SGraph.SNode item, SGraph.ROntologyDatatypePropertyDefinition prop)
        {
            string str = null;
            var range_name = prop.DirectObjectLinks("range")
                .Select(p => p.TargetId).FirstOrDefault();
            if (range_name == "string")
            {
                str = item.DataField(prop.Id);
            }
            else if (range_name == "date")
            {
                str = item.DataField(prop.Id);
                str = SSNodeExt.DatePrinted(str);
            }
            else if (range_name == "text")
            {
                str = item.TextField(prop.Id, "ru");
            }
            else if (range_name == "Email")
            {
                str = item.TextField(prop.Id, "ru");
            }
            else if (range_name == "URI")
            {
                string uri_text = item.DataField(prop.Id);
                return uri_text != null
                           ? new XElement("a",
                                          new XAttribute("href", uri_text),
                                          new XAttribute("target", "external"),
                                          uri_text)
                           : null;
            }
            else if (range_name != null)
            {
                var range_def = StaticModels.sDataModel.OntologyModel.GetItemById(range_name);
                if (range_def != null && range_def.Definition.Id == "EnumerationType")
                {
                    string enum_value = item.DataField(prop.Id);
                    if (enum_value != null)
                        str = range_def.DirectProperties().Where(p => p.Definition.Id == "state").Cast<SGraph.SDataLink>()
                            .Select(p => p.XOriginal).Where(px =>
                                px.Attribute("value").Value == enum_value &&
                                px.Attribute(SGraph.SNames.xmllang).Value == "ru")
                            .Select(px => px.Value).FirstOrDefault();
                }
            }
            return str == null ? null : new XText(str);
        }
        public static XElement ListOfDocs(CompositionElement[] celementsInput, string pageIndexParamName,
            string pageIndexParamValue, Uri requstUri, SNode mainItem)
        {
            var listcomponent = new XElement("div");
            var celements = celementsInput;//.OrderBy(ce => ce.PairNode.Name())
                //.OrderBy(ce =>
                //{
                //    int result = -1;
                //    Int32.TryParse(ce.PairNode.Name(), out result);
                //    return result;
                //})
                //.OrderBy(ce =>
                //             {
                //                 DateTime dateTime = DateTime.MaxValue;
                //                 int result = -1;
                //                 if(Int32.TryParse(ce.PairNode.Name(), out result))
                //                     return dateTime;
                //                 DateTime.TryParse(ce.PairNode.DateFirst(), out dateTime);
                //                 return dateTime;
                //             })
            //   .ToArray();
            int pageEnd, pageStart;
            // Панель для списка страниц
            if (celements.Length > StaticObjects.pagePortion)
            {

                var maxIndex = (celements.Length - 1) / StaticObjects.pagePortion;
                int pageIndex;
                if (!Int32.TryParse(pageIndexParamValue, out pageIndex) || pageIndex < 0 || pageIndex > maxIndex)
                    pageIndex = 0;

                var requstString = requstUri.ToString().Replace('&' + pageIndexParamName + "=" + pageIndexParamValue, "")
                    .Replace('?' + pageIndexParamName + "=" + pageIndexParamValue, "?");
                if (!requstString.Contains('?')) requstString += '?';
                listcomponent.Add(
                    new XElement("div",
                                 Enumerable.Range(0, maxIndex + 1)
                                     .SelectMany(i => new XObject[]
                                                          {
                                                              new XElement("a",
                                                                           new XAttribute("class", i == pageIndex ? "pressed" : "unpressed"),
                                                                           new XAttribute("href", requstString+'&'+pageIndexParamName+'='+i),
                                                                           (i + 1)),
                                                              new XText(" ")
                                                          })));
                pageStart = StaticObjects.pagePortion * pageIndex;
                pageEnd = Math.Min(pageStart + StaticObjects.pagePortion, celements.Length);

            }
            else
            {
                pageStart = 0;
                pageEnd = celements.Length;
            }
            //listcomponent.Add(
            //   new XElement("div",
            //                Enumerable.Range(0, (celements.Length - 1)/StaticObjects.pagePortion + 1)
            //                    .SelectMany(i => new XObject[]
            //                                          {
            //                                              new XElement("a",
            //                                                           new XAttribute("class", i == 0 ? "pressed" : "unpressed"),
            //                                                           new XAttribute("onclick",
            //                                                                          "javascript:Press(this);Extend2(ListPanel,"
            //                                                                          + i*StaticObjects.pagePortion
            //                                                                          + "," + StaticObjects.pagePortion + ")"),
            //                                                           (i + 1)),
            //                                              new XText(" ")      })));
            //               
            //XElement pageList = new XElement("div");
            //for (int i = 0; i < ; i++)
            //    pageList.Add(   
            //        new XElement("a",
            //                     new XAttribute("class", i == 0 ? "pressed" : "unpressed"),
            //                     new XAttribute("onclick",
            //                                    "javascript:Press(this);Extend2(ListPanel," + i*StaticObjects.pagePortion + "," + StaticObjects.pagePortion + ")"),
            //                     "" + (i + 1)),
            //        new XText(" "));
            //listcomponent.Add(pageList);
            var docs = new XElement("div", new XAttribute("ID", "ListPanel"));
            for (int i = pageStart; i < pageEnd; i++)
            {
                var ce = celements[i];
                var display_node = ce.PairNode;
                if (ce.Variant == CompositionVariants.SystemObject) display_node = ce.FocusNode;
                if (display_node == null) continue; //TODO: Будет без следа пропущен один элемент
                string imageurl = display_node.Definition.Id == "photo-doc" ?
                    "Default.aspx?r=small&u=" + display_node.Uri() :
                    StaticObjects.Icons.Elements("icon").Where(xico => xico.Attribute("type").Value == display_node.Definition.Id)
                        .Select(xico => xico.Attribute("image").Value).FirstOrDefault();
                string deepZoomUrl = null; 
                                    //(display_node.Definition.Id == ONames.TagDocument || display_node.Definition.Id == ONames.TagMultiPartDocument) && display_node.InverseProperties("scanned-document").Any()
                                    //     ? "DeepZoomMarked.aspx?id=" + display_node.Id +
                                    //     "&" + ce.ObjectLink.Source.Definition.Id + "id=" + ce.ObjectLink.Source.Id
                                    //     : null;
                string display_nodeUrl = "?id=" + display_node.Id; //+ "&"+ce.ObjectLink.Source.Definition.Id+"id="+ce.ObjectLink.Source.Id;

                var brick =
                    new XElement("div", new XAttribute("style", "float:left;width:126px;height:170px;font-size:10px;"),   //height:140px
                        new XElement("table",
                            new XElement("tr", new XElement("td", new XElement("a",
                                        new XAttribute("href", deepZoomUrl ?? display_nodeUrl),
                                        new XElement("img", new XAttribute("alt", imageurl ?? ""), new XAttribute("style", "border:0;"),
                    /*i >= StaticObjects.pagePortion ? null :*/
                                        new XAttribute("src", deepZoomUrl == null /*? (imageurl ?? "") : UrsulPager.DZMPreviewUri(display_node)*/)))),
                            new XElement("tr", new XElement("td",
                new XAttribute("style", "text-align:center;"),
                (display_node.Definition.OfDocType())
                ? !display_node.InverseProperties("scanned-document").Any()
                 ? SpanDates(display_node, mainItem)
                 : new XElement("div",
                     SpanDates(display_node, mainItem), ", " + display_node.Name() + " " + display_node.DateFirst())
                : new XElement("div",
                                         new XElement("div", display_node.Name()),
                                         new XElement("div", display_node.TextField("description", "ru"))))),
                            null)));
                docs.Add(brick);
            }
            listcomponent.Add(docs);
            return listcomponent;
        }
        public static XElement SpanDates(SNode item, SNode excepted)
        {
            string df = item.DateFirst();
            string dl = item.DateLast();
            XElement xdates = (!String.IsNullOrEmpty(df) || !String.IsNullOrEmpty(dl)) ?
                    new XElement("span", //new XAttribute("class", "date"),
                // "[" + 
                        SSNodeExt.DatePrinted(df) + (dl != null ? "-" + SSNodeExt.DatePrinted(dl) : "")
                //+ "]"
                        )
                    : null;
            IEnumerable<XObject> xPlaces =
                from inDoc in item.InverseProperties("in-doc")
                let ground = inDoc.AllElseProps<SDataLink>().FirstOrDefault(g => g.Definition.Id == "ground")
                where ground != null && ground.InnerText == "place"
                let reflectedPlace = inDoc.AllElseProps<SObjectLink>().FirstOrDefault(refl => refl.Definition.Id == "reflected")
                where reflectedPlace != null && reflectedPlace.Target != null && reflectedPlace.Target != excepted
                                            && reflectedPlace.Target.Name() != String.Empty
                select new XElement("span", reflectedPlace.Target.Name());
            //,new XAttribute("href", "?id="+reflectedPlace.TargetId));
            return new XElement("span",
                new XAttribute("style", "text-align:center;"),
                xdates, xdates != null && xPlaces.Any() ? ", " : null,
                EnumExtensions.InsertSplitter(xPlaces, new XText(", ")));
        }    
    }

    public enum CompositionVariants
    {
        SystemObject,
        DirectSystem,
        InverseSystem,
        UnaryRelation,
        BinaryRelation
        //, DirectField
    }
    public class CompositionGroup
    {
        public string Label { get; set; }
        public CompositionGroup()
        {
            this.Elements = new List<CompositionElement>();
        }

        public List<CompositionElement> Elements { get; set; }
    }
    public static class EnumExtensions
    {
        /// <summary>
        /// Insertes splitter after each element except last.
        /// </summary>
        /// <param name="sequence"></param>
        /// <param name="splitter"></param>
        /// <returns></returns>
        public static IEnumerable<X> InsertSplitter<X>(this IEnumerable<X> sequence, X splitter)
        {
            if (sequence == null) yield break;
            var length = sequence.Count();
            if (length == 0) yield break;
            yield return sequence.First();
            foreach (var item in sequence.Skip(1))
            {
                yield return splitter;
                yield return item;
            }
        }
    }

}
