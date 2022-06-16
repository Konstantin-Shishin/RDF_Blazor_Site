using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Xml.Linq;
//using Fogid.Cassettes;
using Polar.Cassettes;


namespace Turgunda6.Models
{
    //public class Common
    //{
    //    public static XElement formats = new XElement("formats"); // хранилище основных форматов, загружаемых напр. из ApplicationProfile.xml 
    //    public static string[][] OntPairs = new string[][] {
    //        new string[] {"http://fogid.net/o/archive", "архив"},
    //        new string[] {"http://fogid.net/o/person", "персона"},
    //        new string[] {"http://fogid.net/o/org-sys", "орг. система"},
    //        new string[] {"http://fogid.net/o/collection", "коллекция"},
    //        new string[] {"http://fogid.net/o/document", "документ"},
    //        new string[] {"http://fogid.net/o/photo-doc", "фото документ"},
    //        new string[] {"http://fogid.net/o/video-doc", "видео документ"},
    //        new string[] {"http://fogid.net/o/name", "имя"},
    //        new string[] {"http://fogid.net/o/from-date", "нач.дата"},
    //        new string[] {"http://fogid.net/o/to-date", "кон.дата"},
    //        new string[] {"http://fogid.net/o/in-doc", "в документе"},
    //        new string[] {"http://fogid.net/o/degree", "степень"},
    //        new string[] {"http://fogid.net/o/", ""},
    //    };
    //    public static Dictionary<string, string> OntNames = new Dictionary<string, string>(
    //        OntPairs.ToDictionary(pa => pa[0], pa => pa[1]));
    //    public static Dictionary<string, string> InvOntNames = new Dictionary<string, string>();
    //    private static XElement _ontology;
    //    public static void LoadOntNamesFromOntology(XElement ontology)
    //    {
    //        _ontology = ontology;
    //        var ont_names = ontology.Elements()
    //            .Where(el => el.Name == "Class" || el.Name == "ObjectProperty" || el.Name == "DatatypeProperty")
    //            .Where(el => el.Elements("label").Any())
    //            .Select(el => new
    //            {
    //                type_id = el.Attribute(ONames.rdfabout).Value,
    //                label = el.Elements("label").First(lab => lab.Attribute(ONames.xmllang).Value == "ru").Value
    //            })
    //            .ToDictionary(pa => pa.type_id, pa => pa.label);
    //        OntNames = ont_names;
    //    }
    //    public static void LoadInvOntNamesFromOntology(XElement ontology)
    //    {
    //        _ontology = ontology;
    //        var i_ont_names = ontology.Elements("ObjectProperty")
    //            //.Where(el => el.Name == "Class" || el.Name == "ObjectProperty" || el.Name == "DatatypeProperty")
    //            .Where(el => el.Elements("inverse-label").Any())
    //            .Select(el => new
    //            {
    //                type_id = el.Attribute(ONames.rdfabout).Value,
    //                label = el.Elements("inverse-label").First(lab => lab.Attribute(ONames.xmllang).Value == "ru").Value
    //            })
    //            .ToDictionary(pa => pa.type_id, pa => pa.label);
    //        InvOntNames = i_ont_names;
    //    }
    //    public static string GetEnumStateLabel(string enum_type, string state_value)
    //    {
    //        var et_def = _ontology.Elements("EnumerationType")
    //            .FirstOrDefault(et => et.Attribute(ONames.rdfabout).Value == enum_type);
    //        if (et_def == null) return "";
    //        var et_state = et_def.Elements("state")
    //            .FirstOrDefault(st => st.Attribute("value").Value == state_value && st.Attribute(ONames.xmllang).Value == "ru");
    //        if (et_state == null) return "";
    //        return et_state.Value;
    //    }
    //    public static IEnumerable<XElement> GetEnumStates(string enum_type)
    //    {
    //        var et_def = _ontology.Elements("EnumerationType")
    //            .FirstOrDefault(et => et.Attribute(ONames.rdfabout).Value == enum_type);
    //        if (et_def == null) return null;
    //        var et_states = et_def.Elements("state")
    //            .Where(st => st.Attribute(ONames.xmllang).Value == "ru");
    //        return et_states;
    //    }
    //    public static bool IsTextField(string property_id)
    //    {
    //        XElement def_el = _ontology.Elements("DatatypeProperty").FirstOrDefault(p => p.Attribute(ONames.rdfabout).Value == property_id);
    //        if (def_el != null)
    //        {
    //            bool istext = def_el.Elements("range")
    //                .Any(range => range.Attribute(ONames.rdfresource).Value == "http://fogid.net/o/text");
    //            return istext;
    //        }
    //        return false;
    //    }

