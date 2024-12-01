using System;
using System.IO;
using System.Linq;
using System.Reflection;



namespace ae.lib
{
    public class Service
    {
        private string ServiceName = "";
        private string ServiceDirPath = "";
        public string InboxDir = "";
        public string OutboxDir = "";
        public string Reports1cDir = "";

        public Service(string theServiceName) {
            this.ServiceName = theServiceName;
            this.ServiceDirPath = Path.Combine(Base.ServicesDir, theServiceName);
        }

        public bool Init()
        {
            Base.Log("Init service ["+ this.ServiceName +"] with directory " +this.ServiceDirPath);
            if (!Base.MakeFolder(this.ServiceDirPath)) {
                string msg = "Error in Service.Init(): cann't create a directory: [" + this.ServiceDirPath + "]!";
                Base.LogError(msg);
                throw new Exception(msg);
            }

            this.InboxDir = Path.Combine(this.ServiceDirPath, @"Inbox");
            if (!Base.MakeFolder(this.InboxDir)) {
                Base.LogError("Error in Service.Init(): cann't create the directory [" + this.InboxDir + "]!");
                return false;
            }
            Base.Log("Inbox dir: " + this.InboxDir);

            this.OutboxDir = Path.Combine(this.ServiceDirPath, @"Outbox");
            if (!Base.MakeFolder(this.OutboxDir)) {
                Base.LogError("Error in Service.Init(): cann't create the directory [" + this.OutboxDir + "]!");
                return false;
            }
            Base.Log("Outbox dir: " + this.OutboxDir);

            this.Reports1cDir = Path.Combine(this.ServiceDirPath, @"Reports1c");
            if (!Base.MakeFolder(this.Reports1cDir))
            {
                Base.LogError("Error in Service.Init(): cann't create the directory [" + this.Reports1cDir + "]!");
                return false;
            }
            Base.Log("Reports1c dir: " + this.Reports1cDir);

            return true;
        }

        public void RunAction(string Name)
        {
            if (Name.Length > 0) {
                MethodInfo metd = this.GetType().GetMethod(Name);
                metd.Invoke(this, null);
            }
        }
    }
}
