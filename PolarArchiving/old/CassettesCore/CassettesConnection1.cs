using System;
using System.Collections.Generic;
using System.Text;
using System.Xml.Linq;

namespace Polar.Cassettes.DocumentStorage
{
    class CassettesConnection1 : CC
    {
        public CassettesConnection1()
        {

        }
        public override void ConnectToCassettes(IEnumerable<XElement> LoadCassette_elements, LogLine protocol)
        {
            throw new NotImplementedException();
        }

        public override IEnumerable<RDFDocumentInfo> GetFogFiles1()
        {
            throw new NotImplementedException();
        }
    }
}