    //}
    public class PortraitModel
    {
        public string id;
        public string type_id;
        public string typelabel;
        public string name;
        public string uri = null;
        public string[] doclist = null; // Это для размещение списка идентификаторов документов по некоторой "оси"
        public XElement xresult;
        // Для всяких дел:
        public XElement look;
        public string message;
        public XElement xtree;
        public PortraitModel(string id) : this(id, null) { }
        // По идентификатору мы получаем 1) откорректированный идентификатор; 2) тип записи; 3) формат (раскрытия) записи
        // Эту информацию дополняем меткой типа, пытаемся прочитать и зафиксировать имя записи и uri документного контента
        public PortraitModel(string id, XElement rec_format)
        {
            DateTime tt0 = DateTime.Now;
            //XElement rec_format;
            if (rec_format == null) this.type_id = GetFormat(id, out rec_format);
            else this.type_id = rec_format.Attribute("type").Value;

            this.typelabel = TurgundaCommon.ModelCommon.OntNames.Where(pair => pair.Key == type_id).Select(pair => pair.Value).FirstOrDefault();
            if (this.typelabel == null) this.typelabel = type_id;
            // Получим портретное х-дерево
            this.xtree = Turgunda7.SObjects.GetItemById(id, rec_format);
            if (this.xtree == null) return;
            // По дереву вычислим и зафиксируем остальные поля
            // поле идентификатора
            this.id = xtree.Attribute("id").Value;
            // поле имени
            XElement field_name = xtree.Elements("field")
                .Where(fi => fi.Attribute("prop").Value == "http://fogid.net/o/name")
                .OrderBy(fi => {
                    XAttribute lang_att = fi.Attribute(ONames.xmllang);
                    return lang_att == null ? "" : lang_att.Value;
                })
                .FirstOrDefault();
            this.name = field_name == null ? "Noname" : field_name.Value;
            // поле uri
            if (type_id == "http://fogid.net/o/photo-doc" || type_id == "http://fogid.net/o/video-doc" || type_id == "http://fogid.net/o/audio-doc")
            {
                //var uri_el = GetUri(xtree);
                var uri_el = xtree.Elements("field")
                    .FirstOrDefault(fi => fi.Attribute("prop").Value == "http://fogid.net/o/uri");
                if (uri_el != null) uri = uri_el.Value;
            }

            this.xresult = ConvertToResultStructure(rec_format, xtree);
            //this.look = xtree;

            this.message = "duration=" + ((DateTime.Now - tt0).Ticks / 10000L);
            //this.look = xresult;
        }
        public static string GetFormat(string id, out XElement rec_format)
        {
            string type_id;
            // Нам нужен формат.
            XElement f_primitive = new XElement("record");
            XElement xtree0 = Turgunda7.SObjects.GetItemById(id, f_primitive);
            //if (xtree0 == null) { return null; }; // Как-то надо поступить с диагностикой ошибок//
            type_id = xtree0.Attribute("type").Value;
            // Теперь установим нужный формат
            XElement xformat = TurgundaCommon.ModelCommon.formats.Elements("record")
                .FirstOrDefault(re => re.Attribute("type") != null && re.Attribute("type").Value == type_id);
            if (xformat == null)
            {
                xformat = new XElement("record",
                    new XAttribute("type", type_id),
                    new XElement("field", new XAttribute("prop", "http://fogid.net/o/name")));
            }
            rec_format = xformat;
            return type_id;
        }
        public static XElement ConvertToResultStructure(XElement xformat, XElement xtree)
        {
            XElement result = new XElement("Result",
                new XElement("header", ScanForHeaderFields(xformat)),
                GetRecordRow(xtree, xformat),
                null);
            foreach (var f_inv in xformat.Elements("inverse").Where(inv => inv.Attribute("invisible") == null))
            {
                string prop = f_inv.Attribute("prop").Value;
                var queryForInverse = xtree.Elements("inverse").Where(el => el.Attribute("prop").Value == prop).ToArray();
                XElement ip = new XElement("ip", new XAttribute("prop", prop), new XElement("label",
                    (f_inv.Element("label") != null ? f_inv.Element("label").Value : TurgundaCommon.ModelCommon.InvOntNames[prop])));
                foreach (var f_rec in f_inv.Elements("record"))
                {
                    XAttribute view_att = f_rec.Attribute("view");
                    string recType = f_rec.Attribute("type").Value;
                    var queryForRecords = queryForInverse.Select(inve => inve.Element("record"))
                        .Where(re => re != null && re.Attribute("type") != null && re.Attribute("type").Value == recType).ToArray();
                    XElement ir = new XElement("ir",
                        new XAttribute("type", recType),
                        view_att == null ? null : new XAttribute(view_att),
                        new XElement("label",
                            (f_rec.Element("label") != null ? f_rec.Element("label").Value : TurgundaCommon.ModelCommon.OntNames[recType])),
                        new XElement("header", ScanForHeaderFields(f_rec)));
                    ip.Add(ir);
                    foreach (var v_rec in queryForRecords)
                    {
                        ir.Add(GetRecordRow(v_rec, f_rec));
                    }
                }
                result.Add(ip);
            }
            return result;
        }
        // Статический метод: рекурсивное сканирование айтема с вычислением всех определенных форматом записи полей
        // Возвращает таблицу <r id="ид.записи" type="ид.типа"><c>значение</c><c>...</c>...<r>..</r>...</r>
        // где c стоят на позициях полей, а r - на позициях прямых отношений
        private static XElement GetRecordRow(XElement item, XElement frecord)
        {
            var fieldsAndDirects = item.Elements()
                .Where(el => el.Name == "field" || el.Name == "direct")
                .ToArray();
            var inv_props_atts = frecord.Elements("inverse").Where(fel => fel.Attribute("attname") != null)
                //.Select(fel => new { prop = fel.Attribute("prop").Value, attname = fel.Attribute("attname").Value })
                .ToDictionary(fel => fel.Attribute("prop").Value, fel => fel.Attribute("attname").Value);
            // Очень коряво!!!
            var atts = item.Elements("inverse")
                            .Where(el => inv_props_atts.ContainsKey(el.Attribute("prop").Value))
                            .Select(el => new XAttribute(XName.Get(inv_props_atts[el.Attribute("prop").Value]), el.Value))
                            .ToArray();
            // Ограничусь одним атрибутом, если есть совпадения (ОЧЕНЬ ПЛОХО ДЕЛАЮ!!!)
            if (atts.Length > 0) atts = new XAttribute[] { atts[0] };
            XElement r = new XElement("r",
                new XAttribute("id", item.Attribute("id").Value),
                new XAttribute("type", item.Attribute("type").Value),
                atts,
                ScanForFieldValues(fieldsAndDirects, frecord),
                null);
            return r;
        }
        private static IEnumerable<XObject> ScanForFieldValues(IEnumerable<XElement> fieldsAndDirects, XElement frecord)
        {
            foreach (var f_el in frecord.Elements())
            {
                if (f_el.Name == "field")
                {
                    if (f_el.Attribute("invisible") != null) continue;
                    string prop = f_el.Attribute("prop").Value;
                    // Найдем множество полей, имеющееся в "россыпи"
                    var field = fieldsAndDirects.Where(fd => fd.Name == "field")
                        .Where(f => f.Attribute("prop").Value == prop)
                        .FirstOrDefault();
                    //XAttribute pr_att = prop == ONames.p_name ? new XAttribute("prop", prop) : null;
                    if (field == null) { yield return new XElement("c"); }
                    else if (prop == "http://fogid.net/o/uri")
                    {
                        yield return new XAttribute("uri", field.Value);
                    }
                    else
                    {
                        //XAttribute valueTypeAtt = f_el.Attribute("type");
                        string value = field.Value;
                        //if (valueTypeAtt != null) value = Common.GetEnumStateLabel(valueTypeAtt.Value, value);
                        yield return new XElement("c", value);
                    }
                }
                else if (f_el.Name == "direct")
                {
                    string prop = f_el.Attribute("prop").Value;
                    // Найдем один, если есть, элемент direct, удовлетворяющий условиям 
                    var direct = fieldsAndDirects.Where(fd => fd.Name == "direct")
                        .FirstOrDefault(d => d.Attribute("prop").Value == prop);
                    var record = direct == null ? null : direct.Element("record");
                    if (direct == null || record == null) yield return new XElement("r");
                    else
                    {
                        var rec_type_att = record.Attribute("type");
                        if (rec_type_att != null)
                        {
                            XElement f_rec = f_el.Elements("record").FirstOrDefault(re => re.Attribute("type").Value == rec_type_att.Value);
                            if (frecord != null && f_rec != null) yield return GetRecordRow(record, f_rec); // не знаю зачем проверка
                        }
                    }
                }
            }
        }
        // Статический метод: рекурсивное сканирование с выявлением всех определенных форматом записи полей
        public static IEnumerable<XElement> ScanForHeaderFields(XElement frecord)
        {
            foreach (var el in frecord.Elements())
            {
                if (el.Name == "field" && el.Attribute("invisible") != null) continue;
                else if (el.Name == "field") yield return GetLabel(el);
                else if (el.Name == "direct")
                {
                    XElement label_el = el.Elements("label").FirstOrDefault();
                    string label = label_el == null ? TurgundaCommon.ModelCommon.OntNames[el.Attribute("prop").Value] : label_el.Value;
                    yield return new XElement("d", new XAttribute("prop", el.Attribute("prop").Value),
                        new XElement("label", label),
                        el.Elements("record").Select(re => new XElement("r", new XAttribute(re.Attribute("type")),
                            ScanForHeaderFields(re))));
                }
            }
        }
        private static XElement GetLabel(XElement el)
        {
            XAttribute t = el.Attribute("type");
            XAttribute p = el.Attribute("prop");
            XElement label_el = el.Elements("label").FirstOrDefault();
            string label = "label";
            if (label_el != null) label = label_el.Value;
            else if (!TurgundaCommon.ModelCommon.OntNames.TryGetValue(p.Value, out label))
            {
                label = p.Value;
            }
            return new XElement("f",
                p == null ? new XAttribute("prop", "nop") : new XAttribute("prop", p.Value),
                t == null ? null : new XAttribute("valueType", t.Value),
                label,
                null);
        }


