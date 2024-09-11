using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ae.lib.classes.AE
{
    [Serializable]
    internal class SplittedOrdersClass_Order
    {
    }

    [Serializable]
    internal class SplittedOrdersClass
    {
        public int id { get; set; }
        public string OrderNo { get; set; }
        public DateTime OrderCreationDate { get; set; } //"2024-06-18T12:36:23.020"
        public DateTime OrderExecutionDate { get; set; } //"2024-06-19T00:00:00"

    }

}
