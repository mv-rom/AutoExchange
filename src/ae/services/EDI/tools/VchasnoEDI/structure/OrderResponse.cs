using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ae.services.EDI.tools.VchasnoEDI.structure
{
    internal class OrderResponse
    {
        public string deal_status { get; set; } //"in_work"
        public string document_id { get; set; }
        public string vchasno_id { get; set; }
    }
}