        //private static XElement GetUri(XElement xtree)
        //{
        //    var uri = xtree.Elements("inverse")
        //        .Where(i => i.Attribute("prop").Value == ONames.p_fordocument)
        //        .Select(i => i.Elements("record")
        //            .Where(r => r.Attribute("type").Value == ONames.FOG + "FileStore")
        //            .SelectMany(r => r.Elements("field")
        //                .Where(f => f.Attribute("prop").Value == ONames.FOG + "uri")))
        //        .SelectMany(u => u)
        //        .FirstOrDefault();
        //    return uri;
        //}
    }
    public class RecordModel
    {
        public RecordModel() { }

        public string lang = "ru"; // В дальнейшем, надо будет превратить спецификатор в свойство и переключаться по нему
        public string command { get; set; }
        public string exchange { get; set; }
        private bool _firsttime = false;
        public bool firsttime { get { return _firsttime; } set { _firsttime = value; } }
        // Если eid == bid, то это базовая запись 
        public string bid { get; set; } // идентификатор "внешней" записи 
        public string eid { get; set; } // идентификатор "этой" записи
        public string etype { get; set; } // идентификатор типа е-записи
        public string iprop { get; set; } // идентификатор обратного (между базовой и этой) отношения
        public int nc { get; set; } // количество колонок, заменяемы при редактировании
        //private XElement _xresult;
        //public XElement GetXResult() { return _xresult; }

