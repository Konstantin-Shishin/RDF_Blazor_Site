using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Linq;
using Polar.Cassettes;

namespace TurgundaCommon
{
    public class ModelCommon
    {
        public static XElement formats = new XElement("formats"); // хранилище основных форматов, загружаемых напр. из ApplicationProfile.xml 
        public static XElement EditRecords = new XElement("EditRecords"); // хранилище основных форматов, загружаемых напр. из ApplicationProfile.xml 
        public static string[][] OntPairs = new string[][] {
            new string[] {"http://fogid.net/o/archive", "архив"},
            new string[] {"http://fogid.net/o/person", "персона"},
            new string[] {"http://fogid.net/o/org-sys", "орг. система"},
            new string[] {"http://fogid.net/o/collection", "коллекция"},
            new string[] {"http://fogid.net/o/document", "документ"},
            new string[] {"http://fogid.net/o/photo-doc", "фото документ"},
            new string[] {"http://fogid.net/o/video-doc", "видео документ"},
            new string[] {"http://fogid.net/o/name", "имя"},
            new string[] {"http://fogid.net/o/from-date", "нач.дата"},
            new string[] {"http://fogid.net/o/to-date", "кон.дата"},
            new string[] {"http://fogid.net/o/in-doc", "в документе"},
            new string[] {"http://fogid.net/o/degree", "степень"},
            new string[] {"http://fogid.net/o/", ""},
        };
        public static Dictionary<string, string> icons = new Tuple<string, string>[]
        {
            new Tuple<string, string>("http://fogid.net/o/document", "document_m.jpg"),
            new Tuple<string, string>("http://fogid.net/o/photo-doc", "photo_m.jpg"),
            new Tuple<string, string>("http://fogid.net/o/video-doc", "video_m.jpg"),
            new Tuple<string, string>("http://fogid.net/o/audio-doc", "audio_m.jpg"),
            new Tuple<string, string>("http://fogid.net/o/collection", "collection_m.jpg")
        }
        .ToDictionary(p => p.Item1, p => p.Item2);

        public static Dictionary<string, string> OntNames = new Dictionary<string, string>(
            OntPairs.ToDictionary(pa => pa[0], pa => pa[1]));
        public static Dictionary<string, string> InvOntNames = new Dictionary<string, string>();
        private static XElement _ontology;
        public static void LoadOntNamesFromOntology(XElement ontology)
        {
            _ontology = ontology;
            var ont_names = ontology.Elements()
                .Where(el => el.Name == "Class" || el.Name == "ObjectProperty" || el.Name == "DatatypeProperty")
                .Where(el => el.Elements("label").Any())
                .Select(el => new
                {
                    type_id = el.Attribute(ONames.rdfabout).Value,
                    label = el.Elements("label").First(lab => lab.Attribute(ONames.xmllang).Value == "ru").Value
                })
                .ToDictionary(pa => pa.type_id, pa => pa.label);
            OntNames = ont_names;
        }
        public static void LoadInvOntNamesFromOntology(XElement ontology)
        {
            _ontology = ontology;
            var i_ont_names = ontology.Elements("ObjectProperty")
                //.Where(el => el.Name == "Class" || el.Name == "ObjectProperty" || el.Name == "DatatypeProperty")
                .Where(el => el.Elements("inverse-label").Any())
                .Select(el => new
                {
                    type_id = el.Attribute(ONames.rdfabout).Value,
                    label = el.Elements("inverse-label").First(lab => lab.Attribute(ONames.xmllang).Value == "ru").Value
                })
                .ToDictionary(pa => pa.type_id, pa => pa.label);
            InvOntNames = i_ont_names;
        }
        public static string GetEnumStateLabel(string enum_type, string state_value)
        {
            var et_def = _ontology.Elements("EnumerationType")
                .FirstOrDefault(et => et.Attribute(ONames.rdfabout).Value == enum_type);
            if (et_def == null) return "";
            var et_state = et_def.Elements("state")
                .FirstOrDefault(st => st.Attribute("value").Value == state_value && st.Attribute(ONames.xmllang).Value == "ru");
            if (et_state == null) return "";
            return et_state.Value;
        }
        public static IEnumerable<XElement> GetEnumStates(string enum_type)
        {
            var et_def = _ontology.Elements("EnumerationType")
                .FirstOrDefault(et => et.Attribute(ONames.rdfabout).Value == enum_type);
            if (et_def == null) return null;
            var et_states = et_def.Elements("state")
                .Where(st => st.Attribute(ONames.xmllang).Value == "ru");
            return et_states;
        }
        public static bool IsTextField(string property_id)
        {
            XElement def_el = _ontology.Elements("DatatypeProperty").FirstOrDefault(p => p.Attribute(ONames.rdfabout).Value == property_id);
            if (def_el != null)
            {
                bool istext = def_el.Elements("range")
                    .Any(range => range.Attribute(ONames.rdfresource).Value == "http://fogid.net/o/text");
                return istext;
            }
            return false;
        }

    }
}
