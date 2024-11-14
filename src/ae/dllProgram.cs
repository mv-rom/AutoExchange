using System;
using System.IO;
using ae.lib;
using ae.services.EDI;



namespace ae
{
    public static class dllProgram
    {
        //[STAThread]
        public static void Entry()
        {
            Base.Init();

            //EDI.actionInBox();
            //EDI.actionOutBox();

            /*
                Base.Scheduler = Scheduler.getInstance();
                try
                {
                    Base.Scheduler.Run();
                }
                catch (Exception ex)
                {
                    string msg = "Error in Program.Main(): "+ex.Message;
                    Base.Log(msg);
                    throw new Exception(msg);
                }
                finally
                {
                    Base.Scheduler.DeInit();
                }
            */

            // ротация файлов архивов (удаление старых) 
            // в архивной папке через период (количество дней)
            int day = 14;
            // для архивов данных
            string pattern = "_[A-Za-z0-9_-]*.zip";
            Base.RotateArchives(Base.ArchivesDir, pattern, day);

            // для архивов логов
            pattern = "Log_[A-Za-z0-9_-]*.zip";
            Base.RotateArchives(Base.ArchivesDir, pattern, day);

            Base.Log("");
            Base.Log(">>> Работа скрипта завершена.");

            Base.deInit();

            Base.SaveLog(Base.ArchivesDir, "ae.log");
            File.Delete(Path.Combine(Base.RunDir, "ae.log"));
        }
    }
}
