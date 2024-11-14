using System;
using System.IO;
using System.Reflection;
using ae.lib.structure;



namespace ae.lib
{
    public class Service
    {
        private string ServiceName = "";
        private string ServiceDirPath = "";
        private string ServiceNamepsace = "";
        public string InboxDir = "";
        public string OutboxDir = "";

        public Service(string theServiceName, string theServiceNamespace) {
            this.ServiceName = theServiceName;
            this.ServiceNamepsace = theServiceNamespace;
            this.ServiceDirPath = Path.Combine(Base.ServicesDir, theServiceName);
        }

        public bool Init()
        {
            //var n = new AssemblyName(this.ServiceNamepsace);

            this.InboxDir = Path.Combine(this.ServiceDirPath, @"InboxDir");     // ConfigSetting.GetValName(Config, "base_setting").InboxDir;
            this.OutboxDir = Path.Combine(this.ServiceDirPath, @"OutboxDir"); ; // Config.base_setting.OutboxDir;
            Base.Log("InboxDir: " + this.InboxDir);
            Base.Log("OutboxDir: " + this.OutboxDir);

            if (!Base.MakeFolder(this.InboxDir) || !Base.MakeFolder(this.OutboxDir)) {
                string msg = "Error in Base.Init(): cann't create the folders: [" + this.InboxDir + " or " + this.OutboxDir + "]";
                Base.LogError(msg);
                return false;
            }
            return true;
        }
    }
}
