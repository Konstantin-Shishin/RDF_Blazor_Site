using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Web;
using System.Xml.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
//using Fogid.Cassettes;
using Polar.Cassettes;
using FreeImageAPI;
using ExifLib;

namespace Turgunda7.Controllers
{
    public static class RequestExt
    {
        public static string Params(this Microsoft.AspNetCore.Http.HttpRequest requ, string name)
        {
            string res = requ.Query[name];
            if (res == null && requ.HasFormContentType) res = requ.Form[name];
            return res;
        }
    }
    public class HomeController : Controller
    {
        public ActionResult Error(string mess)
        {
            return View("Error", new Turgunda7.Models.ErrorModel() { mess = mess });
        }
        public ActionResult Info(string mess)
        {
            return View("Info", new Turgunda7.Models.InfoModel() { mess = mess });
        }
        public ActionResult LoadDB()
        {
            Turgunda7.SObjects.Init();
            return Redirect("~/Home/Index");
        }

        public ActionResult Index()
        {
            if (!Turgunda7.SObjects.Initiated) { return Redirect("~/Home/Error?mess=" +
                System.Web.HttpUtility.UrlEncode(Turgunda7.SObjects.Errors)); }
            string nm = Request.Query["name"];
            if (nm == null) nm = "Фонды";
            if (nm != null)
            {
                XElement my_element = SObjects.SearchByName(nm).FirstOrDefault(x =>
                    x.Elements("field").Any(f => f.Attribute("prop").Value == "http://fogid.net/o/name" && f.Value == nm));
                if (my_element != null) return Redirect("~/Home/Portrait?id=" + my_element.Attribute("id").Value);
            }
            return View();
        }
        public ActionResult Search(string searchstring, string type)
        {
            string t = string.IsNullOrEmpty(type) ? null : type;
            var model = new Turgunda7.Models.SearchModel(searchstring, t);
            return View(model);
        }
        public ActionResult Portrait(string id)
        {
            if (id == null) return null;
            Models.PortraitModel pmodel = new Models.PortraitModel(id);
            if (!pmodel.ok) return View("Index");
            return View(pmodel);
        }
        public ActionResult PortraitSpecial(string id)
        {
            Models.PortraitSpecialModel pmodel = new Models.PortraitSpecialModel(id);
            return View(pmodel);
        }

        // ==================== Загрузка данных =====================
        //[HttpPost]
        //public ActionResult Load(string cassetteId, IFormFile file)
        //{  // Этот метод был пробный и сейчас не используется
        //    string filename = file.FileName;
        //    string fpath = @"D:\Home\data\savedfile";
        //    using (System.IO.FileStream fs = new System.IO.FileStream(fpath, System.IO.FileMode.Create, System.IO.FileAccess.Write))
        //    {
        //        file.CopyTo(fs);
        //    }
        //    return View("Portrait", new Models.PortraitModel(cassetteId));
        //}
        [HttpPost("UploadFiles")]
        public IActionResult Post(string id, List<IFormFile> files)
        {
            // Пользователь
            var umodel = new Turgunda7.Models.UserModel(Request);
            string user = umodel.Uuser;
            // Нужно проверить прова
            if (!umodel.IsInRole("admin")) return Error("Файлы может добавлять только администратор");

            var cassetteInfo = SObjects.GetCassetteInfoById(id);
            Cassette cassette = cassetteInfo.cassette;
            if (cassetteInfo == null) return Error("Developers error 287344");
            if (cassetteInfo.cassette.Owner != new Turgunda7.Models.UserModel(Request).Uuser) return Error("Вам не разрешено добавлять файлы в эту кассету"); //new EmptyResult();
            string dirpath = cassetteInfo.url;
            // Ищем подколлекцию upload и если нет - создаем
            var c_item = SObjects.GetItemById(id,
                new XElement("record",
                    new XElement("inverse", new XAttribute("prop", "http://fogid.net/o/in-collection"),
                        new XElement("record", new XAttribute("type", "http://fogid.net/o/collection-member"),
                            new XElement("direct", new XAttribute("prop", "http://fogid.net/o/collection-item"),
                                new XElement("record", new XAttribute("type", "http://fogid.net/o/collection"),
                                    new XElement("field", new XAttribute("prop", "http://fogid.net/o/name"))))))));
            var up = c_item.Elements("inverse")
                .Where(i => i.Attribute("prop").Value == "http://fogid.net/o/in-collection")
                .Select(i => i.Element("record")?.Element("direct"))
                .Where(d => d != null)
                .Select(d => d.Element("record"))
                .FirstOrDefault(r => r.Attribute("type").Value == "http://fogid.net/o/collection" &&
                    r.Elements("field").Select(f => f.Value).FirstOrDefault() == "upload");
            string upload_id;
            if (up == null)
            {
                // коллекция
                string cid = cassette.GenerateNewId();
                XElement el1 = new XElement("{http://fogid.net/o/}collection", new XAttribute(ONames.rdfabout, cid),
                    new XElement("{http://fogid.net/o/}name", "upload"));
                // членство в коллекции
                string mid = cassette.GenerateNewId();
                XElement el2 = new XElement("{http://fogid.net/o/}collection-member", new XAttribute(ONames.rdfabout, mid),
                    new XElement("{http://fogid.net/o/}in-collection", new XAttribute(ONames.rdfresource, id)),
                    new XElement("{http://fogid.net/o/}collection-item", new XAttribute(ONames.rdfresource, cid)));

                cassette.db.Add(el1);
                SObjects.PutItemToDb(el1, false, user);
                cassette.db.Add(el2);
                SObjects.PutItemToDb(el2, false, user);

                upload_id = cid;
            }
            else upload_id = up.Attribute("id").Value;

            bool dbupdated = false;
            foreach (var formFile in files)
            {
                // Сохраним документ в промежуточном месте
                string fullname = formFile.FileName;
                int pos = fullname.LastIndexOfAny(new char[] { '/', '\\' });
                string localname = pos == -1 ? fullname : fullname.Substring(pos + 1);
                string tmpfilepath = dirpath + localname;
                using (var stream = new System.IO.FileStream(tmpfilepath, System.IO.FileMode.Create, System.IO.FileAccess.Write))
                {
                    formFile.CopyTo(stream);
                }
                FileInfo fi = new FileInfo(tmpfilepath);

                CassetteExtension.App_Bin_Path = "C:\\home\\bin\\";
                // Воспользуемся "стандартной" записью файла
                //var elements = CassetteExtension.AddFile1(cassette, fi, id).ToArray();
                var elements = CassetteExtension.AddFile1(cassette, fi, upload_id);
                foreach (var item in elements)
                {
                    var item_corrected = Polar.Cassettes.DocumentStorage.DbAdapter.ConvertXElement(item);
                    SObjects.PutItemToDb(item_corrected, false, user);
                    cassette.db.Add(item_corrected);
                    dbupdated = true;
                }
                //Console.WriteLine("QQQQQQQQQQQQQQQQQQQQQQQ" + elements.Length);
                // Уничтожение времянки
                System.IO.File.Delete(tmpfilepath);
            }
            if (dbupdated) cassette.Save();

            return View("Portrait", new Models.PortraitModel(upload_id));
        }