        private XElement _format = null;
        public XElement CalculateFormat()
        {
            _format = Turgunda7.SObjects.GetEditFormat(etype, iprop);
            if (_format == null) _format = new XElement("record", new XAttribute("type", etype));
            return _format;
        }
        private XElement _xtree;
        public void SetXTree(XElement xtree) { _xtree = xtree; }

        public void LoadFromDb()
        {
            CalculateFormat();
            _xtree = Turgunda7.SObjects.GetItemById(eid, _format);
        }
        public IEnumerable<XElement> GetHeaderFlow()
        {
            return PortraitModel.ScanForHeaderFields(_format);
        }
        //public void MakeXResult() 
        //{
        //    _xresult = PortraitModel.ConvertToResultStructure(_format, _xtree);
        //    //look = xresult;
        //}

        /// <summary>
        /// Для имеющегося формата _format и древовидного результата _xtree, ему соответствующего, создается копия объекта
        /// через его отображение в поля f_0, f_1,.., p_0, p1,..,t_0, t_1, ..
        /// </summary>
        public void MakeLocalRecord()
        {
            var field_xvalues = _xtree.Elements("field");
            var direct_xvalues = _xtree.Elements("direct");
            int i = 0;
            foreach (XElement fd in GetHeaderFlow())
            {
                string prop = fd.Attribute("prop").Value;
                if (fd.Name == "f")
                {
                    XElement elem = field_xvalues.Where(f => f.Attribute("prop").Value == prop)
                        .OrderBy(f =>
                        {
                            var att = f.Attribute(ONames.xmllang);
                            return att == null ? "" : att.Value;
                        })
                        .FirstOrDefault();
                    if (elem != null)
                    {
                        SetFValue(i, elem.Value);
                    }
                }
                else if (fd.Name == "d")
                {
                    XElement elem = direct_xvalues.FirstOrDefault(d => d.Attribute("prop").Value == prop);
                    if (elem != null)
                    {
                        XElement rec = elem.Element("record");
                        if (rec != null)
                        {
                            XAttribute t_att = rec.Attribute("type");
                            if (t_att != null)
                            {
                                // Надо еще поставить проверку типов и способа вычисления значения
                                var ref_name_el = rec.Elements("field")
                                    .FirstOrDefault(f => f.Attribute("prop").Value == "http://fogid.net/o/name");
                                if (ref_name_el != null)
                                {
                                    SetVValue(i, ref_name_el.Value);
                                    SetPValue(i, rec.Attribute("id").Value);
                                    SetTValue(i, t_att.Value);
                                }
                            }
                        }
                    }
                }
                else { } // не должно быть этого варианта, но если он будет, непонятно что делать
                i++;
            }
        }

