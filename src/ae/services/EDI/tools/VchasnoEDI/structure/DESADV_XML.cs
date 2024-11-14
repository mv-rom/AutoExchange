using System;
using System.Xml;
using System.Xml.Serialization;
using System.Collections.Generic;

//xml to c# class - http s://xmltocsharp.azurewebsites.net/

namespace ae.services.EDI.tools.VchasnoEDI.structure
{
    [Serializable]
    [XmlRoot(ElementName = "POSITION")]
    public class POSITION
    {
        [XmlElement(ElementName = "POSITIONNUMBER")]
        public string POSITIONNUMBER { get; set; }
        [XmlElement(ElementName = "PRODUCT")]
        public string PRODUCT { get; set; }
        [XmlElement(ElementName = "PRODUCTIDBUYER")]
        public string PRODUCTIDBUYER { get; set; }
        [XmlElement(ElementName = "PRODUCTIDSUPPLIER")]
        public string PRODUCTIDSUPPLIER { get; set; }
        [XmlElement(ElementName = "DELIVEREDQUANTITY")]
        public string DELIVEREDQUANTITY { get; set; }
        [XmlElement(ElementName = "ORDEREDQUANTITY")]
        public string ORDEREDQUANTITY { get; set; }
        [XmlElement(ElementName = "DELIVEREDUNIT")]
        public string DELIVEREDUNIT { get; set; }
        [XmlElement(ElementName = "PRICE")]
        public string PRICE { get; set; }
        [XmlElement(ElementName = "PRICEWITHVAT")]
        public string PRICEWITHVAT { get; set; }
        [XmlElement(ElementName = "TAXRATE")]
        public string TAXRATE { get; set; }
        [XmlElement(ElementName = "DESCRIPTION")]
        public string DESCRIPTION { get; set; }
    }

    [Serializable]
    [XmlRoot(ElementName = "PACKINGSEQUENCE")]
    public class PACKINGSEQUENCE
    {
        [XmlElement(ElementName = "HIERARCHICALID")]
        public string HIERARCHICALID { get; set; }

        [XmlElement(ElementName = "POSITION")]
        public List<POSITION> POSITION { get; set; }
    }

    [Serializable]
    [XmlRoot(ElementName = "HEAD")]
    public class HEAD
    {
        [XmlElement(ElementName = "SUPPLIER")]
        public string SUPPLIER { get; set; }

        [XmlElement(ElementName = "BUYER")]
        public string BUYER { get; set; }

        [XmlElement(ElementName = "DELIVERYPLACE")]
        public string DELIVERYPLACE { get; set; }

        [XmlElement(ElementName = "FINALRECIPIENT")]
        public string FINALRECIPIENT { get; set; }

        [XmlElement(ElementName = "SENDER")]
        public string SENDER { get; set; }

        [XmlElement(ElementName = "RECIPIENT")]
        public string RECIPIENT { get; set; }

        [XmlElement(ElementName = "EDIINTERCHANGEID")]
        public string EDIINTERCHANGEID { get; set; }

        [XmlElement(ElementName = "PACKINGSEQUENCE")]
        public PACKINGSEQUENCE PACKINGSEQUENCE { get; set; }
    }

    [Serializable]
    [XmlRoot(ElementName = "DESADV")]
    public class DESADV
    {
        [XmlElement(ElementName = "NUMBER")]
        public string NUMBER { get; set; }

        [XmlElement(ElementName = "DATE")]
        public string DATE { get; set; }

        [XmlElement(ElementName = "DELIVERYDATE")]
        public string DELIVERYDATE { get; set; }

        [XmlElement(ElementName = "ORDERNUMBER")]
        public string ORDERNUMBER { get; set; }

        [XmlElement(ElementName = "ORDERDATE")]
        public string ORDERDATE { get; set; }

        [XmlElement(ElementName = "DELIVERYNOTENUMBER")]
        public string DELIVERYNOTENUMBER { get; set; }

        [XmlElement(ElementName = "DELIVERYNOTEDATE")]
        public string DELIVERYNOTEDATE { get; set; }

        [XmlElement(ElementName = "HEAD")]
        public HEAD HEAD { get; set; }
    }

}
