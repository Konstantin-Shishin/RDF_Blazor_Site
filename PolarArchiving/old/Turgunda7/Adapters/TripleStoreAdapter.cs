using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace Turgunda7.Adapters
{
    public class TripleStoreAdapter : Polar.Cassettes.DocumentStorage.DbAdapter
    {
        //private 
        public TripleStoreAdapter()
        {

        }
        public override XElement Add(XElement record)
        {
            throw new NotImplementedException();
        }

        public override XElement AddUpdate(XElement record)
        {
            throw new NotImplementedException();
        }

        public override XElement Delete(string id)
        {
            throw new NotImplementedException();
        }

        public override void FinishFillDb(Action<string> turlog)
        {
            throw new NotImplementedException();
        }

        public override XElement GetItemById(string id, XElement format)
        {
            throw new NotImplementedException();
        }

        public override XElement GetItemByIdBasic(string id, bool addinverse)
        {
            throw new NotImplementedException();
        }

        public override XElement GetItemByIdSpecial(string id)
        {
            throw new NotImplementedException();
        }

        public override void Init(string connectionstring)
        {
            throw new NotImplementedException();
        }

        public override void LoadFromCassettesExpress(IEnumerable<string> fogfilearr, Action<string> turlog, Action<string> convertlog)
        {
            throw new NotImplementedException();
        }

        public override void LoadXFlowUsingRiTable(IEnumerable<XElement> xflow)
        {
            throw new NotImplementedException();
        }

        public override XElement PutItem(XElement record)
        {
            throw new NotImplementedException();
        }

        public override void Save(string filename)
        {
            throw new NotImplementedException();
        }

        public override IEnumerable<XElement> SearchByName(string searchstring)
        {
            throw new NotImplementedException();
        }

        public override void StartFillDb(Action<string> turlog)
        {
            throw new NotImplementedException();
        }
    }
}
