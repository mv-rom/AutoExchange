using System;
using System.IO;
using ae.lib;



namespace ae
{
    public static class dllProgram
    {
        //[STAThread]
        public static void Entry()
        {
            Base.Init();

            //var edi = (services.EDI.EDI)Base.Services["EDI"];
            //edi?.actionInBox();
            //edi?.actionOutBox();

            var Scheduler = lib.Scheduler.getInstance();
            try
            {
                Scheduler.Run();
            }
            catch (Exception ex)
            {
                string msg = "Error in dllProgram.Entry(): " + ex.Message;
                Base.Log(msg);
                throw new Exception(msg);
            }
            finally
            {
                Scheduler.DeInit();
            }

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
            Base.Log(">>> Work of script is complete.");

            Base.deInit();

            Base.SaveLog(Base.ArchivesDir, "ae.log");
            File.Delete(Path.Combine(Base.RunDir, "ae.log"));
        }
    }
}
