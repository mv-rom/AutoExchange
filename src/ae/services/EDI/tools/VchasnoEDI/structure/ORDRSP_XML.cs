using System;
using System.Xml;
using System.Xml.Serialization;
using System.Collections.Generic;
//xml to c# class - http s://xmltocsharp.azurewebsites.net/


namespace ae.services.EDI.tools.VchasnoEDI.structure
{
    [Serializable]
    [XmlRoot(ElementName = "POSITION")]
    public class ORDRSP_POSITION
    {
        [XmlElement(ElementName = "POSITIONNUMBER")]
        public string POSITIONNUMBER { get; set; }

        [XmlElement(ElementName = "PRODUCT")]
        public string PRODUCT { get; set; }

        [XmlElement(ElementName = "PRODUCTIDBUYER")]
        public string PRODUCTIDBUYER { get; set; }

        [XmlElement(ElementName = "PRODUCTIDSUPPLIER")]
        public string PRODUCTIDSUPPLIER { get; set; }

        [XmlElement(ElementName = "ACCEPTEDQUANTITY")]
        public string ACCEPTEDQUANTITY { get; set; }

        [XmlElement(ElementName = "ORDERPRICE")]
        public string ORDERPRICE { get; set; }

        [XmlElement(ElementName = "ORDEREDQUANTITY")]
        public string ORDEREDQUANTITY { get; set; }

        [XmlElement(ElementName = "PRICE")]
        public string PRICE { get; set; }

        [XmlElement(ElementName = "PRICEWITHVAT")]
        public string PRICEWITHVAT { get; set; }

        [XmlElement(ElementName = "VAT")]
        public string VAT { get; set; }

        [XmlElement(ElementName = "AMOUNT")]
        public string AMOUNT { get; set; }

        [XmlElement(ElementName = "AMOUNTWITHVAT")]
        public string AMOUNTWITHVAT { get; set; }

        [XmlElement(ElementName = "TAXAMOUNT")]
        public string TAXAMOUNT { get; set; }

        [XmlElement(ElementName = "PRODUCTTYPE")]
        public string PRODUCTTYPE { get; set; }

        [XmlElement(ElementName = "DESCRIPTION")]
        public string DESCRIPTION { get; set; }
    }

    [Serializable]
    [XmlRoot(ElementName = "HEAD")]
    public class ORDRSP_HEAD
    {
        [XmlElement(ElementName = "SUPPLIER")]
        public string SUPPLIER { get; set; }

        [XmlElement(ElementName = "BUYER")]
        public string BUYER { get; set; }

        [XmlElement(ElementName = "BUYERCODE")]
        public string BUYERCODE { get; set; }

        [XmlElement(ElementName = "DELIVERYPLACE")]
        public string DELIVERYPLACE { get; set; }

        [XmlElement(ElementName = "FINALRECIPIENT")]
        public string FINALRECIPIENT { get; set; }

        [XmlElement(ElementName = "INVOICEPARTNER")]
        public string INVOICEPARTNER { get; set; }

        [XmlElement(ElementName = "SENDER")]
        public string SENDER { get; set; }

        [XmlElement(ElementName = "RECIPIENT")]
        public string RECIPIENT { get; set; }

        [XmlElement(ElementName = "CONSIGNEE")]
        public string CONSIGNEE { get; set; }

        [XmlElement(ElementName = "EDIINTERCHANGEID")]
        public string EDIINTERCHANGEID { get; set; }

        [XmlElement(ElementName = "TRANSPORTQUANTITY")]
        public string TRANSPORTQUANTITY { get; set; }

        [XmlElement(ElementName = "TRANSPORTID")]
        public string TRANSPORTID { get; set; }

        [XmlElement(ElementName = "POSITION")]
        public List<ORDRSP_POSITION> POSITION { get; set; }
    }

    [Serializable]
    [XmlRoot(ElementName = "ORDRSP")]
    public class ORDRSP
    {
        [XmlElement(ElementName = "NUMBER")]
        public string NUMBER { get; set; }

        [XmlElement(ElementName = "DATE")]
        public string DATE { get; set; }

        [XmlElement(ElementName = "ORDERNUMBER")]
        public string ORDERNUMBER { get; set; }

        [XmlElement(ElementName = "ORDERDATE")]
        public string ORDERDATE { get; set; }

        [XmlElement(ElementName = "DELIVERYDATE")]
        public string DELIVERYDATE { get; set; }

        [XmlElement(ElementName = "DELIVERYTIME")]
        public string DELIVERYTIME { get; set; }

        [XmlElement(ElementName = "ACTION")]
        public string ACTION { get; set; }

        [XmlElement(ElementName = "HEAD")]
        public ORDRSP_HEAD HEAD { get; set; }
    }
}
