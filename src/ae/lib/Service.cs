using System;
using System.IO;
using ae.lib.structure;



namespace ae.lib
{
    internal class Service
    {
        private static Service Instance = null;
        private string DirName = "Service";
        private string DirPath = "";

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
            if (Base.MakeFolder(DirPath)) {
                Base.Log("Service directory - [" + this.DirPath + "].");
                result = true;
            } else
                Base.Log("Service directory isn't found!");
            return result;
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