        // Отладочная структура
        public XElement look = null;

        public const int nfields = 21;
        public string f_0 { get; set; }
        public string f_1 { get; set; }
        public string f_2 { get; set; }
        public string f_3 { get; set; }
        public string f_4 { get; set; }
        public string f_5 { get; set; }
        public string f_6 { get; set; }
        public string f_7 { get; set; }
        public string f_8 { get; set; }
        public string f_9 { get; set; }
        public string f_10 { get; set; }
        public string f_11 { get; set; }
        public string f_12 { get; set; }
        public string f_13 { get; set; }
        public string f_14 { get; set; }
        public string f_15 { get; set; }
        public string f_16 { get; set; }
        public string f_17 { get; set; }
        public string f_18 { get; set; }
        public string f_19 { get; set; }
        public string f_20 { get; set; }

        public string v_0 { get; set; }
        public string v_1 { get; set; }
        public string v_2 { get; set; }
        public string v_3 { get; set; }
        public string v_4 { get; set; }
        public string v_5 { get; set; }
        public string v_6 { get; set; }
        public string v_7 { get; set; }
        public string v_8 { get; set; }
        public string v_9 { get; set; }
        public string v_10 { get; set; }
        public string v_11 { get; set; }
        public string v_12 { get; set; }
        public string v_13 { get; set; }
        public string v_14 { get; set; }
        public string v_15 { get; set; }
        public string v_16 { get; set; }
        public string v_17 { get; set; }
        public string v_18 { get; set; }
        public string v_19 { get; set; }
        public string v_20 { get; set; }

