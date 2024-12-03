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
        public string Reports1CDir = "";

        public Service(string theServiceName) {
            this.ServiceName = theServiceName;
            this.ServiceDirPath = Path.Combine(Base.ServicesDir, theServiceName);
        }

        public bool Init()
        {
            string generalMsg = "Error in Service.Init(): cann't create the directory";

            Base.Log("Init service ["+ this.ServiceName +"] with directory " +this.ServiceDirPath);
            if (!Base.MakeFolder(this.ServiceDirPath)) {
                string msg = generalMsg+" [" + this.ServiceDirPath + "]!";
                Base.Log(msg);
                throw new Exception(msg);
            }

            this.InboxDir = Path.Combine(this.ServiceDirPath, @"Inbox");
            if (!Base.MakeFolder(this.InboxDir)) {
                Base.Log(generalMsg + " [" + this.InboxDir + "]!");
                return false;
            }
            Base.Log("Inbox dir: " + this.InboxDir);

            this.OutboxDir = Path.Combine(this.ServiceDirPath, @"Outbox");
            if (!Base.MakeFolder(this.OutboxDir)) {
                Base.Log(generalMsg + " [" + this.OutboxDir + "]!");
                return false;
            }
            Base.Log("Outbox dir: " + this.OutboxDir);

            this.Reports1CDir = Path.Combine(this.ServiceDirPath, @"Reports1C");
            if (!Base.MakeFolder(this.Reports1CDir))
            {
                Base.Log(generalMsg + " [" + this.Reports1CDir + "]!");
                return false;
            }
            Base.Log("Reports1C dir: " + this.Reports1CDir);

            return true;
        }

        public void RunAction(string Name)
        {
            if (Name.Length > 0) {
                MethodInfo metd = this.GetType().GetMethod(Name);
                if (metd != null) {
                    metd.Invoke(this, null);
                }
            }
        }
    }
}
