using System;
using System.Collections.Generic;



namespace ae.lib.structure
{
#pragma warning disable CS0649
    [Serializable]
    public class ConfigClass
    {
        public Dictionary<string, string> BaseSetting { get; set; }
        public Dictionary<string, string> App1cSetting { get; set; }
        public Dictionary<string, string> Services { get; set; }
        public SS SchedulerSetting { get; set; }
    }

    [Serializable]
    public class SS
    {
        public string data_file { get; set; }
        public List<SchedulerTaskData> tasks { get; set; }
    }
#pragma warning restore CS0649
}
