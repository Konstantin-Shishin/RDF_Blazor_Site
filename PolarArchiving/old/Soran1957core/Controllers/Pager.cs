using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Xml.Linq;
using SGraph;

namespace Soran1957core
{
    public class Pager
    {
        public static string style_logo = "";//"position:fixed; left:150px; top:0px; width:auto; height:61px;";
        public static string style_logi = "";//"position:fixed; right:0px; top:0px; width:380px; height:61px;";
        public static string style_menu = "";//"position:fixed; left:0px; top:100px; width:150px; height:auto;";
        public static string style_sear = "";//"position:fixed; left:150px; top:100px; right:380px; width:auto; height:80px;";

        public static string home_id = "w20070417_7_1744"; // "pahomepage";
        public static string soran_id = "w20070417_3_6186";
        public static string database_id = "w20070417_4_1010";
        public static string about_id = "w20070417_3_4658";
        public static string photocollections = "w20070417_4_1012";
        public static string sitecollection = "w20070417_4_1027";
        public static string usefullLinks_id = "usefullLinks";
        public static string newspaper_id = "newspaper_cassetteId";

        public static XElement ConstructPage(Microsoft.AspNetCore.Http.HttpContext context)
        {
            //StaticObjects.look = OAData.OADB.SearchByName("Марчук Ал").Select(x => x.ToString()).Aggregate((x, y) => x + "|\n" + y);
            //StaticObjects.look = OAData.OADB.GetItemByIdBasic("svet_100616111408_18691", true).ToString();

            string tilda = context.Request.PathBase;
            string sdirection = context.Request.Query["sdirection"];
            //if (sdirection == null) sdirection = (string)Session["sdirection"];
            if (sdirection == null) sdirection = "person";
            //Session["sdirection"] = sdirection;

            XElement centralPanel = new XElement("div");
            XElement rightPanel = new XElement("div");

            string searchstring = context.Request.Query["searchstring"];
            if (searchstring != null) BuildSearchResults(centralPanel, rightPanel, searchstring, sdirection);
            else BuildItem(centralPanel, rightPanel, string.IsNullOrWhiteSpace(context.Request.Query["id"].FirstOrDefault()) ?
                home_id : context.Request.Query["id"].FirstOrDefault());
            string id = context.Request.Query["id"];
            string p = context.Request.Query["p"];
            XElement logo =
                new XElement("div", new XAttribute("id", "logo"), new XAttribute("style", style_logo),
                    new XAttribute("width", "500"), new XAttribute("height", "99"),
                    new XElement("a", new XAttribute("href", ""),
                        new XElement("img", new XAttribute("src", tilda+"/logo2.gif"),
                            new XAttribute("width", "391"), new XAttribute("height", "61"),
                            new XAttribute("vspace", "12"), new XAttribute("alt", "СО РАН с 1957 года"),
                            new XAttribute("border", "0"))),
                    null);
            XElement login =
                new XElement("div", new XAttribute("id", "login"), new XAttribute("style", style_logi),
                    new XElement("table", new XAttribute("cellpadding", "0"), new XAttribute("cellspacing", "0"),
                        new XAttribute("border", "0"), new XAttribute("width", "100%"),
                        new XElement("tr",
                            new XElement("td", new XAttribute("class", "text_arial_small"), "имя: "),
                            new XElement("td", new XAttribute("width", "40%"),
                                new XElement("input", new XAttribute("type", "text"), new XAttribute("class", "form_login"))),
                            new XElement("td", new XAttribute("class", "text_arial_small"), "пароль: "),
                            new XElement("td", new XAttribute("width", "40%"),
                                new XElement("input", new XAttribute("type", "password"), new XAttribute("class", "form_login"))),
                            new XElement("td",
                                new XElement("input", new XAttribute("type", "image"), new XAttribute("width", "19"), new XAttribute("height", "19"), new XAttribute("src", tilda+"/btn_login.gif")))),
                        new XElement("tr", new XElement("td"),
                            new XElement("td", new XAttribute("colspan", "4"), new XAttribute("class", "text_arial_small"),
                                new XElement("a", "регистрация"),
                                new XText(" | "),
                                new XElement("a", "забыли пароль?"))),
                        null),
                    null);
            XElement menu =
                new XElement("div", new XAttribute("id", "menu"), new XAttribute("style", style_menu),
                    new XElement("a", new XAttribute("href", "?id=" + home_id),
                        new XElement("img",
                            new XAttribute("src", tilda+"/i_home.gif"),
                            new XAttribute("width", "10"), new XAttribute("height", "10"),
                            new XAttribute("hspace", "4"), new XAttribute("border", "0"),
                            new XAttribute("alt", "Главная страница"))),
                    new XElement("a", new XAttribute("href", "mailto:pavl@iis.nsk.su?subject=soran1957.ru%20 " + (string.IsNullOrWhiteSpace(context.Request.Query["id"]) ? home_id : id)),
                        new XElement("img", new XAttribute("src", tilda+"/i_mail.gif"), new XAttribute("width", "10"),
                            new XAttribute("height", "10"), new XAttribute("hspace", "11"),
                            new XAttribute("border", "0"), new XAttribute("alt", "Написать письмо"))),
                    new XElement("br"),
                    new XElement("hr", new XAttribute("noshade", "1"), new XAttribute("size", "1"), new XAttribute("color", "#cccccc")),
                    new XElement("div", new XAttribute("class", "text_menu"),
                        new XElement("div", new XAttribute("class", "p_menu"),
                            new XElement("a", new XElement("b", "Начало"), id == home_id ? null : new XAttribute("href", "?id=" + home_id)),
                            new XElement("hr", new XAttribute("noshade", "1"), new XAttribute("size", "3"), new XAttribute("color", "#cccccc")),
                            new XElement("a", new XElement("b", "СО РАН"), id == soran_id ? null : new XAttribute("href", "?id=" + soran_id)),
                            new XElement("hr", new XAttribute("noshade", "1"), new XAttribute("size", "3"), new XAttribute("color", "#cccccc")),
                            new XElement("a", new XElement("b", "Первичные фотоматериалы"), p == database_id ? null : new XAttribute("href", "?id=" + database_id)),
                            new XElement("hr", new XAttribute("noshade", "1"), new XAttribute("size", "3"), new XAttribute("color", "#cccccc")),
                            new XElement("a", new XElement("b", "Архив газет \"За науку в Сибири\""), new XAttribute("href", "?id=" + newspaper_id)),// new XAttribute("target", "external")
                            new XElement("hr", new XAttribute("noshade", "1"), new XAttribute("size", "3"), new XAttribute("color", "#cccccc")),
                            new XElement("a", new XElement("b", "О проекте"), p == about_id ? null : new XAttribute("href", "?id=" + about_id)),
                            new XElement("hr", new XAttribute("noshade", "1"), new XAttribute("size", "3"), new XAttribute("color", "#cccccc")),
                            new XElement("a", new XElement("b", "Полезные ссылки"), id == soran_id ? null : new XAttribute("href", "?id=" + usefullLinks_id)),
                            null),
                        null),
                    new XElement("hr", new XAttribute("noshade", "1"), new XAttribute("size", "3"), new XAttribute("color", "#cccccc")),
                    null);
            XElement search =
                new XElement("div", new XAttribute("id", "search"), new XAttribute("style", style_sear),
                    new XElement("form", new XAttribute("action", ""),
                        new XElement("input", new XAttribute("type", "hidden"), new XAttribute("name", "p"), new XAttribute("value", "typed-item-list")),
                        new XElement("input", new XAttribute("type", "hidden"), new XAttribute("name", "ch"), new XAttribute("value", "search-form")),
                        new XElement("table", new XAttribute("cellpadding", "0"), new XAttribute("cellspacing", "0"), new XAttribute("border", "0"),
                            new XElement("tr",
                                new XElement("td", new XAttribute("class", sdirection == "photo-doc" ? "search_option_s" : "search_option"),
                                    new XElement("a", sdirection == "photo-doc" ? null : new XAttribute("href", "?sdirection=photo-doc"), "Фотографии")),
                                new XElement("td", new XAttribute("class", sdirection == "person" ? "search_option_s" : "search_option"),
                                    new XElement("a", sdirection == "person" ? null : new XAttribute("href", "?sdirection=person"), "Персоны")),
                                new XElement("td", new XAttribute("class", sdirection == "org-sys" ? "search_option_s" : "search_option"),
                                    new XElement("a", sdirection == "org-sys" ? null : new XAttribute("href", "?sdirection=org-sys"), "Организации")))),
                        new XElement("table", new XAttribute("cellpadding", "0"), new XAttribute("cellspacing", "0"), new XAttribute("border", "0"),
                            new XAttribute("width", "100%"), new XAttribute("height", "55"),
                            new XElement("tr",
                                new XElement("td", new XAttribute("width", "100%"), new XAttribute("class", "search_form"),
                                    new XElement("input", new XAttribute("name", "searchstring"),
                                        new XAttribute("type", "text"), new XAttribute("class", "form_search"))),
                                new XElement("td", new XAttribute("class", "search_form"),
                                    new XElement("input",
                                        new XAttribute("type", "image"),
                                        new XAttribute("width", "63"), new XAttribute("height", "19"),
                                        new XAttribute("src", tilda+"/btn_search.gif"),
                                        new XAttribute("style", "margin-right:5px;")))),
                            new XElement("tr", new XAttribute("valign", "top"),
                                new XElement("td", new XAttribute("colspan", "2"), new XAttribute("class", "search_form"),
                                    new XElement("input", new XAttribute("type", "checkbox"), new XAttribute("id", "search_all"),
                                        new XAttribute("disabled", "disabled"), new XAttribute("style", "border:0px solid #036;")),
                                    new XElement("label", new XAttribute("for", "search_all"), "искать по отдельным словам"),
                                    " | ",
                                    new XElement("a", "расширенный поиск")))),
                        null),
                    null);
            XElement html = new XElement("html",
                new XElement("head",
                    new XElement("meta", new XAttribute("charset", "utf-8")),
                    new XElement("link", new XAttribute("rel", "stylesheet"), new XAttribute("href", tilda+"/zh.css"), new XAttribute("type", "text/css")),
                    new XElement("link", new XAttribute("rel", "stylesheet"),
                      new XAttribute("type", "text/css"), new XAttribute("href", tilda+"/Styles.css")),
                    new XElement("script",
                      new XAttribute("language", "javascript"),
                      new XAttribute("type", "text/JScript"),
                      new XAttribute("src", tilda+"/PublicuemScript.js"),
                      "."),
                    null),
                new XElement("body",
                    new XElement("table", new XAttribute("width", "100%"), new XAttribute("style", "table-layout:fixed;"),
                        new XElement("col", new XAttribute("width", "150")),
                        new XElement("col", new XAttribute("width", "100%")),
                        new XElement("col", new XAttribute("width", "410")),
                        new XElement("tr", new XAttribute("valign", "top"),
                            new XElement("td", null),
                            new XElement("td", logo),
                            new XElement("td", login)),
                        new XElement("tr", new XAttribute("valign", "top"),
                            new XElement("td", menu, new XAttribute("rowspan", "2")),
                            new XElement("td", search),
                            new XElement("td", null)),
                        new XElement("tr", new XAttribute("valign", "top"),
                            //new XElement("td", null),
                            new XElement("td", centralPanel),
                            new XElement("td", rightPanel)),
                        new XElement("tr", new XAttribute("valign", "top"),
                            new XElement("td", new XAttribute("colspan", "3"), new XElement("hr"))),
                        new XElement("tr", new XAttribute("valign", "top"),
                            new XElement("td", null),
                            new XElement("td", new XAttribute("colspan", "2"), "(c) Фотоархив Сибирского отделения Российской академии наук")),
                        //new XElement("tr", new XAttribute("valign", "top"),
                        //    new XElement("td", new XAttribute("colspan", "3"), StaticObjects.look)),
                        null)));

            return html;
        }

