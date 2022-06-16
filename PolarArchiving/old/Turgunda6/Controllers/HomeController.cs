using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Xml.Linq;
//using Fogid.Cassettes;
using Polar.Cassettes;

namespace Turgunda6.Controllers
{
    public class HomeController : Controller
    {
        public ActionResult Index()
        {
            return View();
        }
        public ActionResult Search(string searchstring, string type)
        {
            string t = string.IsNullOrEmpty(type) ? null : type;
            var model = new Turgunda6.Models.SearchModel(searchstring, t);
            return View(model);
        }
        public ActionResult Portrait(string id)
        {
            if (id == null) return null;
            Models.PortraitModel pmodel = new Models.PortraitModel(id);
            return View(pmodel);
        }
        public ActionResult PortraitSpecial(string id)
        {
            Models.PortraitSpecialModel pmodel = new Models.PortraitSpecialModel(id);
            return View(pmodel);
        }

        //
        // ==================== Редактирование данных =====================
        //
        public ActionResult NewRecord(string searchstring, string type)
        {
            if (type == null) type = "http://fogid.net/o/person";
            string nid = Turgunda7.SObjects.CreateNewItem(searchstring, type, (new Turgunda6.Models.UserModel(Request)).Uuser);
            return RedirectToAction("Portrait", "Home", new { id = nid });
        }
        public ActionResult AddInvRelation(string bid, string prop, string rtype)
        {
            Turgunda7.SObjects.AddInvRelation(bid, prop, rtype, (new Turgunda6.Models.UserModel(Request)).Uuser);
            //string nid = StaticObjects.CreateNewItem(searchstring, type, User.Identity.Name);
            return RedirectToAction("Portrait", "Home", new { id = bid });
        }