        private void BuildImageFile(IFormFile formFile, string fpath)
        {
            using (var stream = new System.IO.FileStream(fpath, System.IO.FileMode.Create, System.IO.FileAccess.Write))
            {
                MemoryStream mstream = new MemoryStream();
                formFile.CopyToAsync(mstream);
                using (var original = FreeImageBitmap.FromStream(mstream))
                {
                    original.Save(fpath);
                }
            }
        }
        private async Task BuildImageFile0(IFormFile formFile, string fpath)
        {
            using (var stream = new System.IO.FileStream(fpath, System.IO.FileMode.Create, System.IO.FileAccess.Write))
            {
                await formFile.CopyToAsync(stream);
            }
        }

        //
        // ==================== Редактирование данных =====================
        //
        public ActionResult NewRecord(string searchstring, string type)
        {
            if (type == null) type = "http://fogid.net/o/person";
            string nid = null;
            var umodel = new Turgunda7.Models.UserModel(Request);

            if (type == "http://fogid.net/o/cassette")
            {
                if (!umodel.IsInRole("admin")) return Error("Кассеты может создавать только администратор");
                if (string.IsNullOrEmpty(searchstring)) return Error("Имя кассеты не должно быть пустым");
                Cassette ncass = Cassette.Create(SObjects.newcassettesdirpath + searchstring, (new Turgunda7.Models.UserModel(Request)).Uuser);
                SObjects.xconfig.Add(
                    new XElement("LoadCassette",
                        new XAttribute("write", "yes"),
                        SObjects.newcassettesdirpath + ncass.Name));
                SObjects.SaveConfig();
                SObjects.Init();
                nid = ncass.Name + "_cassetteId";
            }
            else
            {
                nid = SObjects.CreateNewItem(searchstring, type, umodel.Uuser);
            }
            if (nid == null) return Error("Не создан элемент, возможно, у Вас нет полномочий");
            //return RedirectToAction("Portrait", "Home", new { id = nid });
            return Redirect("~/Home/Portrait?id=" + nid);
        }
        [HttpGet]
        public IActionResult ConnectUser(string userlogin, string id)
        {
            var umodel = new Turgunda7.Models.UserModel(Request);
            if (!umodel.IsInRole("admin")) return Error("Кассеты может создавать только администратор");
            Cassette cassette = SObjects.GetCassetteInfoById(id).cassette;
            var elements = cassette.AddNewRdf(userlogin);
            foreach (var item in elements)
            {
                var item_corrected = Polar.Cassettes.DocumentStorage.DbAdapter.ConvertXElement(item);
                SObjects.PutItemToDb(item_corrected, false, (new Turgunda7.Models.UserModel(Request)).Uuser);
                cassette.db.Add(item_corrected);
            }
            cassette.Save();
            SObjects.Init();
            //return View("Info", $"Пользователь {userlogin} подключен к кассете {cass.Name}");
            return Redirect("~/Home/Index");
        }
        public ActionResult AddInvRelation(string bid, string prop, string rtype)
        {
            SObjects.AddInvRelation(bid, prop, rtype, (new Turgunda7.Models.UserModel(Request)).Uuser);
            //string nid = StaticObjects.CreateNewItem(searchstring, type, User.Identity.Name);
            //return RedirectToAction("Portrait", "Home", new { id = bid });
            return Redirect("~/Home/Portrait?id=" + bid);

        }

