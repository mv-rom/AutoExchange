﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
//using System.Runtime.CompilerServices;
using log4net;
//http ://stackify.com/log4net-guide-dotnet-logging/



namespace ae.lib
{
    public class Base
    {
        public static ILog logger;

        public static string RunDir = "";
        public static string BaseDir = "";
        public static string ServicesDir = "";
        public static string ArchivesDir = "";
        public static string torg_sklad = "";

        public static Config Config = null;
        public static Dictionary<string, Service> Services;
        public static int dumpIndex = 0;


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
            BaseDir = Path.GetFullPath(Path.Combine(RunDir, @"..\"));
            Log("----------------------------");
            Log("RunDir: " +  RunDir);
            Log("BaseDir: " + BaseDir);
            Directory.SetCurrentDirectory(RunDir);

            ArchivesDir = Path.Combine(BaseDir, @"Archives");
            Log("ArchivesDir: " + ArchivesDir);
            if (!MakeFolder(ArchivesDir)) {
                string msg = "Error in Base.Init(): cann't create a folder: [" + ArchivesDir + "]!";
                LogError(msg);
                throw new Exception(msg);
            }

            Base.Config = new Config();
            if (!Base.Config.Init()) {
                string msg = "Error in Base.Init(): Problem with init settings of configuration!";
                LogError(msg);
                throw new Exception(msg);
            }

            if (!Base.Config.ConfigSettings.BaseSetting.TryGetValue("torg_sklad", out torg_sklad))
            {
                string msg = "Error in Base.Init(): Hasn't found torg_sklad in settings of configuration!";
                LogError(msg);
                throw new Exception(msg);
            }

            ServicesDir = Path.Combine(BaseDir, @"Services");
            Log("ServicesDir: " + ServicesDir);
            if (!Base.MakeFolder(ServicesDir)) {
                string msg = "Error in Base.Init(): cann't create a folder: [" + ServicesDir + "]!";
                LogError(msg);
                throw new Exception(msg);
            }

            //Load Services using Namespace of CurrentDomain
            var asmList = AppDomain.CurrentDomain.GetAssemblies();
            var asm = asmList.SingleOrDefault(assembly => assembly.GetName().Name == "ae");
            if (asm == null) {
                string msg = "Error in Base.Init(): cann't found [ae] assembly!";
                LogError(msg);
                throw new Exception(msg);
            }

            string searchPatten = @"ae\.services\.(\w+)";
            Regex r = new Regex(searchPatten, RegexOptions.IgnoreCase);

            var configServiceMembers = Config.ConfigSettings.Services.GetType().GetMembers();
            Services = new Dictionary<string, Service>();
            foreach (var t in asm.GetTypes()) {
                var m = r.Match(t.Namespace);
                if (m.Success && t.BaseType == typeof(Service) && t.GetConstructors().Length>0) {
                    var theServiceName = m.Groups[1].Value;
                    List<MemberInfo> cSM = configServiceMembers.Where(mem => (mem.Name == theServiceName)).ToList();
                    if (cSM.Count > 0) {
                        var serviceInstance = (Service)Activator.CreateInstance(t, theServiceName);
                        if (serviceInstance.Init()) {
                            Services.Add(theServiceName, serviceInstance);
                        }
                    }
                }
            }

            if (Services.Count <= 0) {
                string msg = "Error in Base.Init(): No one service is configured!";
                LogError(msg);
                throw new Exception(msg);
            }

            Log("Base.Init() is complete with success.");
        }

        public static void deInit() {
            Base.Config = null;
            Base.Services = null;
            Base.Log("");
            Base.Log("... Base.deInit() is finished.");
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
                        LogError("Error in Base.MakeFolder(): There is no right to create directory [" + DirPath + "]!");
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
            Log("Rotatin of files (template: " + Pattern + ") in archives folder [" + Dir + "]:");
            Regex r = new Regex(Pattern, RegexOptions.IgnoreCase);

            if (Directory.Exists(Dir)) {
                var Files = Directory.GetFiles(Dir, "*.zip");
                foreach (string f in Files)
                {
                    if (r.IsMatch(f)) {
                        FileInfo fi = new FileInfo(f);
                        TimeSpan DayCount = DateTime.Now - fi.LastWriteTime;
                        var fileTotalDays = Math.Floor(DayCount.TotalDays);
                        Log1(":", "-> File " + fi.Name + " with time creating [" + fi.CreationTime + "]" + " - was created " + fileTotalDays + " days ago");
                        var fName = fi.FullName;
                        if (fileTotalDays > Period - 1) {
                            try
                            {
                                File.Delete(fName);
                                if (!File.Exists(fName)) {
                                    Log2(@"\ - deleted.");
                                } else {
                                    string msg = "> Error in Base.RotateArchives():.. deleting is unsuccessful!";
                                    LogError(msg);
                                    throw new Exception(msg);
                                }
                            }
                            catch (Exception ex)
                            {
                                LogError("> Error in Base.RotateArchives(): проблема при удалении файла [" + fName + "] - " + ex.Message + "!");
                            }
                        }
                    }
                }
            } else {
                Log1("> Archived directory wasn't found!");
            }
        }

        public static void SaveLog(string archPath, string filePath)
        {
            // Данные для архива
            string ZipName = "Log_" + NumberDateTime(DateTime.Now) + ".zip";
            string ZipPathName = Path.Combine(archPath, ZipName);

            // архивирование файла в архив
            Console.WriteLine("Saving log-file in the archive [" + ZipName + "]:");
            if (ZIP.Create(filePath, ZipPathName, false) && File.Exists(ZipPathName)) {
                Console.WriteLine(@"\- saved.");
            }
        }

        public static void SaveDirectory(string archPath, string dirPath, string dirName)
        {
            // Данные для архива
            string zipName = dirName+"_" + NumberDateTime(DateTime.Now) + ".zip";
            string zipPath = Path.Combine(archPath, zipName);
            string fullDirPath = Path.Combine(dirPath, dirName);

            if (Directory.Exists(fullDirPath)) {
                // архивирование файла в архив
                Console.WriteLine("Saving directory to the archive [" + zipName + "]:");
                if (ZIP.ListCreate(fullDirPath, zipPath, false) && File.Exists(zipPath)) {
                    Console.WriteLine(@"\- saved.");
                }
            }
        }

        public static bool DumpToFile(string dirPath, string fileName, string strData)
        {
            bool result = false;
            var index = Base.dumpIndex++;
            var path =  Path.Combine(dirPath, "dump"+index+"_"+ Base.NumberDateTime(DateTime.Now) +"_" + fileName);

            using (StreamWriter file = File.CreateText(Path.GetFullPath(path)))
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