        public string p_0 { get; set; }
        public string p_1 { get; set; }
        public string p_2 { get; set; }
        public string p_3 { get; set; }
        public string p_4 { get; set; }
        public string p_5 { get; set; }
        public string p_6 { get; set; }
        public string p_7 { get; set; }
        public string p_8 { get; set; }
        public string p_9 { get; set; }
        public string p_10 { get; set; }
        public string p_11 { get; set; }
        public string p_12 { get; set; }
        public string p_13 { get; set; }
        public string p_14 { get; set; }
        public string p_15 { get; set; }
        public string p_16 { get; set; }
        public string p_17 { get; set; }
        public string p_18 { get; set; }
        public string p_19 { get; set; }
        public string p_20 { get; set; }

        public string t_0 { get; set; }
        public string t_1 { get; set; }
        public string t_2 { get; set; }
        public string t_3 { get; set; }
        public string t_4 { get; set; }
        public string t_5 { get; set; }
        public string t_6 { get; set; }
        public string t_7 { get; set; }
        public string t_8 { get; set; }
        public string t_9 { get; set; }
        public string t_10 { get; set; }
        public string t_11 { get; set; }
        public string t_12 { get; set; }
        public string t_13 { get; set; }
        public string t_14 { get; set; }
        public string t_15 { get; set; }
        public string t_16 { get; set; }
        public string t_17 { get; set; }
        public string t_18 { get; set; }
        public string t_19 { get; set; }
        public string t_20 { get; set; }

