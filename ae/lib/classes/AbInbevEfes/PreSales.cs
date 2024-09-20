using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ae.lib.classes.AbInbevEfes
{
    [Serializable]
    internal class preSalesDetails
    {
        public string productCode { get; set; }
        public string basePrice { get; set; }
        public string qty { get; set; }
        public string lotId { get; set; }
        public string promoType { get; set; }
        public string vat { get; set; } //20.0
    }

    [Serializable]
    internal class PreSalesRequest
    {
        public string preSaleNo { get; set; }
        public string custOrderNo { get; set; }
        public string outletCode { get; set; }
        public string preSaleType { get; set; }
        public string dateFrom { get; set; } //"2024-03-01T11:41:57"
        public string dateTo { get; set; }
        public string warehouseCode { get; set; }
        public string vatCalcMod { get; set; }
        public string custId { get; set; }
        public List<preSalesDetails> preSalesDetails { get; set; }
    }

/*
    [Serializable]
    internal class PreSalesAnswer
    {
        public string result { get; set; }
    }
*/
    [Serializable]
    internal class PreSalesErrorAnswer
    {
        public string error { get; set; }  //? "invalid_request"
        public string error_description { get; set; }
    }


    [Serializable]
    internal class preSalesResponseDetails
    {
        public string productCode { get; set; }
        public string promoType { get; set; } //null - default
        public float qty { get; set; }
        public float price { get; set; }
        public float totalDiscount { get; set; }
    }

    [Serializable]
    internal class PreSalesResponseResult
    {
        public long orderNo { get; set; }
        public string preSaleNo { get; set; }
        public long outletId { get; set; }
        public string outletCode { get; set; }
        public List<preSalesResponseDetails> details { get; set; }
    }

    [Serializable]
    internal class PreSalesResponseSummary
    {
        public int loaded { get; set; }
        public int inserted { get; set; }
        public int updated { get; set; }
        public int deleted { get; set; }
        public int skipped { get; set; }
        public int errors { get; set; }
    }

    [Serializable]
    internal class PreSalesResponse
    {
        public PreSalesResponseResult result { get; set; }
        public string traceIdentifier { get; set; }
        public string urlLog { get; set; }
        public string urlErrorLog { get; set; }
        public PreSalesResponseSummary summary { get; set; }
    }
}