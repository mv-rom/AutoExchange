using System;
using System.Collections.Generic;



namespace ae.services.EDI.structure
{
    public class CompaniesClass
    {
        public string gln { get; set; }
        public string edrpou { get; set; }
        public int enableOrderResponse { get; set; }
        public string comment { get; set; }
        public string executionDayOfWeek { get; set; }
    }

    public class PromotionsClass
    {
        public int Enable { get; set; }
        public string DiscountPercentage { get; set; }
        public string TheEndDate { get; set; }
        public string CompaniesGLN { get; set; }
    }


    [Serializable]
    public class ConfigClass
    {
        public Dictionary<string, string> FtpSetting { get; set; }
        public Dictionary<string, string> AbInbevEfes_ApiSetting { get; set; }
        public Dictionary<string, string> VchasnoEDI_ApiSetting { get; set; }
        public CompaniesClass[] Companies { get; set; }
        public PromotionsClass Promotions { get; set; }
        public string gln { get; set; }
    }
}