        public void SetFValue(int i, string s)
        {
            switch (i)
            {
                case 0: f_0 = s; break;
                case 1: f_1 = s; break;
                case 2: f_2 = s; break;
                case 3: f_3 = s; break;
                case 4: f_4 = s; break;
                case 5: f_5 = s; break;
                case 6: f_6 = s; break;
                case 7: f_7 = s; break;
                case 8: f_8 = s; break;
                case 9: f_9 = s; break;
                case 10: f_10 = s; break;
                case 11: f_11 = s; break;
                case 12: f_12 = s; break;
                case 13: f_13 = s; break;
                case 14: f_14 = s; break;
                case 15: f_15 = s; break;
                case 16: f_16 = s; break;
                case 17: f_17 = s; break;
                case 18: f_18 = s; break;
                case 19: f_19 = s; break;
                case 20: f_20 = s; break;
                default: break;
            }
        }
        public void SetVValue(int i, string s)
        {
            switch (i)
            {
                case 0: v_0 = s; break;
                case 1: v_1 = s; break;
                case 2: v_2 = s; break;
                case 3: v_3 = s; break;
                case 4: v_4 = s; break;
                case 5: v_5 = s; break;
                case 6: v_6 = s; break;
                case 7: v_7 = s; break;
                case 8: v_8 = s; break;
                case 9: v_9 = s; break;
                case 10: v_10 = s; break;
                case 11: v_11 = s; break;
                case 12: v_12 = s; break;
                case 13: v_13 = s; break;
                case 14: v_14 = s; break;
                case 15: v_15 = s; break;
                case 16: v_16 = s; break;
                case 17: v_17 = s; break;
                case 18: v_18 = s; break;
                case 19: v_19 = s; break;
                case 20: v_20 = s; break;
                default: break;
            }
        }
        public void SetPValue(int i, string s)
        {
            switch (i)
            {
                case 0: p_0 = s; break;
                case 1: p_1 = s; break;
                case 2: p_2 = s; break;
                case 3: p_3 = s; break;
                case 4: p_4 = s; break;
                case 5: p_5 = s; break;
                case 6: p_6 = s; break;
                case 7: p_7 = s; break;
                case 8: p_8 = s; break;
                case 9: p_9 = s; break;
                case 10: p_10 = s; break;
                case 11: p_11 = s; break;
                case 12: p_12 = s; break;
                case 13: p_13 = s; break;
                case 14: p_14 = s; break;
                case 15: p_15 = s; break;
                case 16: p_16 = s; break;
                case 17: p_17 = s; break;
                case 18: p_18 = s; break;
                case 19: p_19 = s; break;
                case 20: p_20 = s; break;
                default: break;
            }
        }
        public void SetTValue(int i, string s)
        {
            switch (i)
            {
                case 0: t_0 = s; break;
                case 1: t_1 = s; break;
                case 2: t_2 = s; break;
                case 3: t_3 = s; break;
                case 4: t_4 = s; break;
                case 5: t_5 = s; break;
                case 6: t_6 = s; break;
                case 7: t_7 = s; break;
                case 8: t_8 = s; break;
                case 9: t_9 = s; break;
                case 10: t_10 = s; break;
                case 11: t_11 = s; break;
                case 12: t_12 = s; break;
                case 13: t_13 = s; break;
                case 14: t_14 = s; break;
                case 15: t_15 = s; break;
                case 16: t_16 = s; break;
                case 17: t_17 = s; break;
                case 18: t_18 = s; break;
                case 19: t_19 = s; break;
                case 20: t_20 = s; break;
                default: break;
            }
        }
        public string GetFValue(int i)
        {
            switch (i)
            {
                case 0: return f_0;
                case 1: return f_1;
                case 2: return f_2;
                case 3: return f_3;
                case 4: return f_4;
                case 5: return f_5;
                case 6: return f_6;
                case 7: return f_7;
                case 8: return f_8;
                case 9: return f_9;
                case 10: return f_10;
                case 11: return f_11;
                case 12: return f_12;
                case 13: return f_13;
                case 14: return f_14;
                case 15: return f_15;
                case 16: return f_16;
                case 17: return f_17;
                case 18: return f_18;
                case 19: return f_19;
                case 20: return f_20;
                default: break;
            }
            return null;
        }
        public string GetVValue(int i)
        {
            switch (i)
            {
                case 0: return v_0;
                case 1: return v_1;
                case 2: return v_2;
                case 3: return v_3;
                case 4: return v_4;
                case 5: return v_5;
                case 6: return v_6;
                case 7: return v_7;
                case 8: return v_8;
                case 9: return v_9;
                case 10: return v_10;
                case 11: return v_11;
                case 12: return v_12;
                case 13: return v_13;
                case 14: return v_14;
                case 15: return v_15;
                case 16: return v_16;
                case 17: return v_17;
                case 18: return v_18;
                case 19: return v_19;
                case 20: return v_20;
                default: break;
            }
            return null;
        }
        public string GetPValue(int i)
        {
            switch (i)
            {
                case 0: return p_0;
                case 1: return p_1;
                case 2: return p_2;
                case 3: return p_3;
                case 4: return p_4;
                case 5: return p_5;
                case 6: return p_6;
                case 7: return p_7;
                case 8: return p_8;
                case 9: return p_9;
                case 10: return p_10;
                case 11: return p_11;
                case 12: return p_12;
                case 13: return p_13;
                case 14: return p_14;
                case 15: return p_15;
                case 16: return p_16;
                case 17: return p_17;
                case 18: return p_18;
                case 19: return p_19;
                case 20: return p_20;
                default: break;
            }
            return null;
        }
        public string GetTValue(int i)
        {
            switch (i)
            {
                case 0: return t_0;
                case 1: return t_1;
                case 2: return t_2;
                case 3: return t_3;
                case 4: return t_4;
                case 5: return t_5;
                case 6: return t_6;
                case 7: return t_7;
                case 8: return t_8;
                case 9: return t_9;
                case 10: return t_10;
                case 11: return t_11;
                case 12: return t_12;
                case 13: return t_13;
                case 14: return t_14;
                case 15: return t_15;
                case 16: return t_16;
                case 17: return t_17;
                case 18: return t_18;
                case 19: return t_19;
                case 20: return t_20;
                default: break;
            }
            return null;
        }
    }

