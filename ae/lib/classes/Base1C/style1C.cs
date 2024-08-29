using System;
using System.Xml;
using System.Xml.Serialization;
using System.Collections.Generic;


namespace ae.lib.classes.Base1C
{
    [Serializable]
    internal class ProductPrice
    {
        public string ean { get; set; }
        public int codeKPK { get; set; } //SWE Code
        public string name { get; set; }
        public double baseprice { get; set; }
        public bool akcuz { get; set; }
        public bool pivo { get; set; }
        public bool akciya { get; set; }
        public bool nds { get; set; }
    }

    [Serializable]
    internal class BasePriceForTT
    {
        public string externalCodeTT { get; set; }
        public Dictionary<string, ProductPrice> productPriceDict { get; set; } //key template: "<codeKPK>@<ean>"
    }


    [Serializable]
    internal class InBoxOrderElement
    {
        public int code { get; set; }
        public float price { get; set; }
        public int qty { get; set; }
        public int vat { get; set; }
        public string base_price { get; set; }
        public int PromoActivities_ID { get; set; }
        public int PromoType { get; set; }
        public string TotalDiscount { get; set; }
    }

    [Serializable]
    internal class InBoxOrder {
        public int id { get; set; }
        public string OrderNo { get; set; }
        public DateTime OrderGettionDate { get; set; } //"2024-06-18T12:36:23.020"
        public DateTime OrderCreationDate { get; set; } //"2024-06-18T12:36:23.020"
        public DateTime OrderExecutionDate { get; set; } //"2024-06-19T00:00:00"
        public string DeliveryAddress { get; set; }
        public int CustId { get; set; }
        public List<InBoxOrderElement> InBoxOrderElements { get; set;}
    }
}
