using System.Xml.Linq;

namespace Soran1957core
{
    public static class StaticObjects
    {
        public static string cassettes_path = "", PublicuemCommon_path = "";        
        public static void Init(string basepath)
        {
            if (basepath[basepath.Length - 1] != '/' && basepath[basepath.Length - 1] != '\\') basepath += '/';
            _basepath = basepath;
            //SGraph.LOG.fileName = _basepath + "LOG.txt";
            AnalizeConfig();
        }

        private static void AnalizeConfig()
        {
            XElement xconfig = XElement.Load(_basepath + "config.xml");
            XElement entry = xconfig.Element("entry");
            if (entry != null)
            {
                XAttribute startIdAtt = entry.Attribute("startId");
                if (startIdAtt != null) homeid = startIdAtt.Value;
                if (!string.IsNullOrEmpty(entry.Value)) sitelabel = entry.Value;
            }
            XElement logo = xconfig.Element("logo");
            if (logo != null)
            {
                XAttribute uri = logo.Attribute("uri");
                if (uri != null) logourl = "?r=small&u=" + uri.Value;
            }
            xmenu = xconfig.Element("menu");
            if (xmenu == null) xmenu = new XElement("menu");
        }
        public static string sitelabel = "Публикуем!";
        public static string homeid = null;
        public static string logourl = "PublicuemCommon/logo1.jpg";
        public static XElement xmenu;

        public static int pagePortion = 20;
        private static string _basepath;
        public static string Basepath { get { return _basepath; } }
        public static XElement Groups = 
            new XElement("groups",
                // Для системных объектов
                new XElement("group", new XAttribute("groupId", "referred-sys"),
                    new XElement("label", "Другие наименования"),
                    new XElement("order", "81"),
                    null),
                new XElement("group", new XAttribute("groupId", "has-title"),
                    new XElement("label", "Титулы и награды"),
                    new XElement("order", "045"),
                    new XElement("sorting", "date"),
                    null),
                new XElement("group", new XAttribute("groupId", "user"),
                    new XElement("label", "Связь"),
                    new XElement("order", "032"),
                    new XElement("sorting", "date"),
                    null),
                new XElement("group", new XAttribute("groupId", "something"),
                    new XElement("label", "Расположение"),
                    new XElement("order", "01"),
                    new XElement("sorting", "date"),
                    null),
            // Для организационных систем
                new XElement("group", new XAttribute("groupId", "org-child"),
                    new XElement("label", "Входит в"),
                    new XElement("order", "031"),
                    null),
                new XElement("group", new XAttribute("groupId", "org-parent"),
                    new XElement("label", "Состоит из"),
                    new XElement("order", "03"),
                    null),
                new XElement("group", new XAttribute("groupId", "first-faces"),
                    new XElement("label", "Первые лица"),
                    new XElement("order", "02"),
                    null),
                // Для персон
                new XElement("group", new XAttribute("groupId", "learner"),
                    new XElement("label", "Учеба"),
                    new XElement("order", "04"),
                    new XElement("sorting", "date"),
                    null),
                new XElement("group", new XAttribute("groupId", "work-inorg"),
                    new XElement("label", "Работа"),
                    new XElement("order", "035"),
                    new XElement("sorting", "date"),
                    null),
                new XElement("group", new XAttribute("groupId", "family"),
                    new XElement("label", "Семья"),
                    new XElement("order", "02"),
                    null),
                new XElement("group", new XAttribute("groupId", "participant"),
                    new XElement("label", "Участие"),
                    new XElement("order", "015"),
                    new XElement("sorting", "date"),
                    null),
                new XElement("group", new XAttribute("groupId", "in-org"),
                    new XElement("label", "Участники"),
                    new XElement("order", "015"),
                    new XElement("sorting", "date"),
                    null),
                new XElement("group", new XAttribute("groupId", "author"),
                    new XElement("label", "Автор"),
                    new XElement("order", "013"),
                    new XElement("sorting", "date"),
                    null),
            // Для коллекций
                new XElement("group", new XAttribute("groupId", "collection-item"),
                    new XElement("order", "05"),
                    new XElement("sorting", "date"),
                    null),
                new XElement("group", new XAttribute("groupId", "in-collection"),
                    new XElement("order", "04"),
                    new XElement("sorting", "date"),
                    null),
                // для документов
                new XElement("group", new XAttribute("groupId", "adoc"),
                    new XElement("label", "Автор(ы)"),
                    new XElement("order", "013"),
                    null),
                new XElement("group", new XAttribute("groupId", "in-doc"),
                    new XElement("label", "Отражение"),
                    new XElement("order", "020"),
                    null),
                // Для геосистем
                new XElement("group", new XAttribute("groupId", "location-place"),
                    new XElement("label", "Место для"),
                    new XElement("order", "020"),
                    null),

                null);
        public static XElement Icons = new XElement("icons",
            new XElement("icon", new XAttribute("type", "sys-object-default"), new XAttribute("image", "PublicuemCommon/icons/medium/default_m.jpg")),
            new XElement("icon", new XAttribute("type", "audio-doc"), new XAttribute("image", "PublicuemCommon/icons/medium/audio_m.jpg")),
            new XElement("icon", new XAttribute("type", "city"), new XAttribute("image", "PublicuemCommon/icons/medium/city_m.jpg")),
            new XElement("icon", new XAttribute("type", "document"), new XAttribute("image", "PublicuemCommon/icons/medium/document_m.jpg")),
            new XElement("icon", new XAttribute("type", "person"), new XAttribute("image", "PublicuemCommon/icons/medium/person_m.jpg")),
            new XElement("icon", new XAttribute("type", "photo-doc"), new XAttribute("image", "PublicuemCommon/icons/medium/photo_m.jpg")),
            new XElement("icon", new XAttribute("type", "video-doc"), new XAttribute("image", "PublicuemCommon/icons/medium/video_m.jpg")),
            new XElement("icon", new XAttribute("type", "collection"), new XAttribute("image", "PublicuemCommon/icons/medium/collection_m.jpg")),
            //new XElement("icon", new XAttribute("type", ""), new XAttribute("image", "PublicuemCommon/icons/medium/_m.jpg")),
            null);
        public static XElement RelationIcons = new XElement("relationicons",
            new XElement("icon", new XAttribute("inverseProp", "in-collection"), new XAttribute("type", "collection-member"), new XAttribute("image", "PublicuemCommon/icons/TreeClosed.gif")),
            new XElement("icon", new XAttribute("inverseProp", "collection-item"), new XAttribute("type", "collection-member"), new XAttribute("image", "PublicuemCommon/icons/TreeOpen.gif")),
            new XElement("icon", new XAttribute("inverseProp", "user"), new XAttribute("type", "phone"), new XAttribute("image", "PublicuemCommon/icons/tel.gif")),
            new XElement("icon", new XAttribute("inverseProp", "has-title"), new XAttribute("type", "titled"), new XAttribute("image", "PublicuemCommon/icons/orden-small.jpg")),
            new XElement("icon", new XAttribute("inverseProp", "author"), new XAttribute("type", "authority"), new XAttribute("image", "PublicuemCommon/icons/authority.jpg")),
            //new XElement("icon", new XAttribute("inverseProp", ""), new XAttribute("type", ""), new XAttribute("image", "icons/")),
            null);
    }
 }
