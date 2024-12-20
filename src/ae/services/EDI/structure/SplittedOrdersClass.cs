﻿using System;
using System.Collections.Generic;



namespace ae.services.EDI.structure
{
    [Serializable]
    internal class SplittedOrdersClass_Order
    {
        public long ean13 { get; set; }
        public int codeKPK { get; set; }
        public float basePrice { get; set; }
        public float qty { get; set; }
        public int promoType { get; set; }
        public float totalDiscount { get; set; } //default 0.0
    }

    [Serializable]
    internal class SplittedOrdersClass
    {
        public string id { get; set; }
        public string ae_id { get; set; }
        public string orderNumber { get; set; }
        public int codeTT_part1 { get; set; }
        public int codeTT_part2 { get; set; }
        public int codeTT_part3 { get; set; }
        //public string outletCode { get; set; }
        public DateTime OrderDate { get; set; } //"2024-06-18T12:36:23.020"
        public DateTime OrderExecutionDate { get; set; } //"2024-06-19T00:00:00"

        public List<SplittedOrdersClass_Order> Items { get; set; }

        public int status1c { get; set; } //default 0 - not processing, 1 - processing, 2 - otmenena
        public string deal_status { get; set; } //default new, proccessig, reject

        public string resut_orderNo { get; set; }
        public string result_outletId { get; set; }
        public int resut_owner_id { get; set; }
    }
}
