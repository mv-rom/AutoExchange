using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Serialization;

namespace ae.lib.classes.Base1C
{
    [Serializable]
    [XmlRoot(ElementName = "codeTT")]
    public class codeTT
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
    public class TTbyGLN_Item
    {
        [XmlElement(ElementName = "glnTT")]
        public long glnTT { get; set; }

        [XmlElement(ElementName = "glnTT_gruz")]
        public long glnTT_gruz { get; set; }

        [XmlElement(ElementName = "codeTT")]
        public codeTT codeTT { get; set; }

    }

    [Serializable]
    [XmlRoot(ElementName = "LIST")]
    public class TTbyGLN
    {
        [XmlElement(ElementName = "ITEM")]
        public List<TTbyGLN_Item> list { get; set; }
    }

    //--------------------------------------------------------------

    [Serializable]
    [XmlRoot(ElementName = "ITEM")]
    public class ProductProfiles_Item
    {
        [XmlElement(ElementName = "EAN")]
        public long EAN { get; set; }

        [XmlElement(ElementName = "Title")]
        public string Title { get; set; }

        [XmlElement(ElementName = "ProductCode")]
        public int ProductCode { get; set; }

        [XmlElement(ElementName = "ProductType")]
        public int ProductType { get; set; }

        [XmlElement(ElementName = "BasePrice")]
        public float BasePrice { get; set; }


        [XmlAttribute(AttributeName = "num")]
        public int Number { get; set; }

    }

    [Serializable]
    [XmlRoot(ElementName = "GROUP")]
    public class ProductProfiles_Group
    {
        [XmlAttribute(AttributeName = "id")]
        public string id { get; set; }

        [XmlAttribute(AttributeName = "ExecutionDate")]
        public string ExecutionDate { get; set; }


        [XmlAttribute(AttributeName = "codeTT_part1")]
        public int codeTT_part1 { get; set; }

        [XmlAttribute(AttributeName = "codeTT_part2")]
        public int codeTT_part2 { get; set; }

        [XmlAttribute(AttributeName = "codeTT_part3")]
        public int codeTT_part3 { get; set; }


        [XmlElement(ElementName = "ITEM")]
        public List<ProductProfiles_Item> list { get; set; }
    }

    [Serializable]
    [XmlRoot(ElementName = "LIST")]
    public class ProductProfiles
    {
         [XmlElement(ElementName = "GROUP")]
        public List<ProductProfiles_Group> group { get; set; }
    }
}
