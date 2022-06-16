using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CDService.Models
{
    public class Triple
    {
        public string Subj { get; set; }
        public string Pred { get; set; }
        public string Obj { get; set; }
    }
    public class Record
    {
        public string id { get; set; }
        public string ty { get; set; }
        public Arc[] arcs { get; set; }
    }
    public class Arc
    {
        public string alt { get; set; }
        public string prop { get; set; }
    }
    public class ArcField : Arc
    {
        public string text { get; set; } // для варианта field
    }
    public class ArcDirect : Arc
    {
        public Record rec { get; set; } // для direct
    }
    public class ArcInverse : Arc
    {
        public Record[] recs { get; set; } // для inverse
    }
}
