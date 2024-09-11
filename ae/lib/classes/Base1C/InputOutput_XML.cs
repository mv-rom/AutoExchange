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
        [XmlAttribute(AttributeName = "num")]
        public int Number { get; set; }


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


    //--------------------------------------------------------------

    [Serializable]
    [XmlRoot(ElementName = "ITEM")]
    public class NewOrders_Item
    {
        [XmlAttribute(AttributeName = "num")]
        public int Number { get; set; }


        [XmlElement(ElementName = "Name")]
        public string Name { get; set; }

        [XmlElement(ElementName = "codeKPK")]
        public int codeKPK { get; set; }

        [XmlElement(ElementName = "ProductType")] //0 - pivo, 1 - kega, 3 - b/a
        public int ProductType { get; set; }

        [XmlElement(ElementName = "BasePrice")]
        public float BasePrice { get; set; }

        [XmlElement(ElementName = "NDS")]
        public bool nds { get; set; }

        [XmlElement(ElementName = "Akcya")]
        public float Akcya { get; set; }

        [XmlElement(ElementName = "Quant")]
        public int Quant { get; set; }
    }

    [Serializable]
    [XmlRoot(ElementName = "ORDER")]
    public class NewOrders_Order
    {
        [XmlAttribute(AttributeName = "SelfId")]
        public string SelfId { get; set; }

        [XmlAttribute(AttributeName = "ExecutionDate")]
        public string ExecutionDate { get; set; }


        [XmlAttribute(AttributeName = "codeTT_part1")]
        public int codeTT_part1 { get; set; }

        [XmlAttribute(AttributeName = "codeTT_part2")]
        public int codeTT_part2 { get; set; }

        [XmlAttribute(AttributeName = "codeTT_part3")]
        public int codeTT_part3 { get; set; }


        [XmlElement(ElementName = "ITEM")]
        public List<NewOrders_Item> items { get; set; }
    }

        [Serializable]
    [XmlRoot(ElementName = "LIST")]
    public class NewOrders
    {
        [XmlElement(ElementName = "ORDER")]
        public List<ProductProfiles_Group> orders { get; set; }
    }
}
