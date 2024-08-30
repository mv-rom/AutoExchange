using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Serialization;

namespace ae.lib.classes.Base1C
{
    [Serializable]
    [XmlRoot(ElementName = "ExternalCodeTT")]
    public class ExternalCodeTT
    {
        [XmlElement(ElementName = "part1")]
        public int part1 { get; set; }

        [XmlElement(ElementName = "part2")]
        public int part2 { get; set; }

        [XmlElement(ElementName = "part3")]
        public int part3 { get; set; }
    }

    //--------------------------------------------------------------

    [Serializable]
    [XmlRoot(ElementName = "ITEM")]
    public class TTbyGLN_Elements
    {
        [XmlElement(ElementName = "glnTT")]
        public long glnTT { get; set; }

        [XmlElement(ElementName = "glnTT_gruz")]
        public long glnTT_gruz { get; set; }

        [XmlElement(ElementName = "externalCodeTT")]
        public ExternalCodeTT externalCodeTT { get; set; }

    }

    [Serializable]
    [XmlRoot(ElementName = "LIST")]
    public class TTbyGLN
    {
        [XmlElement(ElementName = "ITEM")]
        public List<TTbyGLN_Elements> list { get; set; }
    }

    //--------------------------------------------------------------

    [Serializable]
    [XmlRoot(ElementName = "ITEM")]
    public class ProductProfiles_Elements
    {
        [XmlElement(ElementName = "EAN")]
        public long EAN { get; set; }

        [XmlElement(ElementName = "ProductCode")]
        public int ProductCode { get; set; }

        [XmlElement(ElementName = "ProductType")]
        public int ProductType { get; set; }

        [XmlElement(ElementName = "BasePrice")]
        public float BasePrice { get; set; }
    }

    [Serializable]
    [XmlRoot(ElementName = "GROUP")]
    public class ProductProfiles_Group
    {
        public ExternalCodeTT externalCodeTT { get; set; }
        public List<ProductProfiles_Elements> list { get; set; }
    }

    [Serializable]
    [XmlRoot(ElementName = "LIST")]
    public class ProductProfiles
    {
         [XmlElement(ElementName = "GROUP")]
        public List<ProductProfiles_Group> group { get; set; }
    }
}
