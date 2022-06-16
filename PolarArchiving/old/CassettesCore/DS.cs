using System;
using System.Collections.Generic;
using System.Text;
using System.Xml.Linq;

namespace Polar.Cassettes.DocumentStorage
{
    /// <summary>
    /// Абастрактный класс, реализующий документное хранилище
    /// </summary>
    abstract public class DS
    {
        abstract public void Init(XElement xconfig);
        abstract public void InitAdapter(DbAdapter adapter);
        abstract public void LoadFromCassettesExpress();
        public CC connection;
        abstract public XElement EditCommand(XElement comm);
        abstract public string GetPhotoFileName(string u, string s);
        abstract public string GetVideoFileName(string u);
        abstract public string GetAudioFileName(string u);
    }
}
