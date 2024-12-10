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
            Base.RotateArchives(Base.ArchivesDir, @"^(?:(?!Log).)*$", day);
            // для архивов логов
            Base.RotateArchives(Base.ArchivesDir, @".+Log_[0-9-]+_[0-9]+\.zip", day);

            Base.deInit();

            var fi = new FileInfo(Path.Combine(Base.RunDir, "ae.log"));
            if (fi.Exists) {
                Base.SaveLog(Base.ArchivesDir, fi.FullName);
                fi.Delete();
            }
        }
    }
}