        private static void BuildItem(XElement centralPanel, XElement rightPanel, string v)
        {
            //throw new NotImplementedException();
        }

        private static void BuildSearchResults1(XElement centralPanel, XElement rightPanel, string searchstring, string sdirection)
        {
            //throw new NotImplementedException();
        }
        /*
       private void BuildItem(XElement centralPanel, XElement rightPanel, string id)
       {
           //TODO: Нужен вариант индивидуального решения для домашней страницы
           SGraph.SNode node = StaticModels.sDataModel.GetItemById(id);
           if (node == null) return; //TODO: как-то по другому
           var cgroups = CompositionElement.ExtractCompositionGroups(node);
           XElement naming = new XElement("div");
           XElement description = null;
           XElement doclist = new XElement("div");
           SGraph.SNode portraitphoto = null;
           XElement firstfaces = new XElement("div");
           XElement participants = new XElement("div");
           XElement orgchilds = new XElement("div");
           XElement titles = new XElement("div");
           XElement works = null;
           XElement org_events = null;
           XElement incollection = null;
           XElement reflections = null;
           XElement authors = null;
           XElement archive = null;
           foreach (var pair in cgroups)
           {
               if (pair.Key == "referred-sys")
               {
                   naming.Add(new XElement("div", "Другие названия (имена):"));
                   var orderedEnumerable = pair.Value.Elements.Select(el => new { el, fd = el.FocusNode.DateFirst(), td = el.FocusNode.DateLast() })
                                               //TODO: Сортировка по дате, без даты наверх.
                                               .OrderBy(el =>
                                               {
                                                   Int32 firstDate = -1;
                                                   Int32.TryParse(el.fd, out firstDate);
                                                   return firstDate;
                                               }).OrderBy(el =>
                                               {
                                                   DateTime firstDate = DateTime.MinValue;
                                                   DateTime.TryParse(el.fd, out firstDate);
                                                   return firstDate;
                                               });

                   naming.Add(orderedEnumerable
                   .Select(el =>
                   {
                       string dates = null;
                       if (el.fd != null || el.td != null) dates =
                            " (" + (el.fd != null ? "с " + el.fd.Substring(0, 4) : "") +
                            (el.td != null ? " по " + el.td.Substring(0, 4) : "") +
                            ")";
                       return new XElement("div",
                           new XElement("span", el.el.FocusNode.TextField("alias", "ru"), new XAttribute("style", "font-weight:bold;")),
                           dates);
                   }));
               }
               else if (pair.Key == "description")
               {
                   description = new XElement("div",
                       pair.Value.Elements.Select(ce => new XElement("div", ce.FocusNode.TextField("description", "ru"))));
               }
               else if (pair.Key == "docinreflection" || pair.Key == "docincollection")
               {
                   CompositionElement[] listOfDocs = pair.Value.Elements
                       .OrderBy(e => e.PairNode.Name())
                       .OrderByDescending(e => e.PairNode.Definition.Id.LocalName).ToArray();
                   Session["listOfDocs"] = listOfDocs;
                   doclist = CompositionElement.ListOfDocs(listOfDocs, "pageListDoc", Request["pageListDoc"], Request.Url, node);
                   // Нахождение нужной фотки
                   if (node.Definition.Id == "person") //TODO: надо бы еще логитип для организации
                   {
                       portraitphoto = pair.Value.Elements.Where(ce =>
                           ce.PairNode != null && ce.FocusNode.DataField("ground") == "portrait")
                           .Select(ce => ce.PairNode)
                           .LastOrDefault();
                   }

               }
               else if (pair.Key == "first-faces")
               {
                   firstfaces.Add(new XElement("ul",
                       pair.Value.Elements
                       .OrderBy(ce => ce.FocusNode.DateFirst())
                       .Select(ce =>
                       {
                           string fd = ce.FocusNode.DateFirst();
                           return new XElement("li",
                               fd != null ? "с " + fd.Substring(0, 4) + " " : null,
                               ce.FocusNode.TextField("role", "ru") + " ",
                               ce.PairNode == null ? null : new XElement("a", new XAttribute("href", "?id=" + ce.PairNode.Id), ce.PairNode.Name()),
                               null);
                       })));
               }
               else if (pair.Key == "in-org")
               {
                   participants.Add(new XElement("ul",
                       pair.Value.Elements
                       .OrderBy(ce => ce.PairNode == null ? "zz" : ce.PairNode.Name())
                       .Select(ce =>
                       {
                           string fd = ce.FocusNode.DateFirst();
                           return new XElement("li",
                               fd != null && fd.Length > 0 ? "с " + fd.Substring(0, 4) + " " : null,
                               ce.PairNode == null ? null : new XElement("a", new XAttribute("href", "?id=" + ce.PairNode.Id), ce.PairNode.Name()),
                               " " + ce.FocusNode.TextField("role", "ru") + " ",
                               null);
                       })));
               }
               else if (pair.Key == "org-parent")
               {
                   orgchilds.Add(new XElement("ul",
                       pair.Value.Elements
                       .Select(ce => new XElement("li",
                           ce.PairNode == null ? null : new XElement("a", new XAttribute("href", "?id=" + ce.PairNode.Id), ce.PairNode.Name())))));
               }
               else if (pair.Key == "has-title")
               {
                   titles.Add(new XElement("h2", "Титулы"),
                       new XElement("ul",
                           pair.Value.Elements
                           .OrderBy(ce => ce.FocusNode.DateFirst())
                           .Select(ce =>
                           {
                               string fd = ce.FocusNode.DateFirst();
                               return new XElement("li",
                               fd != null ? "с " + fd.Substring(0, 4) + " " : null,
                               ce.FocusNode.TextField("degree", "ru"));
                           })));
               }
               else if (pair.Key == "work-inorg")
               {
                   works = new XElement("div",
                       new XElement("h2", "Места работы"),
                       new XElement("ul",
                       pair.Value.Elements
                           .OrderBy(ce => ce.FocusNode.DateFirst() ?? "zz")
                           .Select(ce =>
                           {
                               string fd = ce.FocusNode.DateFirst();
                               return new XElement("li",
                                   fd != null ? "с " + fd.Substring(0, 4) + " " : null,
                                   ce.PairNode == null ? null : new XElement("a", new XAttribute("href", "?id=" + ce.PairNode.Id), ce.PairNode.Name()),
                                   " " + ce.FocusNode.TextField("role", "ru"));
                           })));
               }
               else if (pair.Key == "participant")
               {
                   org_events = new XElement("div",
                       new XElement("h2", "Участие"),
                       new XElement("ul",
                       pair.Value.Elements
                           .OrderBy(ce => ce.FocusNode.DateFirst() ?? "zz")
                           .Select(ce =>
                           {
                               string fd = ce.FocusNode.DateFirst();
                               return new XElement("li",
                                   fd != null ? "с " + fd.Substring(0, 4) + " " : null,
                                   ce.PairNode == null ? null : new XElement("a", new XAttribute("href", "?id=" + ce.PairNode.Id), ce.PairNode.Name()),
                                   " " + ce.FocusNode.TextField("role", "ru"));
                           })));
               }
               else if (pair.Key == "in-collection")
               {
                   incollection = CompositionElement.ListOfDocs(pair.Value.Elements.OrderBy(ce => ce.PairNode.Name()).ToArray(), "pageListDoc", Request["pageListDoc"], Request.Url, node);
               }
               else if (pair.Key == "in-doc")
               {
                   reflections = new XElement("div",
                       new XElement("h2", "Отражены"),
                       pair.Value.Elements
                       .Where(ce => ce.PairNode != null)
                           .Select(ce => new XElement("div",
                               new XElement("a", new XAttribute("href", "?id=" + ce.PairNode.Id), ce.PairNode.Name()))));
               }
               else if (pair.Key == "adoc")
               {
                   authors = new XElement("div", "Автор: ",
                       pair.Value.Elements
                       .Select(ce => new XElement("span", new XAttribute("class", "margin-right: 10px;"),
                           ce.PairNode == null ? null : new XElement("a", new XAttribute("href", "?id=" + ce.PairNode.Id), ce.PairNode.Name()))));
               }
               else if (pair.Key == "archive-item")
               {
                   archive = new XElement("div",
                       pair.Value.Elements
                           .Select(ce => new XElement("div", "Архив: ",
                               new XElement("span", ce.PairNode.Name()),
                               ", Источник: ",
                               new XElement("span", ce.FocusNode.DirectObjectLinks("info-source")
                                   .Select(ol => ol.Target.Name())),
                               null)));
               }
           }
           if (!node.Definition.OfDocType()) centralPanel.Add(node.Id == home_id ? null : new XElement("h1", node.Name()),
                new XElement("div", CompositionElement.SpanDates(node, node)));
           else
           { // Документ

               CompositionElement[] listOfDocs = (CompositionElement[])Session["listOfDocs"];
               int ind = -1;
               if (listOfDocs != null)
               {
                   ind = listOfDocs.Select(ce => ce.PairNode).TakeWhile(nd => nd != node).Count();
                   if (ind == listOfDocs.Length) ind = -1;
               }

               centralPanel.Add(
                   ind != -1 ? new XElement("div", new XAttribute("style", "text-align:center;"),
                       new XElement("a", ind > 0 ? new XAttribute("href", "?id=" + listOfDocs[ind - 1].PairNode.Id) : null, "пред."),
                       new XText(" "),
                       new XElement("a", ind < listOfDocs.Length - 1 ? new XAttribute("href", "?id=" + listOfDocs[ind + 1].PairNode.Id) : null, "след."),
                       null) :
                   null, // пред. след.
                   new XElement("div", new XAttribute("style", "text-align:center;"),
                       DocHtml(node)),
                   new XElement("div", new XAttribute("style", "text-align:center;"), CompositionElement.SpanDates(node, node)));
           }
           if (description != null)
           {
               centralPanel.Add(description,
                                   new XElement("a", new XAttribute("href", "mailto:pavl@iis.nsk.su?subject=soran1957.ru" + (string.IsNullOrWhiteSpace(Request.Params["id"]) ? home_id : Request.Params["id"])),
                       new XElement("img", new XAttribute("src", tilda+"/i_mail.gif"), new XAttribute("width", "10"),
                           new XAttribute("height", "15"), new XAttribute("hspace", "11"),
                           new XAttribute("border", "0"), new XAttribute("alt", "уточнить/дополнить"))));
           }
           if (!node.Definition.OfDocType() && node.Id != home_id) centralPanel.Add(new XElement("div", new XElement("hr")));

           if (naming.Elements().Any()) centralPanel.Add(naming);
           centralPanel.Add(doclist);
           centralPanel.Add(new XElement("div", new XElement("hr")));
           if (authors != null) centralPanel.Add(authors);
           if (archive != null) centralPanel.Add(archive);
           // для простой коллекции (когда элементы коллекции не являются документы)
           if (incollection != null) centralPanel.Add(incollection);

           if (portraitphoto != null)
           {
               rightPanel.Add(
                   new XElement("img",
                       new XAttribute("src", "Default.aspx?r=small&u=" + portraitphoto.Uri()),
                       new XAttribute("align", "right"), new XAttribute("width", "150")));
           }

           // для документов
           if (reflections != null) rightPanel.Add(reflections);
           // для персоны
           rightPanel.Add(titles);
           if (works != null) rightPanel.Add(works);
           if (org_events != null) rightPanel.Add(org_events);
           // для органиации
           rightPanel.Add(firstfaces);
           rightPanel.Add(orgchilds);
           rightPanel.Add(participants);

           // Особый случай - первая страница сайта
           if (node.Id == home_id)
           {
               rightPanel.Add(
                   new XElement("img", new XAttribute("src", "Default.aspx?r=small&u=iiss://PA_folders24-59@iis.nsk.su/0001/0004/0392"), new XAttribute("align", "right")),
                   "Проект посвящен истории Сибирского отделения Российской Академии Наук. ",
                   "Здесь можно найти уникальные фотографии, информацию о людях и организациях и статьи, касающиеся разных периодов жизни Сибирского отделения.",
                   //new XElement("h2", "Фотоколлекции сайта"),
                   //new XElement("ul",
                   //    new XElement("li", new XElement("a", new XAttribute("href", "?id=w20070417_3_7246"), "Новосибирский Академгородок осенью")),
                   //    new XElement("li", new XElement("a", new XAttribute("href", "?id=w20071113_9_8252"), "Досуг в Новосибирском Академгородке")),
                   //    new XElement("li", new XElement("a", new XAttribute("href", "?id=w20071113_9_11681"), "Президенты АН и Председатели СО РАН")),
                   //    new XElement("li", new XElement("a", new XAttribute("href", "?id=w20070417_3_3211"), "Отцы-основатели")),
                   //    new XElement("li", new XElement("a", new XAttribute("href", "?id=w20071113_9_3091"), "Ветераны Великой Отечественной войны")),
                   //    new XElement("li", new XElement("a", new XAttribute("href", "?id=w20070417_7_1735"), "Лики науки")),
                   //    new XElement("li", new XElement("a", new XAttribute("href", "?id=w20070417_3_7152"), "Новосибирский Академгородок зимой")),
                   //    new XElement("li", new XElement("a", new XAttribute("href", "?id=w20071113_9_12201"), "Виды Новосибирского Академгородка")),
                   //    new XElement("li", new XElement("a", new XAttribute("href", "?id=w20070417_3_7263"), "Панорамные виды Новосибирского Академгородка")),
                   //    new XElement("li", new XElement("a", new XAttribute("href", "?id=w20070417_7_1806"), "Первопоселенцы Золотой долины")),
                   //    new XElement("li", new XElement("a", new XAttribute("href", "?id=w20070417_3_7197"), "Строительство Новосибирского Академгородка")),
                   //    new XElement("li", new XElement("a", new XAttribute("href", "?id=fog_pavlovskaya200812256983"), "Гости Новосибирского Академгородка (люди науки)")),
                   //    new XElement("li", new XElement("a", new XAttribute("href", "?id=fog_pavlovskaya200812256981"), "Гости Новосибирского Академгородка (официальные лица)")),
                   //    new XElement("li", new XElement("a", new XAttribute("href", "?id=w20070417_2_1046"), "Гости Новосибирского Академгородка")),
                   //    null),
                   //new XElement("h2", "Коллекция сайта"),
                   //new XElement("ul",
                   //    new XElement("li", new XElement("a", new XAttribute("href", "?id=w20071113_9_9041"), "Визит Н.С. Хрущева в Новосибирск (1959 г.)")),
                   //    new XElement("li", new XElement("a", new XAttribute("href", "?id=w20071030_1_26317"), "Визит Шарля де Голля в Новосибирский Академгородок")),
                   //    new XElement("li", new XElement("a", new XAttribute("href", "?id=w20071113_9_19474"), "Фестиваль авторской песни (1968 г.)")),
                   //    new XElement("li", new XElement("a", new XAttribute("href", "?id=w20070417_5_12843"), "Фестиваль авторской песни \"Под интегралом\" 40 лет спустя")),
                   //    new XElement("li", new XElement("a", new XAttribute("href", "?id=fog_pavlovskaya200812254337"), "Карнавал НГУ (1967 г.)")),
                   //    new XElement("li", new XElement("a", new XAttribute("href", "?id=w20070417_7_1063"), "Байкало-Амурская магистраль")),
                   //    new XElement("li", new XElement("a", new XAttribute("href", "?id=w20071120_10_18309"), "Клуб юных техников СО РАН")),
                   //    new XElement("li", new XElement("a", new XAttribute("href", "?id=w20070417_3_6323"), "Кафе-клуб \"Под интегралом\"")),
                   //    new XElement("li", new XElement("a", new XAttribute("href", "?id=fog_pavlovskaya200812254335"), "Новосибирский государственный университет")),
                   //    new XElement("li", new XElement("a", new XAttribute("href", "?id=fog_pavlovskaya200812254885"), "Делегация космонавтов \"Союз-Апполон\" в Академгородке")),
                   //    new XElement("li", new XElement("a", new XAttribute("href", "?id=w20071120_10_20556"), "Институт ядерной физики им. Г.И. Будкера СО РАН")),
                   //    new XElement("li", new XElement("a", new XAttribute("href", "?id=pavl_100531115859_9984"), "КВН НГУ (1985-1995)")),
                   //    new XElement("li", new XElement("a", new XAttribute("href", "?id=fog_pavlovskaya200812254396"), "КВН НГУ (1968-1970)")),
                   //    new XElement("li", new XElement("a", new XAttribute("href", "?id=piu_200809053371"), "Празднование масленицы (1967 г.)")),
                   //    null),
                   null);
               SGraph.SNode photocoll_node = StaticModels.sDataModel.GetItemById(photocollections);
               SGraph.SNode sitecoll_node = StaticModels.sDataModel.GetItemById(sitecollection);
               rightPanel.Add(
                        new XElement("h2", "Коллекции сайта"),
                        new XElement("ul",
                            photocoll_node == null ? null :
                           photocoll_node.InverseProperties(factograph.ONames.TagIncollection)
                            .Select(incollprop => incollprop.Source)
                            .Select(coll_memb => coll_memb.DirectObjectLinks(factograph.ONames.TagCollectionitem).FirstOrDefault())
                            .Where(coll_itm_prop => coll_itm_prop != null)
                            .Select(coll_itm_prop => coll_itm_prop.Target)
                            .Where(target => target != null)
                            .Select(target => new XElement("li",
                                new XElement("a", new XAttribute("href", "?id=" + target.Id), target.Name()))),
                            sitecoll_node == null ? null :
                           sitecoll_node.InverseProperties(factograph.ONames.TagIncollection)
                            .Select(incollprop => incollprop.Source)
                            .Select(coll_memb => coll_memb.DirectObjectLinks(factograph.ONames.TagCollectionitem).FirstOrDefault())
                            .Where(coll_itm_prop => coll_itm_prop != null)
                            .Select(coll_itm_prop => coll_itm_prop.Target)
                            .Where(target => target != null)
                            .Select(target => new XElement("li",
                                new XElement("a", new XAttribute("href", "?id=" + target.Id), target.Name()))),
                            null));
           }
       }

       private static XElement DocHtml(SNode node)
       {
           var uri = node.Uri();
           if (node.Definition.Id == "photo-doc")
               return new XElement("img", new XAttribute("src", "Default.aspx?r=medium&u=" + uri), new XAttribute("align", "center"));

           if (node.Definition.Id == "video-doc")
               return _Default.FlashVideo(node);

           if (node.Definition.Id == "audio-doc")
           {
               var dogIndex = uri.IndexOf("@");
               var casseteeName = uri.Substring(7, dogIndex - 7);
               var folderName = uri.Substring(uri.Length - 9, 9);
               string url = "cassettes/" + casseteeName + "/originals/" + folderName + ".mp3"; //@"J:\home\FactographProjects\Test20100903\documents\normal\0001\0056.flv";// "?flashUri=" + url;
               //string url = "audiosample.mp3";
               return new XElement("div",
                   new XElement("audio", new XAttribute("src", url), new XAttribute("preload", "auto"), new XAttribute("autoplay", "true"), " "), null);
           }

           if (node.Definition.Id == "document")
           {
               if (!node.DirectProperties<SDataLink>("iisstore").Any())
               {
                   if (node.DirectProperties<SDataLink>("doc-content").Any())
                   {
                       string text = node.DirectProperties<SDataLink>("doc-content").First().InnerText;
                       int maxRowLength = 50;
                       var rows = text.Split(new[] { Environment.NewLine, "\n", "\r" }, StringSplitOptions.RemoveEmptyEntries);
                       return new XElement("TEXTAREA", text,
                              new XAttribute("rows", rows.Sum(row => row.Length / maxRowLength + 1)),
                              //onclick="this.focus();this.select()"
                              new XAttribute("readonly", "readonly"),
                              new XAttribute("cols", maxRowLength)   //new XAttribute("style", "width:70%;")
                       );
                   }
                   if (node.DirectProperties<SDataLink>("published").Any())
                       return new XElement("a", node.Name(),
                           new XAttribute("href", node.DirectProperties<SDataLink>("published").First()),
                           new XAttribute("target", "external"));
                   if (
                       node.InverseProperties("reflected")
                           .Any(indoc =>
                               indoc.AllElseProps<SDataLink>()
                                    .Any(position => position.XOriginal.Name.LocalName == "position")))
                       return new XElement("div", node.Name());

               }
               if (node.Documenttype() == "application/html")
               {
                   factograph.CassetteInfo cassetteInfo = null;
                   string fname = null;
                   try
                   {
                       uri = uri.ToLower();
                       cassetteInfo = StaticModels.cassettesInfo[uri.Substring(0, uri.Length - 15)];
                       fname = uri.Substring(uri.Length - 9);
                   }
                   catch (Exception ex)
                   {
                       LOG.WriteLine(ex.Message + " soran1957.aspx.cs 553 " + uri);
                   }
                   var doc_url = cassetteInfo.url + "originals/" + fname;
                   DirectoryInfo dir = new DirectoryInfo(cassetteInfo.url + "originals/" + uri.Substring(uri.Length - 9, 4));
                   var files = dir.GetFiles(uri.Substring(uri.Length - 4) + ".*");
                   if (files.Length > 0)
                   {
                       var file = files[0].FullName;
                       TextReader tr = new StreamReader(new FileStream(file, FileMode.Open, FileAccess.Read),
                          System.Text.Encoding.GetEncoding("windows-1251"));
                       var html = tr.ReadToEnd();
                       try
                       {
                           var Xhtml = XElement.Parse(html);
                           if (Xhtml.DescendantsAndSelf("body") != null)
                               return new XElement("div", Xhtml.DescendantsAndSelf("body").Elements());
                           return Xhtml;
                       }
                       catch (Exception ex)
                       {
                           try
                           {
                               var XDoc = XDocument.Load(file);
                               if (XDoc.Descendants("body") != null)
                                   return new XElement("div", XDoc.Descendants("body").Elements());
                               return new XElement("div", XDoc.Elements());
                           }
                           catch (Exception ex1)
                           {
                               //return new XElement("TEXTAREA",
                               //   new XAttribute("readonly", "readonly"), 
                               // html);
                               //  return new XElement("irfame", new XAttribute("src", "0047.htm"));
                               while (!html.StartsWith("<body>")) html = html.Substring(1);
                               while (!html.EndsWith("</body>")) html = html.Substring(0, html.Length - 1);
                               //html=html.Replace("<body>", "").Replace("</body>", "");
                               try
                               {
                                   return new XElement("div", XElement.Parse(html).Elements());
                               }
                               catch (Exception ex2)
                               {
                                   LOG.WriteLine("cannot pars xml " + ex2.Message + " " + file);
                                   //   return new XElement("iframe", new XAttribute("src", "cassettes" + cassetteInfo.ShortName+ "/originals/" + fname));
                               }
                           }
                       }
                       return new XElement("iframe",
                       new XAttribute("style", "width:100%;height:100%"),
                       new XAttribute("frameborder", 0),
                           new XAttribute("src", "cassettes/" + cassetteInfo.ShortName + "/originals/" + uri.Substring(uri.Length - 9, 4) + "/" + files[0].Name));

                   }

               }
               if (node.Documenttype() == "text/plain")
               {
                   factograph.CassetteInfo cassetteInfo = null;
                   string fname = null;
                   try
                   {
                       cassetteInfo = StaticModels.cassettesInfo[uri.Substring(0, uri.Length - 15)];
                       fname = uri.Substring(uri.Length - 9);
                   }
                   catch (Exception)
                   { }
                   var doc_path = cassetteInfo.url + "originals/" + fname.Substring(0, 4);
                   var files = (new DirectoryInfo(doc_path)).GetFiles(fname.Substring(5) + ".*");
                   if (files.Length == 1)
                       return new XElement("TEXTAREA", System.IO.File.ReadAllText(files[0].FullName), new XAttribute("rows", 30), new XAttribute("cols", 50));
               }
           }
           return null;
       }
*/

