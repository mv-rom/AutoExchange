using System;
using System.IO;
using System.Reflection;
using System.Runtime.CompilerServices;
//using System.Text.RegularExpressions;
//using System.Threading.Tasks;
using log4net;
using Newtonsoft.Json;

//http ://stackify.com/log4net-guide-dotnet-logging/


namespace ae.lib
{
    internal class Base
    {
        public static ILog logger;

        public static string RunDir = "";
        public static string BaseDir = "";
        public static string ArchivesDir = "";
        public static string InboxDir = "";
        public static string OutboxDir = "";
        public static string torg_sklad = "";

        public static Config Config = null;
        public static Scheduler Scheduler = null;
        //public static SQLiteDB SQLiteDB = null;

        public static void Init(string Logo="")
        {
            logger = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

            Log("");
            Log("");
            Log("Base.Init() starting ...");
            // Показать Лого Старта
            if (Logo.Length > 0) {
                Log(Logo);
            }

            RunDir = Path.GetDirectoryName(Assembly.GetEntryAssembly().Location);
            //RunDir =  Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            BaseDir = Path.Combine(RunDir, @"..\");
            Log("----------------------------");
            Log("RunDir: " + RunDir);
            Log("BaseDir: " + BaseDir);
            Directory.SetCurrentDirectory(RunDir);

            Config = new Config();
            if (!Config.Init()) {
                string msg = "Error in Base.Init(): Problem with init settings of configuration!";
                LogError(msg);
                throw new Exception(msg);
            }


            if (!Base.Config.ConfigSettings.BaseSetting.TryGetValue("torg_sklad", out torg_sklad)) {
                string msg = "Error in Base.Init(): Hasn't found torg_sklad in settings of configuration!";
                LogError(msg);
                throw new Exception(msg);
            }

            ArchivesDir = Path.Combine(BaseDir, @"Archives");
            Log("ArchivesDir: " + ArchivesDir);
            if (!MakeFolder(ArchivesDir)) {
                string msg = "Error in Base.Init(): Проблема создания рабочих папок: [" + ArchivesDir + "]";
                LogError(msg);
                throw new Exception(msg);
            }

            InboxDir = Path.Combine(BaseDir, @"InboxDir");     // ConfigSetting.GetValName(Config, "base_setting").InboxDir;
            OutboxDir = Path.Combine(BaseDir, @"OutboxDir"); ; // Config.base_setting.OutboxDir;
            Log("InboxDir: " + InboxDir);
            Log("OutboxDir: " + OutboxDir);

            if (!MakeFolder(InboxDir) || !MakeFolder(OutboxDir)) {
                string msg = "Error in Base.Init(): Проблема создания рабочих папок: [" + InboxDir + " | " + OutboxDir + "]";
                LogError(msg);
                throw new Exception(msg);
            }

            //SQLiteDB = SQLiteDB.getInstance();
            Log("Base.Init() is complete with success.");
        }

        public static void deInit() {
            /*
            if (SQLiteDB != null) {
                SQLiteDB.DeInit();
            }
            */
            Base.logger.Logger.Repository.Shutdown();
        }

        public static void Log(string msg)
        {
            logger.Info(msg);
        }
        public static void LogError(string msg, Exception ex=null)
        {
            logger.Error(msg);
            if (ex !=null)
                logger.Error(
                    "\r\n===============================================\r\n"+
                    ex.StackTrace+
                    "\r\n===============================================\r\n"
                );
        }

        public static void Log1(string msg, string vb="")
        {
            Log(vb + "\t" + msg);
        }

        public static void Log2(string msg, string vb="")
        {
            Log(vb + "\t\t" + msg);
        }
        public static void Log3(string msg, string vb="")
        {
            Log(vb + "\t\t\t" + msg);
        }

        private static string AddZero(string val)
        {
            int num = Int32.Parse(val);
            return (num < 10) ? "0" + num.ToString() : num.ToString();
        }

        private static string Pad(int val)
        {
            return val < 10 ? "0" + val.ToString() : val.ToString();
        }

        public static string Right(string s, int n_count)
        {
            if (String.IsNullOrEmpty(s)) return string.Empty;

            if (n_count <= 0) return s;

            int len = s.Length;
            if (n_count > len) {
                return s;
            } else {
                //return s.Substring(len, len - n_length);
                return s.Substring(len - n_count, n_count);
            }
        }

        public static string pureNumberDateTime(DateTime d)
        {
            return String.Format("{0:d4}{1:d2}{2:d2}{3:d2}{4:d2}{5:d2}",
                d.Year, d.Month, d.Day, d.Hour, d.Minute, d.Second);
        }

