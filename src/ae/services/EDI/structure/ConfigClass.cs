using System;
using System.Collections.Generic;



namespace ae.services.EDI.structure
{
    [Serializable]
    public class ConfigClass
    {
        public Dictionary<string, string> FtpSetting { get; set; }
        public Dictionary<string, string> AbInbevEfes_ApiSetting { get; set; }
        public Dictionary<string, string> VchasnoEDI_ApiSetting { get; set; }
        public CompaniesClass[] Companies { get; set; }
        public string gln { get; set; }
    }

    public class CompaniesClass
    {
        public string   erdpou { get; set; }
        public int      enable_order_response { get; set; }
        public string   comment { get; set; }
    }
}
