using System;
using System.Collections.Generic;
using System.Text;
using System.Xml.Linq;

namespace Polar.Cassettes.DocumentStorage
{
    public class DStorage2 : DS
    {
        public override XElement EditCommand(XElement comm)
        {
            throw new NotImplementedException();
        }

        public override string GetAudioFileName(string u)
        {
            throw new NotImplementedException();
        }

        public override string GetPhotoFileName(string u, string s)
        {
            throw new NotImplementedException();
        }

        public override string GetVideoFileName(string u)
        {
            throw new NotImplementedException();
        }

        public override void Init(XElement xconfig)
        {
            //throw new NotImplementedException();
        }

        public override void InitAdapter(DbAdapter adapter)
        {
            //throw new NotImplementedException();
        }

        public override void LoadFromCassettesExpress()
        {
            throw new NotImplementedException();
        }
    }
}