        [HttpGet]
        public PartialViewResult EditForm(Turgunda7.Models.RecordModel rmodel)
        {
            //string chk = Request.Params["chk"]; // проверка ОДНОЙ введенной связи
            //string ok = Request.Params["ok"]; // фиксация изменений, запоминание в БД
            //string canc = Request.Params["canc"]; // Отмена редактирования
            string chk = Request.Params("chk"); // проверка ОДНОЙ введенной связи
            string ok = Request.Params("ok"); // фиксация изменений, запоминание в БД
            string canc = Request.Params("canc"); // Отмена редактирования

            //string p_4 = Request["p_4"];
            //string p_4_ = Request.QueryString["p_4"];

            if (rmodel.firsttime)
            { // формирование формы редактирования
                rmodel.firsttime = false;
                bool replacemode = true;
                if (rmodel.eid == "create888")
                {
                    string eid = SObjects.AddInvRelation(rmodel.bid, rmodel.iprop, rmodel.etype,
                        (new Turgunda7.Models.UserModel(Request)).Uuser);
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
                    var pair = hrows.Select((hr, ind) => new { hr, ind })
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
                        Turgunda7.Models.SearchModel sm = new Models.SearchModel(rmodel.GetFValue(ind), rmodel.GetTValue(ind));
                        ViewData["searchresults"] = sm.Results;
                        ViewData["searchindex"] = ind;
                    }
                }
                else if (ok != null)
                { // Запоминание
                    // Соберем получившуюся запись
                    XElement record = new XElement(ONames.GetXName(rmodel.etype),
                        new XAttribute(ONames.rdfabout, rmodel.eid),
                        hrows.Select((fd, ind) => new { fd, ind })
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
                    SObjects.PutItemToDb(record, false, (new Turgunda7.Models.UserModel(Request)).Uuser);
                    // Если эта запись является записью типа "DocumentPart", то фиксируем две величины:
                    // ссылку inDocument и идентификатор, имеющийся "за" этой ссылкой
                    if (record.Name.LocalName == "DocumentPart" && record.Name.NamespaceName == "http://fogid.net/o/")
                    {
                        var resource_el = record.Element("{http://fogid.net/o/}inDocument");
                        if (resource_el != null)
                        {
                            string pvalue = resource_el.Attribute(ONames.rdfresource).Value;
                            //HttpContext.Session.SetString("inDocumentId", pvalue);
                            int ind = 0;
                            for (; ind < Turgunda7.Models.RecordModel.nfields; ind++)
                                if (rmodel.GetPValue(ind) == pvalue) break;
                            if (ind < Turgunda7.Models.RecordModel.nfields)
                            {
                                //var s1 = rmodel.GetFValue(ind);
                                //var s2 = rmodel.GetPValue(ind);
                                //var s3 = rmodel.GetTValue(ind);
                                //var s4 = rmodel.GetVValue(ind);

                                //HttpContext.Session.SetString("inDocumentName", rmodel.GetVValue(ind));
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
                    string p_name = System.Web.HttpUtility.UrlDecode(parts[2]);
                    rmodel.SetPValue(ind, p_id);
                    rmodel.SetVValue(ind, p_name);
                    rmodel.CalculateFormat();
                }
                else if (rmodel.command != null && rmodel.command == "SetVariantNew")
                { // Связывание с новым значением
                    string[] parts = rmodel.exchange.Split('|');
                    int ind = Int32.Parse(parts[0]);
                    string p_type = parts[1];
                    string p_name = System.Web.HttpUtility.UrlDecode(parts[2]);
                    string nid = SObjects.CreateNewItem(p_name, p_type, (new Turgunda7.Models.UserModel(Request)).Uuser);
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
        public PartialViewResult SetVariant(Turgunda7.Models.RecordModel rmodel)
        {
            //Turgunda2.Models.RecordModel rmodel = new Models.RecordModel();
            //return PartialView("EditForm", rmodel);
            return EditForm(rmodel);
        }
        public PartialViewResult DeleteRow(string eid)
        {
            SObjects.DeleteItem(eid, (new Turgunda7.Models.UserModel(Request)).Uuser);
            return PartialView();
        }

        // ========================= Конфигурирование ========================
        public IActionResult UserConfiguration()
        {
            var usermodel = new Models.UserModel(Request);
            var pars = Request.Query.ToArray();
            if (pars.Length > 0)
            {
                string acti = null;
                foreach (var p in pars)
                {
                    string pkey = p.Key;
                    if (pkey == "radio") acti = p.Value;
                }
                if (acti != null)
                {
                    if (usermodel.SetActiveCassette(acti)) usermodel.Save();
                }
            }
            List<Models.Conf> list = new List<Models.Conf>();
            foreach (var c in SObjects.GetFogFiles1())
            {
                list.Add(new Models.Conf() { Cass = c.cassette, Editable = c.isEditable, Owner = c.owner,
                    Active = usermodel.ActiveCassette() });

            }

            //Turgunda7.Models.ConfigurationModel cmodel = new Models.ConfigurationModel(fogs);
            return View("UserConfiguration", new Models.UserConfigurationModel() { Confs = list.ToArray() });
        }
        public IActionResult SystemConfiguration()
        {
            var pars = Request.Query.ToArray();
            XElement[] loadcassetteelements = null;
            // i-ый элемент соответствует i-му параметру
            bool[] deletes = null;
            bool[] towrites = null;
            string addcassette = null;
            bool awrite = false;
            foreach (var p in pars)
            {
                if (loadcassetteelements == null)
                {
                    loadcassetteelements = SObjects.xconfig.Elements("LoadCassette").ToArray();
                    deletes = new bool[loadcassetteelements.Length];
                    towrites = new bool[loadcassetteelements.Length];
                }
                var name = p.Key;
                var val = p.Value.FirstOrDefault();
                char c = name[0];
                if (char.IsDigit(c))
                { // Открепление кассеты
                    int nom = Int32.Parse(name);
                    //Console.WriteLine($"============================== deleting cass {nom}");
                    deletes[nom] = true;
                }
                else if (c == 'w')
                {
                    int nom = Int32.Parse(name.Substring(1));
                    //Console.WriteLine($"============================== changing write status cass {nom}");
                    towrites[nom] = true;
                }
                else if (name == "addition")
                {
                    addcassette = val;
                }
                else if (name == "awrite")
                {
                    awrite = true;
                }
            }
            bool changed = false;
            if (loadcassetteelements != null)
            { // зафиксировать результат
                for (int i = 0; i < loadcassetteelements.Length; i++)
                {
                    // коррекция атрибута write
                    XElement el = loadcassetteelements[i];
                    bool towrite = towrites[i];
                    string wr_value = towrite ? "yes" : "no";
                    XAttribute wr_att = el.Attribute("write");
                    if (wr_att == null) { el.Add(new XAttribute("write", wr_value)); changed = true; }
                    else { changed = wr_att.Value != wr_value; wr_att.Value = wr_value; }
                    // уничтожение, если заказано
                    if (deletes[i]) { el.Remove(); changed = true; }
                }
            }
            if (addcassette != null)
            {
                if (Directory.Exists(addcassette) && System.IO.File.Exists(addcassette + "/cassette.finfo"))
                {
                    SObjects.xconfig.Add(
                        new XElement("LoadCassette",
                            new XAttribute("write", awrite ? "yes" : "no"),
                            addcassette));
                    changed = true;
                }
            }
            if (changed)
            {
                SObjects.SaveConfig();
                SObjects.Init();
            }

            return View("SystemConfiguration");
        }

        internal struct Sostoyanie
        {
            internal bool toedit;
            internal string user;
            internal string id;
            internal string mode; // варианты - panel, table
            internal string eid;
            internal string linktype;
            internal string linkname;
            internal int cnt; // для технических аспектов и измерений
            //internal string tilda;
            internal string Code()
            {
                return toedit.ToString() + "\n" +
                    user + "\n" +
                    id + "\n" +
                    mode + "\n" +
                    eid + "\n" +
                    linktype + "\n" +
                    linkname + "\n" +
                    "";
            }
            internal void Decode(string cs)
            {
                var parts = cs.Split('\n');
                toedit = bool.Parse(parts[0]);
                user = parts[1];
                id = parts[2];
                mode = parts[3];
                eid = parts[4];
                linktype = parts[5];
                linkname = parts[6];
            }
            internal void Init()
            {
                toedit = false;
                user = "anonimous";
                id = "no_id";
                mode = "panel";
            }
        }
        internal Sostoyanie sos;
        private string tilda = null;
        // ====================== Экспресс-вариант портрета ========================
        private void LoadSostoyanie()
        {
            if (tilda == null) tilda = HttpContext.Request.PathBase;

            string sostoyanie = HttpContext.Session.GetString("sostoyanie");
            if (sostoyanie == null) sos.Init();
            else sos.Decode(sostoyanie);

        }
        private void SaveSostoyanie()
        {
            string sostoyanie = sos.Code();
            HttpContext.Session.SetString("sostoyanie", sostoyanie);
        }

        public IActionResult P(string id, string tt, string mode, bool? toedit, string eid, string linktype, string linkname)
        {
            LoadSostoyanie();
            ContentResult cr = new ContentResult() { ContentType = "text/html" };

            if (toedit != null) sos.toedit = (bool)toedit;
            if (mode != null) sos.mode = mode;
            if (id != null) sos.id = id;
            else id = sos.id;
            if (eid != null) sos.eid = eid;
            if (linktype != null) sos.linktype = linktype;
            if (linkname != null) sos.linkname = linkname;
            XElement main_panel = null;

            // Параметры быстрого отладочного запуска
            if (id == null || id == "no_id")
            {
                //sos.id = id = "cass_mag02_1011";//"syp2001-p-marchuk_a"; //"cass_mag05_1008"; //"syp2001-p-marchuk_a";
                //sos.toedit = true;
                //sos.user = "mag";
            }

            if (id != null && id != "no_id")
            {
                if (tt == null)
                {
                    XElement xres = Turgunda7.SObjects.GetItemByIdSpecial(id);
                    tt = xres?.Attribute("type").Value;
                }
                if (tt != null)
                {
                    XElement format = TurgundaCommon.ModelCommon.formats.Elements()
                                        .FirstOrDefault(el => el.Attribute("type").Value == tt);
                    main_panel = PortraitPanel(id, format);
                }

            }
            XElement html = PageLayout(SearchPanel(null, null), main_panel);
            SaveSostoyanie();
            cr.Content = "<!DOCTYPE html>\n" + html.ToString(); // (SaveOptions.DisableFormatting);
            return cr;
        }
        private XElement PageLayout(XElement searchPanel, XElement bodyPanel)
        {
            XElement html = new XElement("html",
new XElement("head",
    new XElement("meta", new XAttribute("charset", "utf-8")),
    new XElement("link", new XAttribute("rel", "stylesheet"), new XAttribute("href", tilda + "/css/site.css")),
    new XElement("script", new XAttribute("type", "text/javascript"), new XAttribute("src", tilda+"/s.js"), " "),
    new XElement("style", "table td {border: solid 1px;}")),
new XElement("body",
    new XElement("table", new XAttribute("width", "100%"), new XAttribute("style", "margin-top:10px;"),
        new XElement("tr", new XAttribute("valign", "top"),
            new XElement("td", new XAttribute("rowspan", "2"),
                new XElement("div", new XAttribute("style", "width:120px;"), new XAttribute("align", "center"),
                    new XElement("a", new XAttribute("href", tilda + "/Home/Index"),
                        new XElement("img", new XAttribute("src", tilda + "/images/logo1.jpg"))))),
            new XElement("td",
                new XElement("div", new XAttribute("style", "width:200px;"),
                    new XElement("a", new XAttribute("href", tilda + "/Home/index"), "Начало"))),
            new XElement("td", new XAttribute("width", "100%"),
                new XElement("h2", new XAttribute("style", "color:#5c87b2;padding: 0 0 0 0;"), "Открытый архив СО РАН (редактирующее приложение)")),
            new XElement("td",
                new XElement("div", new XAttribute("style", "width:100px; text-align:right;"),
                    sos.user + " ",
                    new XElement("a", new XAttribute("href", tilda + $"/Home/L"), 
                        sos.toedit ? "вых" : "ред")))),
        new XElement("tr", new XAttribute("valign", "bottom"),
            new XElement("td", new XAttribute("colspan", "3"),
                searchPanel)),
        new XElement("tr", new XAttribute("valign", "bottom"),
            new XElement("td"),
            new XElement("td", new XAttribute("colspan", "3"),
                bodyPanel))

                
)));
            return html;
        }
        public IActionResult Srch(string searchstring, string choosetype)
        {
            LoadSostoyanie();
            ContentResult cr = new ContentResult() { ContentType = "text/html" };
            XElement html = PageLayout(SearchPanel(searchstring, choosetype), null);
            SaveSostoyanie();
            cr.Content = "<!DOCTYPE html>\n" + html.ToString(); // (SaveOptions.DisableFormatting);
            return cr;
        }

// ======================== Генераторы панелей ===========================
        private XElement SearchPanel(string ss, string tt)
        {
            XElement[] xresults = new XElement[0];
            if (ss != null)
            {
                bool checktype = !string.IsNullOrEmpty(tt);
                xresults = Turgunda7.SObjects.SearchByName(ss)
                    .Where(x => !checktype || (checktype && x.Attribute("type").Value == tt))
                    .Take(1000)
                    .ToArray();
            }
            return new XElement("div", new XAttribute("style", "vertical-align:bottom;"),
                new XElement("form", new XAttribute("method", "get"), new XAttribute("action", tilda + "/Home/Srch"),
                    new XAttribute("style", "margin:0;"),
                    new XElement("input", new XAttribute("type", "text"), new XAttribute("name", "searchstring"),
                        (ss == null ? null : new XAttribute("value", ss)),
                        new XAttribute("style", "width:300px;")),
                    new XElement("select", new XAttribute("name", "choosetype"),
                        new XElement("option", " "),
                        TurgundaCommon.ModelCommon.formats.Elements("record")
                        .Select(r =>
                        {
                            string t = r.Attribute("type").Value;
                            return new XElement("option",
                                new XAttribute("value", t),
                                (t == tt ? new XAttribute("selected", "selected") : null),
                                new XText(TurgundaCommon.ModelCommon.OntNames[t]));
                        })),
                    new XElement("input", new XAttribute("type", "submit"), new XAttribute("value", "искать"),
                        new XAttribute("style", "font-size:small;"))), ss == null ? null :
                new XElement("div", 
                (sos.toedit && !string.IsNullOrEmpty(tt))
                ? new XElement("div", 
                    new XElement("a", 
                        new XAttribute("href", tilda + "/Home/N?ss="+ss+"&tt="+tt)), "нов.")  
                : new XElement("div", "найдено: " + xresults.Length)),
                xresults.Select(x => new Tuple<string, XElement>(SObjects.GetField(x, "http://fogid.net/o/name"), x ))
                    .OrderBy(tu => tu.Item1)
                    .Select(tu => new XElement("div",
                        new XElement("a", new XAttribute("href", tilda + "/Home/P?id=" + 
                            tu.Item2.Attribute("id").Value + "&tt=" + tu.Item2.Attribute("type").Value), 
                            tu.Item1)))
                    );
        }
        private XElement PortraitPanel(string id, XElement format)
        {
            var s = sos;
            XElement xresult = Turgunda7.SObjects.GetItemById(id, format);

            string uri = Turgunda7.SObjects.GetField(xresult, "http://fogid.net/o/uri");
            string typ = xresult.Attribute("type").Value;
            XElement doc_content = null;
            if (!string.IsNullOrEmpty(uri) && typ == "http://fogid.net/o/photo-doc")
            {
                doc_content = new XElement("div",
                    new XElement("img",
                        new XAttribute("src", tilda + "/Docs/GetPhoto?s=normal&u=" + uri)));
            }

            XElement htable0 = Htable(Enumerable.Repeat(xresult, 1), format, "d", id, null);
            XElement panel = new XElement("div",
                new XElement("div", TurgundaCommon.ModelCommon.OntNames[format.Attribute("type").Value]),
                doc_content,
                //Htable(Enumerable.Repeat(xresult, 1) , format.ToString(), "d", id, eid, null),
                htable0,
                new XElement("table",
                format.Elements()
                    .Where(f => f.Name == "inverse")
                    .Select(f =>
                    {
                        string prop = f.Attribute("prop").Value;
                        XElement frec = f.Elements("record").FirstOrDefault();
                        string panelview = frec.Attribute("view")?.Value;
                        XElement[] rels = xresult.Elements("inverse")
                            .Where(inv => inv.Attribute("prop").Value == f.Attribute("prop").Value)
                            .Select(inv => inv.Elements().FirstOrDefault())
                            .ToArray();
                        if (rels.Length > 0 || sos.toedit)
                        {
                            return new XElement("tr", new XAttribute("valign", "top"),
                                new XElement("td",
                                    TurgundaCommon.ModelCommon.InvOntNames[prop],
                                    //((panelview == "largeicons")
                                    //? new XElement("div",
                                    //    " ",
                                    //    new XElement("a", new XAttribute("href", tilda + "/Home/P?id=" + id + "&mode=" +
                                    //            (sos.mode == null || sos.mode == "panel" ? "table" : "panel")),
                                    //            (sos.mode == null || sos.mode == "panel" ? "T" : "P")),
                                    //    " ",
                                    //    (sos.toedit ? new XElement("a", new XAttribute("href", tilda + "/Home/NI"), "нов.") : new XElement("div"))
                                    //: null)),
                                    new XElement("div",
                                    panelview == "largeicons"
                                    ? new XElement("a", new XAttribute("href", tilda + "/Home/P?id=" + id + "&mode=" +
                                                (sos.mode == null || sos.mode == "panel" ? "table" : "panel")),
                                                (sos.mode == null || sos.mode == "panel" ? "T" : "P"))
                                    : null,
                                    " ",
                                    new XElement("a", new XAttribute("href",
                                        tilda + $"/Home/R?eid={id}&prop={prop}&tt={frec.Attribute("type").Value}"), "нов."),
                                    null)),
                                new XElement("td",
                                    (panelview != "largeicons" || sos.mode == "table")
                                    ? Htable(rels, frec, null, id, prop)
                                    : new XElement("div", rels.Select(r =>
                                    {
                                        XElement rc = r.Element("direct")?.Element("record");
                                        string src = tilda + "/icons/medium/default_m.jpg";
                                        string href = "";
                                        string name = "noname";
                                        string startdate = Turgunda7.SObjects.GetField(rc, "http://fogid.net/o/from-date");


                                        if (rc != null && rc.Attribute("type") != null)
                                        {
                                            string ty = rc.Attribute("type").Value;
                                            string ur = Turgunda7.SObjects.GetField(rc, "http://fogid.net/o/uri");
                                            name = Turgunda7.SObjects.GetField(rc, "http://fogid.net/o/name");
                                            if (ty == "http://fogid.net/o/photo-doc" && ur != null)
                                            {
                                                src = tilda + "/Docs/GetPhoto?s=small&u=" + ur;
                                            }
                                            else if (TurgundaCommon.ModelCommon.icons.ContainsKey(ty))
                                            {
                                                src = tilda + "/icons/medium/" + TurgundaCommon.ModelCommon.icons[ty];
                                            }
                                            href = tilda + "/Home/P?id=" + rc.Attribute("id").Value + "&tt=" + ty;
                                        }
                                        return new XElement("div", new XAttribute("class", "brick"),
                                            new XElement("div",
                                                new XElement("a", new XAttribute("href", href),
                                                    new XElement("img", new XAttribute("src", src)), new XElement("href", ""))),
                                            new XElement("div", new XAttribute("style", "font-size:smaller;"), startdate),
                                            null);
                                    }))));
                        }
                        else return null;
                    })
                ));
            return panel;
        }
        /// <summary>
        /// Построение таблицы и конкретных рядков таблицы
        /// </summary>
        /// <param name="xrecs">массив записей</param>
        /// <param name="format">формат элементов массива</param>
        /// <param name="cla">класс для визуализации</param>
        /// <param name="eid">выделенный рядок</param>
        /// <param name="iprop">обратное свойтво верхнего уровня</param>
        /// <returns>Построенный HTML таблицы</returns>
        private XElement Htable(IEnumerable<XElement> xrecs, XElement format, string cla, string bid, string iprop)
        {
            //XElement format = XElement.Parse(forma_t);
            XElement[] xx = xrecs.ToArray();
            return new XElement("table", 
                new XElement("thead",
                    new XElement("tr",
                        format.Elements()
                        .Where(f => f.Name == "field" || f.Name == "direct")
                        .Select(f => new XElement("th",
                            TurgundaCommon.ModelCommon.OntNames[f.Attribute("prop").Value]))
                    )),
                new XElement("tbody",
                    xrecs.Select(r =>
                    {
                        XElement row = new XElement("tr");
                        if (r.Attribute("id").Value == sos.eid)
                        { // форма редактирования
                            var fe_arr = format.Elements()
                                .Where(f => f.Name == "field" || f.Name == "direct")
                                .ToArray(); // Можно и без преобразования в массив
// === вычисление таблицы редактирования
XElement etable = new XElement("div", new XAttribute("style", "border-width:thin; border-style:dashed; border-color:Black;"),
    new XElement("form", new XAttribute("method", "post"), new XAttribute("action", tilda+"/Home/E"), 
    new XElement("table", new XAttribute("width", "100%"),
        new XElement("input", new XAttribute("type", "hidden"), new XAttribute("name", "eid"), new XAttribute("value", sos.eid)),
        new XElement("input", new XAttribute("type", "hidden"), new XAttribute("name", "bid"), new XAttribute("value", bid)),
        string.IsNullOrEmpty(iprop) ? null: new XElement("input", new XAttribute("type", "hidden"), new XAttribute("name", "prop"), new XAttribute("value", iprop)),
        new XElement("input", new XAttribute("type", "hidden"), new XAttribute("name", "rtype"), new XAttribute("value", r.Attribute("type").Value)),
        fe_arr.Select<XElement, XElement>(f =>
        {
            string prop = f.Attribute("prop").Value;
            string direction = f.Name.LocalName;
            XElement cell = null;

            if (direction == "field")
            {
                if (prop == "http://fogid.net/o/description" || prop == "http://fogid.net/o/doc-content")
                {
                    cell = new XElement("textarea", new XAttribute("rows", "5"),
                        new XAttribute("style", "width: 400px;"),
                        new XAttribute("name", prop),
                        Turgunda7.SObjects.GetField(r, prop));
                }
                else
                {
                    // Обработка перечисления
                    string ftype = f.Attribute("type")?.Value;
                    if (ftype != null)
                    {
                        var variants = TurgundaCommon.ModelCommon.GetEnumStates(ftype)
                            .ToArray();
                        cell = new XElement("select", new XAttribute("name", prop),
                            new XElement("option", new XAttribute("value", " ")),
                            variants.Select(variant => new XElement("option", new XAttribute("value", variant.Attribute("value").Value),
                                variant.Attribute("value").Value == Turgunda7.SObjects.GetField(r, prop) ? new XAttribute("selected", "selected") : null,
                                variant.Value)),
                            null);
                    }
                    else
                    {
                        cell = new XElement("input", new XAttribute("type", "text"),
                            new XAttribute("style", "width: 400px;"),
                            new XAttribute("name", prop),
                            new XAttribute("value", Turgunda7.SObjects.GetField(r, prop)));
                    }
                }

            }
            else if (direction == "direct")
            {
                var rec = r.Elements("direct").FirstOrDefault(d => d.Attribute("prop").Value == prop)?
                    .Element("record");
                bool tolink = true;
                if (rec != null)
                {
                    cell = new XElement("a", new XAttribute("href", tilda+"/Home/P?id=" + rec.Attribute("id").Value),
                        Turgunda7.SObjects.GetField(rec, "http://fogid.net/o/name"));
                }
                else if (tolink)
                {
                    IEnumerable<XElement> samples = Enumerable.Empty<XElement>();
                    if (!string.IsNullOrEmpty(sos.linkname)) samples = Turgunda7.SObjects.SearchByName(sos.linkname)
                        .Where(rr => rr.Attribute("type").Value == sos.linktype);
                    cell = new XElement("div", new XElement("div",
                        new XElement("select", new XAttribute("name", "linktype"),
                            f.Elements("record").Select(re =>
                            {
                                var t = re.Attribute("type").Value;
                            return new XElement("option", new XAttribute("value", t),
                                sos.linktype == t ? new XAttribute("selected", "selected") : null,
                                TurgundaCommon.ModelCommon.OntNames[t]);
                            })),
                        new XElement("input", new XAttribute("type", "text"), new XAttribute("name", "linkname"),
                            !string.IsNullOrEmpty(sos.linkname) ? new XAttribute("value", sos.linkname) : null),
                        string.IsNullOrEmpty(sos.linkname) ? null :
                        new XElement("div",
                            new XElement("div", 
                                new XElement("a", new XAttribute("href", tilda + 
                                    $"/Home/NI?ss={sos.linkname}&tt={sos.linktype}&prop={prop}&eid={sos.eid}&rtype={r.Attribute("type").Value}"), "нов."),
                                samples.Select(rr => new XElement("div", 
                                    new XElement("a", new XAttribute("href", tilda+ 
                                    $"/Home/I?prop={prop}&eid={sos.eid}&rtype={r.Attribute("type").Value}&resource={rr.Attribute("id").Value}"), 
                                        Turgunda7.SObjects.GetField(rr, "http://fogid.net/o/name")))))),
                            
                        null));
                    tolink = false;
                }
            }

            return new XElement("tr",
            new XElement("td", TurgundaCommon.ModelCommon.OntNames[prop]),
            new XElement("td", cell),
            null);
        }),
        new XElement("tr",
            new XElement("td"),
            new XElement("td", 
                new XElement("input", new XAttribute("type", "submit"), 
                    new XAttribute("name", "comm_chk"), new XAttribute("value", "пров.")),
                new XElement("input", new XAttribute("type", "submit"),
                    new XAttribute("name", "comm_save"), new XAttribute("value", "сохр.")),
                new XElement("input", new XAttribute("type", "submit"),
                    new XAttribute("name", "comm_canc"), new XAttribute("value", "отм.")),
                null))
        )));
// ===
                            row.Add(
                                new XElement("td", new XAttribute("colspan", fe_arr.Count()+1), etable));

                        }
                        else
                        { // форма рядка
                            row.Add(
                                format.Elements()
                                .Where(f => f.Name == "field" || f.Name == "direct")
                                .Select(f =>
                                {
                                    XElement xcontent = null;
                                    if (f.Name == "field")
                                    {
                                        string s = Turgunda7.SObjects.GetField(r, f.Attribute("prop").Value);
                                        string ftype = f.Attribute("type")?.Value;
                                        if (ftype != null)
                                        {
                                            string rustext = TurgundaCommon.ModelCommon.GetEnumStateLabel(ftype, s);
                                            if (rustext != null) s = rustext;
                                        }
                                        xcontent = new XElement("span", s);
                                    }
                                    else if (f.Name == "direct")
                                    {
                                        xcontent = new XElement("a", new XAttribute("href", tilda + "/Home/P?id="
                                            + r.Element("direct")?.Element("record")?.Attribute("id")?.Value + "&tt="
                                            + r.Element("direct")?.Element("record")?.Attribute("type")?.Value),
                                            Turgunda7.SObjects.GetField(r.Element("direct")?.Element("record"), "http://fogid.net/o/name"));

                                    }
                                    return new XElement("td", cla == null ? null : new XAttribute("class", cla), xcontent);
                                }
                                    ),
                                sos.toedit ?
                                new XElement("td",
                                    new XElement("a", new XAttribute("href", tilda + "/Home/P?eid="+r.Attribute("id").Value), "ред"), " ",
                                    r.Elements("inverse").Count() > 0 ? null :
                                    new XElement("a", new XAttribute("href", tilda + "/Home/D?did=" + r.Attribute("id").Value),
                                        //new XAttribute("onclick_", "look(); return false;"), "x")) :
                                        new XAttribute("onclick", "look(); "), "x")) :
                                null);
                        }
                        return row;
                    }
                )));
        }
        // Login
        //[HttpPost]
        public IActionResult L(string login, string pass, string newuser) 
        {
            LoadSostoyanie();
            // Если нажали в режиме редактирования, значит это выход и надо сделать выход из режима
            if (sos.toedit)
            {
                sos.toedit = false;
                sos.user = null;
                SaveSostoyanie();
                //return P(sos.id, null, null, null, null);
                return new RedirectResult(tilda + "/Home/P?id=" + sos.id);
            }
            // здесь могут быть уже сформированные данные, если они правильные, надо просто переключить режим
            if (login != null)
            {
                var candidate = SObjects.accounts.Elements("account")
                    .FirstOrDefault(a => a.Attribute("login").Value == login);
                // правильные данные: либо логин и пасс совпадают, либо кандидат пустой, пасс непустой, и newuser
                if (candidate?.Attribute("pass").Value == pass || (candidate == null && pass != null && newuser == "on"))
                {
                    // Фиксируем изменения
                    sos.toedit = true;
                    sos.user = login;
                    if (newuser == "on")
                    {
                        SObjects.accounts.Add(new XElement("account",
                            new XAttribute("login", login), new XAttribute("pass", pass)));
                        SObjects.SaveAccounts();
                    }
                    SaveSostoyanie();
                    //return P(sos.id, null, null, null, null);
                    return new RedirectResult(tilda + "/Home/P?id=" + sos.id);
                }
                // А остальное - ошибочные данные, нужно повторять ввод 
            }
            ContentResult cr = new ContentResult() { ContentType = "text/html" };

            XElement html = new XElement("html",
new XElement("head",
new XElement("meta", new XAttribute("charset", "utf-8")),
new XElement("link", new XAttribute("rel", "stylesheet"), new XAttribute("href", tilda + "/css/site.css"))),
new XElement("body", new XAttribute("style", "margin: 20px;"),
new XElement("h2", "Для редактирования, надо авторизоваться"),
new XElement("form", new XAttribute("method", "get"), new XAttribute("action", ""),
    new XElement("table",
        new XElement("tr",
            new XElement("td", "код пользователя"),
            new XElement("td", new XElement("input", new XAttribute("type", "text"), new XAttribute("name", "login")))),
        new XElement("tr",
            new XElement("td", "пароль"),
            new XElement("td", new XElement("input", new XAttribute("type", "text"), new XAttribute("name", "pass")))),
        new XElement("tr",
            new XElement("td", "новый пользователь?"),
            new XElement("td", new XElement("input", new XAttribute("type", "checkbox"), new XAttribute("name", "newuser")))),
        new XElement("tr",
            new XElement("td"),
            new XElement("td", new XElement("input", new XAttribute("type", "submit")))),
        null))));

            cr.Content = "<!DOCTYPE html>\n" + html.ToString(); // (SaveOptions.DisableFormatting);
            return cr;

        }
        // New record
        public IActionResult N(string ss, string tt, string prop)
        {
            if (tt == null) tt = "http://fogid.net/o/person";
            string nid = null;
            var umodel = new Turgunda7.Models.UserModel(Request);
            LoadSostoyanie();

            if (tt == "http://fogid.net/o/cassette")
            {
                if (!umodel.IsInRole("admin")) return Error("Кассеты может создавать только администратор");
                if (string.IsNullOrEmpty(ss)) return Error("Имя кассеты не должно быть пустым");
                Cassette ncass = Cassette.Create(SObjects.newcassettesdirpath + ss, (new Turgunda7.Models.UserModel(Request)).Uuser);
                SObjects.xconfig.Add(
                    new XElement("LoadCassette",
                        new XAttribute("write", "yes"),
                        SObjects.newcassettesdirpath + ncass.Name));
                SObjects.SaveConfig();
                SObjects.Init();
                nid = ncass.Name + "_cassetteId";
            }
            else
            {
                //nid = SObjects.CreateNewItem(searchstring, type, umodel.Uuser);
                nid = Turgunda7.SObjects.CreateNewItem(ss, tt, sos.user);
            }
            if (nid == null) return Error("Не создан элемент, возможно, у Вас нет полномочий");

            return new RedirectResult(tilda + "/Home/P?id=" + sos.id);
        }
        // New Relation
        public IActionResult R(string eid, string prop, string tt)
        {
            LoadSostoyanie();
            string id = Turgunda7.SObjects.AddInvRelation(eid, prop, tt, sos.user);
            return new RedirectResult(tilda + "/Home/P?id=" + sos.id); // идентификатор может быть eid
        }
        // Проставить прямую ссылку prop записи id (типа tt) на объект obj_id 
        public IActionResult I(string prop, string eid, string rtype, string resource)
        {
            LoadSostoyanie();
            XElement format = TurgundaCommon.ModelCommon.EditRecords.Elements("record")
                .FirstOrDefault(f => f.Attribute("type").Value == rtype);
            //var query = SObjects.GetItemByIdSpecial(id);
            XElement record = SObjects.GetItemById(eid, format);
            int rslash = rtype.LastIndexOf('/');
            XName rtag = XName.Get(rtype.Substring(rslash + 1), rtype.Substring(0, rslash + 1));
            int pslash = prop.LastIndexOf('/');
            XName ptag = XName.Get(prop.Substring(pslash + 1), prop.Substring(0, pslash + 1));

            XElement rec = new XElement(rtag, new XAttribute(ONames.rdfabout, eid),
                record.Elements().Select(r =>
                {   // Поля и директы
                    string pro = r.Attribute("prop").Value;
                    int ps = pro.LastIndexOf('/');
                    XName ptg = XName.Get(pro.Substring(ps + 1), prop.Substring(0, ps + 1));
                    if (r.Name == "field")
                    {
                        return new XElement(ptg, r.Value);
                    }
                    else if (r.Name == "direct")
                    {
                        return new XElement(ptg, new XAttribute(ONames.rdfresource, r.Element("record").Attribute("id").Value));
                    }
                    return (XElement)null;
                }),
                new XElement(ptag, new XAttribute(ONames.rdfresource, resource)),
                null);

            SObjects.PutItemToDb(rec, false, sos.user);
            sos.eid = null;
            sos.linkname = null;
            SaveSostoyanie();

            return new RedirectResult(tilda + "/Home/P?id=" + sos.id);
        }
        // Создать новый объект и проставить прямую ссылку prop записи id (типа rtype) на него 
        public IActionResult NI(string ss, string tt, string prop, string eid, string rtype)
        {
            LoadSostoyanie();
            string idd = Turgunda7.SObjects.CreateNewItem(ss, tt, sos.user);
            XElement format = TurgundaCommon.ModelCommon.EditRecords.Elements("record")
                .FirstOrDefault(f => f.Attribute("type").Value == rtype);
            //var query = SObjects.GetItemByIdSpecial(id);
            XElement record = SObjects.GetItemById(eid, format);
            int rslash = rtype.LastIndexOf('/');
            XName rtag = XName.Get(rtype.Substring(rslash + 1), rtype.Substring(0, rslash + 1));
            int pslash = prop.LastIndexOf('/');
            XName ptag = XName.Get(prop.Substring(pslash + 1), prop.Substring(0, pslash + 1));

            XElement rec = new XElement(rtag, new XAttribute(ONames.rdfabout, eid),
                record.Elements().Select(r =>
                {   // Поля и директы
                    string pro = r.Attribute("prop").Value;
                    int ps = pro.LastIndexOf('/');
                    XName ptg = XName.Get(pro.Substring(ps + 1), prop.Substring(0, ps + 1));
                    if (r.Name == "field")
                    {
                        return new XElement(ptg, r.Value);
                    }
                    else if (r.Name == "direct")
                    {
                        return new XElement(ptg, new XAttribute(ONames.rdfresource, r.Element("record").Attribute("id").Value));
                    }
                    return (XElement)null;
                }),
                new XElement(ptag, new XAttribute(ONames.rdfresource, idd)),
                null);

            SObjects.PutItemToDb(rec, false, sos.user);
            sos.eid = null;
            sos.linkname = null;
            SaveSostoyanie();

            return new RedirectResult(tilda + "/Home/P?id=" + sos.id);
        }

        // Delete
        public IActionResult D(string did)
        {
            LoadSostoyanie();
            SObjects.DeleteItem(did, sos.user);
            //return P(sos.id, null, null, null, null);
            return new RedirectResult(tilda + "/Home/P?id=" + sos.id);
        }
        // EditRecord
        [HttpPost]
        public IActionResult E(string comm_chk, string comm_save, string comm_canc,
            string eid, string bid, string prop, string rtype, string linktype, string linkname)
        {
            //var v = HttpContext.Request.Form["comm_canc"];
            //HttpContext.Request.Form.Where(kv => kv.Key[0] == '_');
            LoadSostoyanie();
            sos.linktype = linktype;
            sos.linkname = linkname;
            if (comm_canc != null)
            {
                sos.eid = null;
                SaveSostoyanie();
                return new RedirectResult(tilda + "/Home/P?id=" + sos.id);
            }

            if (comm_save != null)
            {   // Надо обрать запись из того, что пришло. Предполагается, что были посланы: 
                // eid - идентификатор записи
                // bid - вышестоящая запись
                // prop - вышестоящее отношение
                // Тип записи берется из формата
                // Вычисляю Name записи
                int slash = rtype.LastIndexOf('/');
                XName rectag = XName.Get(rtype.Substring(slash + 1), rtype.Substring(0, slash + 1));
                //var rname = XName.Get("person", "http://fogid.net/o/");
                XElement record = new XElement(rectag, new XAttribute(ONames.rdfabout, eid));
                XElement format = TurgundaCommon.ModelCommon.EditRecords.Elements("record")
                    .FirstOrDefault(f => f.Attribute("type").Value == rtype);
                // Если нет, то что-то странное
                if (format == null) throw new Exception("Err: 2482978");
                // Сканирую формат, собираю поля и прямые ссылки
                foreach (var f in format.Elements().Where(f => f.Name == "field" || f.Name == "direct"))
                {
                    string pr = f.Attribute("prop").Value;
                    // достать значение
                    string val = HttpContext.Request.Form[pr];
                    if (string.IsNullOrEmpty(val)) continue;
                    int lastslash = pr.LastIndexOf('/');
                    XElement elem = new XElement(XName.Get(pr.Substring(lastslash + 1), pr.Substring(0, lastslash + 1)));
                    if (f.Name == "field")
                    {
                        elem.Value = val;
                        if (TurgundaCommon.ModelCommon.IsTextField(pr))
                        {
                            elem.Add(new XAttribute(ONames.xmllang, "ru"));
                        }
                    }
                    else
                    {
                        elem.Add(new XAttribute(ONames.rdfresource, val));
                    }
                    record.Add(elem);
                }
                SObjects.PutItemToDb(record, false, sos.user);
                sos.eid = null;
                SaveSostoyanie();
                return new RedirectResult(tilda + "/Home/P?id=" + sos.id);
            }
            else if (comm_chk != null) // Это вариант проверки формы
            {
                // Нас в проверке интересует запрос на объект ссылки. Его и поищем
                // должны быть установлены linktype и linkname
                if (sos.linktype != null && sos.linkname != null)
                {
                    var recs = Turgunda7.SObjects.SearchByName(linkname)
                        .Where(r => r.Attribute("type").Value == linktype);
                }
                // Всегда возвращается в редактирование
                //sos.Code();
                return P(sos.id, null, sos.mode, true, eid, linktype, linkname);
            }
            //string eid = HttpContext.Request.Form["eid"];
            else return new RedirectResult(tilda + "/Home/P?id=" + sos.id);
        }
    }
}