        //[HttpPost]
        public PartialViewResult EditForm(Turgunda6.Models.RecordModel rmodel)
        {
            string chk = Request.Params["chk"]; // проверка ОДНОЙ введенной связи
            string ok = Request.Params["ok"]; // фиксация изменений, запоминание в БД
            string canc = Request.Params["canc"]; // Отмена редактирования

            //string p_4 = Request["p_4"];
            //string p_4_ = Request.QueryString["p_4"];

            if (rmodel.firsttime)
            { // формирование формы редактирования
                rmodel.firsttime = false;
                bool replacemode = true;
                if (rmodel.eid == "create888")
                {
                    string eid = Turgunda7.SObjects.AddInvRelation(rmodel.bid, rmodel.iprop, rmodel.etype,
                        (new Turgunda6.Models.UserModel(Request)).Uuser);
                    rmodel.eid = eid;
                    replacemode = false;
                }
                rmodel.LoadFromDb();
                rmodel.MakeLocalRecord();
                //XElement[] arr = rmodel.GetHeaderFlow().ToArray();
                //rmodel.MakeXResult();
                if (!replacemode) ViewData["insertnewrelation"] = rmodel.eid;
            }
            else if (canc != null)
            {
                rmodel.LoadFromDb();
                rmodel.MakeLocalRecord();
                return PartialView("EditFormFinal", rmodel);
            }
            else
            { // Собирание данных из реквеста
                XElement format = rmodel.CalculateFormat();
                XElement[] hrows = rmodel.GetHeaderFlow().ToArray();
                if (chk != null)
                {   // Проверка. Находим первый ряд такой, что: а) это прямое отношение, б) набран текст и есть тип, в) нет ссылки. 
                    // Делаем поисковый запрос через SearchModel. Результаты SearchResult[] помещаем в ViewData под именем searchresults,
                    // а в ViewData["searchindex"] поместим индекс
                    var pair = hrows.Select((hr, ind) => new { hr = hr, ind = ind })
                        .FirstOrDefault(hrind =>
                        {
                            var hr = hrind.hr;
                            if (hr.Name != "d") return false;
                            var ind = hrind.ind;
                            if (string.IsNullOrEmpty(rmodel.GetFValue(ind)) || string.IsNullOrEmpty(rmodel.GetTValue(ind))) return false;
                            if (!string.IsNullOrEmpty(rmodel.GetPValue(ind))) return false;
                            return true;
                        });
                    if (pair != null)
                    {
                        int ind = pair.ind;
                        // Ничего проверять не буду
                        Turgunda6.Models.SearchModel sm = new Models.SearchModel(rmodel.GetFValue(ind), rmodel.GetTValue(ind));
                        ViewData["searchresults"] = sm.Results;
                        ViewData["searchindex"] = ind;
                    }
                }
                else if (ok != null)
                { // Запоминание
                    // Соберем получившуюся запись
                    XElement record = new XElement(ONames.GetXName(rmodel.etype),
                        new XAttribute(ONames.rdfabout, rmodel.eid),
                        hrows.Select((fd, ind) => new { fd = fd, ind = ind })
                        .Select(fdind =>
                        {
                            XElement fd = fdind.fd;
                            int ind = fdind.ind;
                            XName xprop = ONames.GetXName(fd.Attribute("prop").Value);
                            if (fd.Name == "f")
                            {
                                string value = rmodel.GetFValue(ind);
                                if (true || !string.IsNullOrEmpty(value)) // Эта проверка мешает(ла) уничтожению полей
                                {
                                    // Надо определить еще нужен ли язык и какой
                                    bool istext = TurgundaCommon.ModelCommon.IsTextField(fd.Attribute("prop").Value);
                                    return new XElement(xprop, rmodel.GetFValue(ind),
                                        istext ? new XAttribute(ONames.xmllang, rmodel.lang) : null);
                                }
                            }
                            else if (fd.Name == "d")
                            {
                                string pvalue = rmodel.GetPValue(ind);
                                if (!string.IsNullOrEmpty(pvalue))
                                    return new XElement(xprop,
                                    new XAttribute(ONames.rdfresource, rmodel.GetPValue(ind)));
                            }
                            return (XElement)null;
                        }));
                    // Пошлем эту запись на изменение
                    Turgunda7.SObjects.PutItemToDb(record, false, (new Turgunda6.Models.UserModel(Request)).Uuser);
                    // Если эта запись является записью типа "DocumentPart", то фиксируем две величины:
                    // ссылку inDocument и идентификатор, имеющийся "за" этой ссылкой
                    if (record.Name.LocalName == "DocumentPart" && record.Name.NamespaceName == "http://fogid.net/o/")
                    {
                        var resource_el = record.Element("{http://fogid.net/o/}inDocument");
                        if (resource_el != null)
                        {
                            string pvalue = resource_el.Attribute(ONames.rdfresource).Value;
                            Session["inDocumentId"] = pvalue;
                            int ind = 0;
                            for (; ind < Turgunda6.Models.RecordModel.nfields; ind++)
                                if (rmodel.GetPValue(ind) == pvalue) break;
                            if (ind < Turgunda6.Models.RecordModel.nfields)
                            {
                                //var s1 = rmodel.GetFValue(ind);
                                //var s2 = rmodel.GetPValue(ind);
                                //var s3 = rmodel.GetTValue(ind);
                                //var s4 = rmodel.GetVValue(ind);

                                Session["inDocumentName"] = rmodel.GetVValue(ind);
                            }
                        }
                    }

                    return PartialView("EditFormFinal", rmodel);
                }
                else if (rmodel.command != null && rmodel.command == "SetVariant")
                { // Выбор варианта значения для связывания
                    string[] parts = rmodel.exchange.Split('|');
                    int ind = Int32.Parse(parts[0]);
                    string p_id = parts[1];
                    string p_name = Server.UrlDecode(parts[2]);
                    rmodel.SetPValue(ind, p_id);
                    rmodel.SetVValue(ind, p_name);
                    rmodel.CalculateFormat();
                }
                else if (rmodel.command != null && rmodel.command == "SetVariantNew")
                { // Связывание с новым значением
                    string[] parts = rmodel.exchange.Split('|');
                    int ind = Int32.Parse(parts[0]);
                    string p_type = parts[1];
                    string p_name = Server.UrlDecode(parts[2]);
                    string nid = Turgunda7.SObjects.CreateNewItem(p_name, p_type, (new Turgunda6.Models.UserModel(Request)).Uuser);
                    rmodel.SetPValue(ind, nid);
                    rmodel.SetVValue(ind, p_name);
                    rmodel.CalculateFormat();
                }
                else
                { // Остальное

                }
            }
            return PartialView("EditForm", rmodel);
        }
        //[HttpPost]
        public PartialViewResult SetVariant(Turgunda6.Models.RecordModel rmodel)
        {
            //Turgunda2.Models.RecordModel rmodel = new Models.RecordModel();
            //return PartialView("EditForm", rmodel);
            return EditForm(rmodel);
        }
        public PartialViewResult DeleteRow(string eid)
        {
            Turgunda7.SObjects.DeleteItem(eid, (new Turgunda6.Models.UserModel(Request)).Uuser);
            return PartialView();
        }

    }
}