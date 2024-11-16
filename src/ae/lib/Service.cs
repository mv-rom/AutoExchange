using System;
using System.IO;
using System.Reflection;



namespace ae.lib
{
    public class Service
    {
        private string ServiceName = "";
        private string ServiceDirPath = "";
        public string InboxDir = "";
        public string OutboxDir = "";

        public Service(string theServiceName) {
            this.ServiceName = theServiceName;
            this.ServiceDirPath = Path.Combine(Base.ServicesDir, theServiceName);
        }

        public bool Init()
        {

            Base.Log("Init service ["+ this.ServiceName + "] with directory " + this.ServiceDirPath);
            if (!Base.MakeFolder(this.ServiceDirPath))
            {
                string msg = "Error in Service.Init(): cann't create a directory: [" + this.ServiceDirPath + "]!";
                Base.LogError(msg);
                throw new Exception(msg);
            }

            this.InboxDir = Path.Combine(this.ServiceDirPath, @"Inbox");     // ConfigSetting.GetValName(Config, "base_setting").InboxDir;
            this.OutboxDir = Path.Combine(this.ServiceDirPath, @"Outbox"); ; // Config.base_setting.OutboxDir;
            Base.Log("Inbox dir: " + this.InboxDir);
            Base.Log("Outbox dir: " + this.OutboxDir);

            if (!Base.MakeFolder(this.InboxDir) || !Base.MakeFolder(this.OutboxDir)) {
                string msg = "Error in Service.Init(): cann't create the folders: [" + this.InboxDir + " or " + this.OutboxDir + "]";
                Base.LogError(msg);
                return false;
            }
            return true;
        }
    }
}
