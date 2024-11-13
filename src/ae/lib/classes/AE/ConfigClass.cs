using System;
using System.Collections.Generic;


namespace ae.lib.classes.AE
{
#pragma warning disable CS0649
    [Serializable]
    public class ConfigClass
    {
        public Dictionary<string, string> BaseSetting { get; set; }
        public Dictionary<string, string> FtpSetting { get; set; }
        public Dictionary<string, string> AbInbevEfes_ApiSetting { get; set; }
        public Dictionary<string, string> VchasnoEDI_ApiSetting { get; set; }
        public Dictionary<string, string> App1cSetting { get; set; }
        public CompaniesClass[] Companies { get; set; }
        public SS SchedulerSetting { get; set; }
    }

    [Serializable]
    public class SS
    {
        public string data_file { get; set; }
        public List<SchedulerTaskData> tasks { get; set; }
    }

    public class CompaniesClass
    {
        public string gln  { get; set; }
        public string erdpou { get; set; }
        public int enable_order_response { get; set; }
        public string comment { get; set; }
    }
#pragma warning restore CS0649

}
