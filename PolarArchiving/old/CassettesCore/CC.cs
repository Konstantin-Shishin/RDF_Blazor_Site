using System;
using System.Collections.Generic;
using System.Text;
using System.Xml.Linq;

namespace Polar.Cassettes.DocumentStorage
{
    abstract public class CC
    {
        abstract public void ConnectToCassettes(IEnumerable<XElement> LoadCassette_elements, LogLine protocol);
        public Dictionary<string, Cassettes.CassetteInfo> cassettesInfo = new Dictionary<string, Cassettes.CassetteInfo>();
        abstract public IEnumerable<Cassettes.RDFDocumentInfo> GetFogFiles1();
    }
}
