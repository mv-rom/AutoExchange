using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Serialization;

namespace ae.lib.classes.Base1C
{
    [Serializable]
    [XmlRoot(ElementName = "ITEM")]
    public class TTbyGLN_Elements
    {
        [XmlElement(ElementName = "glnTT")]
        public string glnTT { get; set; }

        [XmlElement(ElementName = "glnTT_gruz")]
        public string glnTT_gruz { get; set; }

        [XmlElement(ElementName = "externalCodeTT")]
        public string externalCodeTT { get; set; }
    }

    [Serializable]
    [XmlRoot(ElementName = "LIST")]
    public class TTbyGLN_List
    {
        [XmlElement(ElementName = "ITEM")]
        public List<TTbyGLN_Elements> listTT { get; set; }
    }
}
