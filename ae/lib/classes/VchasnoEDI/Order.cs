using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ae.lib.classes.VchasnoEDI
{
    [Serializable]
    internal class OrderDataItem
    {
        public string title { get; set; }
        public string measure { get; set; }
        public int position { get; set; }
        public string quantity { get; set; }
        public string tax_rate { get; set; }
        public string buyer_code { get; set; }
        public string product_code { get; set; }
        public string supplier_code { get; set; }
    }

    [Serializable]
    internal class OrderData
    {
        public string date { get; set; }
        public string type { get; set; }
        public string number { get; set; }
        public string order_number { get; set; }
        public string currency { get; set; }
        public string buyer_gln { get; set; }
        public string seller_gln { get; set; }
        public string sender_gln { get; set; }
        public string delivery_gln { get; set; }
        public string recipient_gln { get; set; }
        public string invoicepartner_gln { get; set; }
        public string date_expected_delivery { get; set; }
        public string delivery_address { get; set; }
        public OrderDataItem[] items { get; set; }
    }

    [Serializable]
    internal class Order
    {
        public string id { get; set; }
        public string number { get; set; }
        public int type { get; set; } //"type": 1
        public int status { get; set; }
        public string company_from { get; set; }
        public string company_to { get; set; }
        public string deal_id { get; set; }
        public string date_created { get; set; }
        public string date_updated { get; set; }
        public string date_document { get; set; }
        public string deal_status { get; set; }
        public string company_from_edrpou { get; set; } //23633937 Orbita
        public string company_to_edrpou { get; set; }
        public bool is_processed { get; set; }

        public OrderData as_json { get; set; }

        //public Order @__ParentObject { get; set; }
}

    [Serializable]
    internal class PostData
    {
        public string ids { get; set; }
    }

    [Serializable]
    internal class PostAnswer
    {
        public string updated_ids { get; set; }
    }

    [Serializable]
    internal class PostErrorAnswer
    {
        public string reason { get; set; }
        public string details { get; set; }
        public string deal_id { get; set; }
        public int code { get; set; }
    }
}