    public class SearchResult
    {
        public string id;
        public string type;
        public string value;
        public string lang;
        public string type_name;
    }
    public class SearchModel
    {
        private SearchResult[] _results;
        public SearchResult[] Results { get { return _results; } }
        public string message;
        public string searchstring;
        public string type;
        public TimeSpan timespan;
        public SearchModel(string searchstring, string type)
        {
            DateTime tt0 = DateTime.Now;
            this.searchstring = searchstring;
            //type = "http://fogid.net/o/person"; // для отладки
            this.type = type;
            //if (string.IsNullOrEmpty(searchstring)) { _results = new SearchResult[0]; return; }
            var query = Turgunda7.SObjects.SearchByName(searchstring)
                .Select(xres =>
                {
                    XElement name_el = xres
                        .Elements("field").Where(f => f.Attribute("prop").Value == "http://fogid.net/o/name")
                        .OrderByDescending(n => n.Attribute(ONames.xmllang) == null ? "ru" : n.Attribute(ONames.xmllang).Value)
                        .FirstOrDefault();
                    string name = name_el == null ? "noname" : name_el.Value;
                    SearchResult res = new SearchResult() { id = xres.Attribute("id").Value, value = name };
                    XAttribute t_att = xres.Attribute("type");
                    if (t_att != null) res.type = t_att.Value;
                    string tname = "";
                    if (!string.IsNullOrEmpty(res.type)) TurgundaCommon.ModelCommon.OntNames.TryGetValue(res.type, out tname);
                    res.type_name = tname;
                    return res;
                });
            if (this.type != null) query = query.Where(res => res.type == this.type);
            _results = query
                .OrderBy(s => s.value)
                .ToArray();
            timespan = DateTime.Now - tt0;
            message = "duration=" + (timespan.Ticks / 10000L);
        }
    }

    /// <summary>
    /// Эта модель - для тонкого анализа структуры данных. Выдаются поля, прямые и обратные ствойства в терминах онтологии 
    /// </summary>
    public abstract class Relation { }
    public class Field : Relation { public string prop; public string value; public string lang; }
    public class Direct : Relation { public string prop; public string resource; }
    public class Inverse : Relation { public string prop; public string about; public string name; }
    public class PortraitSpecialModel
    {
        public string id = null;
        public string type_id;
        public string type;
        public string name;
        public Field[] farr;
        public Direct[] darr;
        public Inverse[] iarr;
        public TimeSpan duration;
        public PortraitSpecialModel(string id)
        {
            DateTime tt0 = DateTime.Now;
            XElement record = Turgunda7.SObjects.GetItemByIdSpecial(id);
            if (record == null) return;
            this.id = record.Attribute("id").Value;
            string type_id = record.Attribute("type") == null ? "notype" : record.Attribute("type").Value;
            this.type_id = type_id;
            this.type = TurgundaCommon.ModelCommon.OntNames.Where(pair => pair.Key == type_id).Select(pair => pair.Value).FirstOrDefault();
            if (this.type == null) this.type = type_id;
            var name_el = record.Elements("field").Where(f => f.Attribute("prop").Value == "http://fogid.net/o/name")
                .FirstOrDefault();
            this.name = name_el == null ? "noname" : name_el.Value;
            farr = record.Elements("field")
                .Select(el => new Field() { prop = el.Attribute("prop").Value, value = el.Value, lang = (el.Attribute(ONames.xmllang) == null ? null : el.Attribute(ONames.xmllang).Value) })
                .ToArray();
            darr = record.Elements("direct")
                .Select(el => new Direct() { prop = el.Attribute("prop").Value, resource = el.Element("record").Attribute("id").Value })
                .ToArray();
            iarr = record.Elements("inverse")
                .Select(el => new Inverse() { prop = el.Attribute("prop").Value, about = el.Element("record").Attribute("id").Value })
                .ToArray();
            duration = DateTime.Now - tt0;
        }
    }

}