        public static string NumberDateTime(DateTime d)
        {
            return String.Format("{0:d2}-{1:d2}-{2:d4}_{3:d2}{4:d2}{5:d2}",
                d.Day, d.Month, d.Year, d.Hour, d.Minute, d.Second);
        }

        public static string Now()
        {
            DateTime dt = DateTime.Now;
            return String.Format("{0:d2}-{1:d2}-{2:d4} {3:d2}:{4:d2}:{5:d2}",
                dt.Day, dt.Month, dt.Year, dt.Hour, dt.Minute, dt.Second
            );
        }

        public static bool isFileDateDiffLess(string filepath, int second) 
        {
            TimeSpan ts = DateTime.Now - File.GetLastWriteTime(filepath);
            return (ts.TotalSeconds < second) ? true : false;
        }

        public static DateTime StrToDate(string s)
        {
            //format: 2020-08-19 to date (2020/08/19)
            string[] arr = s.Split('-');
            return DateTime.Parse(arr[0] + "/" + arr[1] + "/" + arr[2]);
        }

        public static double getCurentUnixDateTime()
        {
            TimeSpan epochTicks = new TimeSpan(new DateTime(1970, 1, 1).Ticks);
            TimeSpan unixTicks = new TimeSpan(DateTime.UtcNow.Ticks) - epochTicks;
            return unixTicks.TotalSeconds; //( x /1000 )
        }

        public static double getUnixDateTime(DateTime dt)
        {
            TimeSpan epochTicks = new TimeSpan(new DateTime(1970, 1, 1).Ticks);
            TimeSpan unixTicks = new TimeSpan(dt.ToUniversalTime().Ticks) - epochTicks;
            return (int)Math.Floor(unixTicks.TotalSeconds); //( x /1000 )
        }

        public static bool MakeFolder(string DirPath)
        {
            bool result = false;
            try
            {
                if (!Directory.Exists(DirPath)) {
                    Directory.CreateDirectory(DirPath);
                    if (!Directory.Exists(DirPath)) {
                        LogError("Error in Base.MakeFolder(): Не хватает прав для создания папки [" + DirPath + "]!");
                    }
                    else
                        result = true;
                }
                else
                    result = true;
            }
            catch (IOException ex) {
                LogError("Error in Base.MakeFolder(): " + ex.Message);
            }
            return result;
        }

        public static void RotateArchives(string Dir, string Pattern, int Period)
        {
            Log("");
            Log("Ротация файлов (шаблон: " + Pattern + ") в архивной папке [" + Dir + "]:");

            if (Directory.Exists(Dir)) {
                foreach (string f in Directory.GetFiles(Dir, Pattern))
                {
                    FileInfo fi = new FileInfo(Path.Combine(Dir, f));
                    TimeSpan DayCount = DateTime.Now - fi.LastWriteTime;
                    Log1(":", "-> файл " + fi.Name + " с верменем создания [" + fi.CreationTime + "]" + " - создан " + DayCount.TotalDays + " дней назад");
                    if (DayCount.TotalDays > Period - 1) {
                        try
                        {
                            File.Delete(fi.FullName);
                            if (!File.Exists(fi.FullName)) {
                                Log2(@"\ - удален.");
                            } else {
                                string msg = "> Error in Base.RotateArcheves():.. удаление файла неуспешное!";
                                LogError(msg);
                                throw new Exception(msg);
                            }
                        }
                        catch (Exception ex)
                        {
                            LogError("> Error in Base.RotateArcheves(): проблема при удалении файла [" + fi.FullName + "] - " + ex.Message + "!");
                        }
                    }
                }
            } else {
                Log1("> Архивная папка не найдена!");
            }
        }

        public static void SaveLog(string arch_dir, string file_path)
        {
            // Данные для архива
            string ZipName = "Log_" + NumberDateTime(DateTime.Now) + ".zip";
            string ZipPathName = Path.Combine(arch_dir, ZipName);

            // архивирование файла в архив
            Console.WriteLine("Сохранение лог-файла в архив [" + ZipName + "]:");
            if (ZIP.Create(file_path, ZipPathName, false) && File.Exists(ZipPathName)) {
                Console.WriteLine(@"\- сохранен.");
            }
        }

        public static bool DumpToFile(string FileNamePath, string strData)
        {
            bool result = false;
            using (StreamWriter file = File.CreateText(Path.GetFullPath(FileNamePath)))
            {
                file.WriteLine(strData);
                result = true;
            }
            return result;
        }

        public static string genarateKey()
        {
            return Guid.NewGuid().ToString("N");
        }
    }
}
