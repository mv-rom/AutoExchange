using ae.services.EDI.structure._1C;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Serialization;



namespace ae.services.EDI.structure
{
    [Serializable]
    public class AlterProductElementClass
    {
        public string NameProduct { get; set; }
        public long EAN { get; set; }
        public long alterEAN { get; set; }
    }

    [Serializable]
    public class AlterProductClass
    {
        public AlterProductElementClass[] AlterProductList { get; set; }
    }



    //-----------------------------------
    [Serializable]
    [XmlRoot(ElementName = "Outlet")]
    public class Outlet
    {
        [XmlAttribute(AttributeName = "OL_CODE")]
        public string OL_CODE { get; set; }

        [XmlAttribute(AttributeName = "OWNER_ID")]
        public int OWNER_ID { get; set; }
    }

    [Serializable]
    [XmlRoot(ElementName = "Outlets")]
    public class Outlets
    {
        [XmlElement(ElementName = "Outlet")]
        public List<Outlet> Outlet { get; set; }
    }

    [Serializable]
    [XmlRoot(ElementName = "ROOT")]
    public class AgentNumberListClass
    {
        [XmlElement(ElementName = "Outlets")]
        public Outlets Outlets { get; set; }
    }
}