       private static void BuildSearchResults(XElement centralPanel, XElement rightPanel, string searchstring, string sdirection)
       {
           //SGraph.LOG.StartTimer();
           string ss = searchstring.ToLower();
           var cl_def = StaticModels.sDataModel.OntologyModel.GetOntologyNode(sdirection) as SGraph.ROntologyClassDefinition;
           if (cl_def == null) return;
           var naming_def = StaticModels.sDataModel.OntologyModel.GetOntologyNode("naming") as SGraph.ROntologyClassDefinition;
           if (naming_def == null) return;
           if (sdirection == "photo-doc")
           {
               string[] words = ss.Split(new char[] { ' ', ',' }, StringSplitOptions.RemoveEmptyEntries);
               HashSet<SGraph.ROntologyDatatypePropertyDefinition> checkProps = new HashSet<ROntologyDatatypePropertyDefinition>();
               SGraph.ROntologyDatatypePropertyDefinition namedef = StaticModels.sDataModel.OntologyModel.GetOntologyNode(factograph.ONames.TagName) as SGraph.ROntologyDatatypePropertyDefinition;
               SGraph.ROntologyDatatypePropertyDefinition descdef = StaticModels.sDataModel.OntologyModel.GetOntologyNode("description") as SGraph.ROntologyDatatypePropertyDefinition;
               checkProps.Add(namedef);
               checkProps.Add(descdef);
               var qu = StaticModels.sDataModel.Elements()
                   .Where(nd => nd.Definition == cl_def)
                   .Select(nd => new
                   {
                       Nod = nd,
                       Cnt = nd
                           .DirectProperties<SGraph.SDataLink>()
                           .Where(pr =>
                           (ROntologyDatatypePropertyDefinition)pr.Definition == namedef
                           || (ROntologyDatatypePropertyDefinition)pr.Definition == descdef)
                           .Select(pr =>
                           {
                               string phrase = pr.InnerText.ToLower();
                               int cnt = 0;
                               foreach (string word in words) if (phrase.Contains(word)) cnt++;
                               return cnt;
                           }).Sum()
                   })
                   .Where(countednode => countednode.Cnt > 0)
                   .OrderByDescending(countednode => countednode.Cnt)
                   .Select(countednode => countednode.Nod);

               CompositionElement[] celements =
                   qu.Take(80).Select(nd => new CompositionElement(nd)).ToArray();
               //TODO: centralPanel.Add(CompositionElement.ListOfDocs(celements, "pageListDoc", Request["pageListDoc"], Request.Url, null));
           }
           else
           {
               var qu = StaticModels.sDataModel.Elements()
                   .Where(nd =>
                       (nd.Definition == cl_def
                         && nd.Name().ToLower().StartsWith(ss)) //nd.DirectProperties("name").Any(n => ((SGraph.SDataLink)n).InnerText.StartsWith(ss)))
                       ||
                       (nd.Definition == naming_def
                         && nd.DirectObjectLinks("referred-sys").Any(op => op.Target != null && op.Target.Definition == cl_def)
                         && nd.DirectProperties<SGraph.SDataLink>("alias").Any(dp => dp.InnerText.ToLower().StartsWith(ss)))
                         )
                    .Select(objOrNaming => objOrNaming.Definition == naming_def ? objOrNaming.DirectObjectLinks("referred-sys").First().Target : objOrNaming);

               HashSet<SNode> hs = new HashSet<SNode>();
               foreach (var nd in qu) hs.Add(nd);

               //centralPanel.Add(qu.Take(40).OrderBy(nd => nd.Name())
               centralPanel.Add(hs.OrderBy(nd => nd.Name()).Take(40)
                   .Select(nd => new XElement("div",
                       new XElement("a", new XAttribute("href", "?id=" + nd.Id), nd.Name()))));
           }
       }

    }
}
