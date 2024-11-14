using System;
using System.IO;
using ae.lib.structure;



namespace ae.lib
{
    public class Service
    {
        private static Service Instance = null;
        private string DirName = "Services";
        private string DirPath = "";

        public static string InboxDir = "";
        public static string OutboxDir = "";

        public Service() {
            DirPath = Path.Combine(Base.BaseDir, DirName);

        }

        public static Service getInstance()
        {
            if (Service.Instance == null)
            {
                Service.Instance = new Service();
                if (Service.Instance.Init() != true) Service.Instance.DeInit();
            }
            return Service.Instance;
        }

        public bool Init()
        {
            bool result = false;
            if (!Base.MakeFolder(DirPath)) {
                string msg = "Error in Service.Init(): cann't create a folder: [" + DirPath + "]";
                Base.LogError(msg);
                return false;
            }
            Base.Log("ServicesDir: " + this.DirPath);

            InboxDir = Path.Combine(Base.BaseDir, @"InboxDir");     // ConfigSetting.GetValName(Config, "base_setting").InboxDir;
            OutboxDir = Path.Combine(Base.BaseDir, @"OutboxDir"); ; // Config.base_setting.OutboxDir;
            Base.Log("InboxDir: " + InboxDir);
            Base.Log("OutboxDir: " + OutboxDir);

            if (!Base.MakeFolder(InboxDir) || !Base.MakeFolder(OutboxDir)) {
                string msg = "Error in Base.Init(): cann't create the folders: [" + InboxDir + " | " + OutboxDir + "]";
                Base.LogError(msg);
                throw new Exception(msg);
            }

            return true;
        }

        public void DeInit()
        {
        }

        public bool Run()
        {
            bool result = false;
            return result;
        }

    }
}
