using FactographyView.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using System.Xml.Linq;
using RDFEngine;

namespace FactographyView.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;

        public HomeController(ILogger<HomeController> logger)
        {
            _logger = logger;
        }
        public IActionResult Index(string id)
        {
            return View("Index", id);
        }

        public IActionResult Person(string id)
        {
            var model = Infobase.engine.GetRRecord(id);
            return View("Person", model);
        }
        public IActionResult Portrait(string id)
        {
            var model = Infobase.engine.GetRRecord(id);
            return View("Portrait", model);
        }
        //public IActionResult ShowTree(string id)
        //{
        //    var model = Infobase.engine.GetRTree(id, 2, null);
        //    return View("ShowTree", model);
        //}

        private static string BuildPortraitText(string id, int level, string forbidden)
        {
            var rec = Infobase.engine.GetRRecord(id);
            if (rec == null) return null;
            string result = "{Id:" + rec.Id + ", Tp:" + rec.Tp + ", " +
                rec.Props.Select<RDFEngine.RProperty, string>(p =>
                {
                    if (p is RDFEngine.RField) return p.Prop + ":" + ((RDFEngine.RField)p).Value;
                    else if (level > 0 && p is RDFEngine.RLink && p.Prop != forbidden) return p.Prop + ":" + BuildPortraitText(((RDFEngine.RLink)p).Resource, 0, null);
                    else if (level > 1 && p is RDFEngine.RInverseLink) return p.Prop + ":" + BuildPortraitText(((RDFEngine.RInverseLink)p).Source, 1, p.Prop);
                    return null;
                }).Where(s => s != null).Aggregate((a, s) => a + ", " + s) + "}";
            return result;
        }
        public IActionResult Portrait1(string id)
        {
            var model = BuildPortraitText(id, 2, null);
            return View("Portrait1", model);
        }

        private static RRecord BuildPortrait(string id)
        {
            return BuPo(id, 2, null);
        }
        private static RRecord BuPo(string id, int level, string forbidden)
        {
            var rec = Infobase.engine.GetRRecord(id);
            if (rec == null) return null;
            RRecord result_rec = new RRecord()
            {
                Id = rec.Id,
                Tp = rec.Tp,
                Props = rec.Props.Select<RProperty, RProperty>(p => 
                {
                    if (p is RField)
                        return new RField() { Prop = p.Prop, Value = ((RField)p).Value };
                    else if (level > 0 && p is RLink && p.Prop != forbidden)
                        return new RDirect() { Prop = p.Prop, DRec = BuPo(((RLink)p).Resource, 0, null) };
                    else if (level > 1 && p is RInverseLink)
                        return new RInverse() { Prop = p.Prop, IRec = BuPo(((RInverseLink)p).Source, 1, p.Prop) };
                    return null;
                }).Where(p => p != null).ToArray()
            };
            return result_rec;
        }

        RRecord erecord = new RRecord()
        {
            Id = "o19302",
            Tp = "org-sys",
            Props = new RProperty[3]
        };
        public IActionResult Portrait2(string id)
        {
            var model = BuildPortrait(id);
            if (model == null) return View("Index");
            return View("Portrait2", model);
        }


        /// <summary>
        /// Класс состоит из элементов и структур, требуемых для отрисовки портрета
        /// Логическая структура отрисовки: тип, идентификатор, name, uri
        /// Потом - массив полей и прямых свойств
        /// Потом - массив групп обратных свойств. Каждая группа определяется именем предиката ОС и типом записи ОС
        /// </summary>
        public class PModel
        {
            public string Id, Tp, name, uri;
            public RProperty[] row;
            public RInverse[] inv;
        }
        private PModel MkPModel(RRecord erec)
        {
            PModel pm = new PModel() { Id = erec.Id, Tp = erec.Tp };
            List<RDFEngine.RProperty> rowlist = new List<RDFEngine.RProperty>();
            List<RInverse> inverselist = new List<RInverse>();
            foreach (var p in erec.Props)
            {
                if (p is RField)
                {
                    RField f = (RField)p;
                    if (f.Prop == "name") pm.name = f.Value;
                    else if (f.Prop == "uri") pm.uri = f.Value;
                    else
                    {
                        rowlist.Add(new RField() { Prop = f.Prop, Value = f.Value });
                    }
                }
                else if (p is RDirect)
                {
                    RDirect d = (RDirect)p;
                    rowlist.Add(new RDirect() { Prop = d.Prop, DRec = d.DRec });
                }
                else if (p is RInverse)
                {
                    RInverse ri = (RInverse)p;
                    rowlist.Add(new RInverse() { Prop = ri.Prop, IRec = ri.IRec });
                }
            }
            pm.row = rowlist.ToArray();
            pm.inv = inverselist.ToArray();
            return pm;
        }


        public IActionResult Portrait3(string id)
        {
            //var rec = Infobase.engine.GetRRecord(id);
            //string tp = rec.Tp;
            // Сформируем портрет второго уровня
            var rec = BuildPortrait(id);

            P3Model model = new P3Model(rec);
            
            return View("Portrait3", model);
        }





        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}

