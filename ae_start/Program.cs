using System;
using System.IO;
using System.Reflection;
using System.Diagnostics;
using System.Threading;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
//using System.Runtime;
//using System.Runtime.CompilerServices;

//http s://www.c-sharpcorner.com/UploadFile/ravesoft/access-assemblyinfo-file-and-get-product-informations/
//http s://nathondalton.wordpress.com/2016/10/03/use-assembly-title-description-company-copyright-version-in-c/
//http s://stackoverflow.com/questions/19384193/get-company-name-and-copyright-information-of-assembly

//http s://learn.microsoft.com/en-us/archive/msdn-magazine/2019/march/net-parse-the-command-line-with-system-commandline


namespace ae_start
{
    internal class ArgOption
    {
        public string Name { get; set; }
        public string NameTemplate { get; set; }
        public string Value { get; set; }
        public bool getNextArg { get; set; }
    }

    internal class Program
    {
        static string libDir = "libs";

        private static bool isAlreadyRunSameProcess(string CurrentProcessDir)
        {
            bool result = false;
            try
            {
                Process currentProcess = Process.GetCurrentProcess();
                foreach (Process process in Process.GetProcesses())
                {
                    if (process.ProcessName == currentProcess.ProcessName) {
                        string text = null;
                        try
                        {
                            text = process.MainModule.FileName;
                        }
                        catch {}

                        if (!string.IsNullOrEmpty(text) && Path.GetDirectoryName(text) == CurrentProcessDir) {
                            try
                            {
                                process.Refresh();
                                if (process.Id != currentProcess.Id) {
                                    result = true;
                                }
                            }
                            catch {}
                        }
                    }
                }
            }
            catch {}
            return result;
        }

        private static Assembly MyResolveDllEventHandler(object source, ResolveEventArgs e)
        {
            var asm = default(Assembly);
            var arrName = e.Name.Split(',');
            var libName = arrName[0].Trim();
            var BaseDir = ((System.AppDomain)source).BaseDirectory;

            var CDas = AppDomain.CurrentDomain.GetAssemblies();
            var loadedAssembly = CDas.Where(a => a.FullName == libName).FirstOrDefault();
            if (loadedAssembly != null) {
                return loadedAssembly;
            }

            var libFileName = arrName[0].Trim() + ".dll";
            if (!Regex.IsMatch(libFileName, @"ae\..+\.dll", RegexOptions.IgnoreCase)) {
                var libDirPath = Path.Combine(BaseDir, Program.libDir);
                var libPath = Path.Combine(libDirPath, libFileName);
                libPath = (File.Exists(libPath)) ? libPath : Path.Combine(libDirPath, "ru", libFileName);
                //Console.WriteLine(">> "+libFileName);
                if (File.Exists(libPath)) {
                    try
                    {
                        asm = Assembly.LoadFrom(libPath);
                        Console.WriteLine("Loaded DLL: " + libPath + " is OK!");
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine("Error: on load Dll [" + libName + "] in " + libDirPath + "!");
                        Console.WriteLine("Error description: " + ex.Message);
                    }
                } else {
                    Console.WriteLine("Error: not found Dll [" + libName + "] in " + libDirPath + "!");
                }
            }
            return asm;
        }

        private static List<ArgOption> ArgOptionStart(string[] args, string ExecFileName, string AppName)
        {
            List<ArgOption> argOptions = new List<ArgOption>() {
                new ArgOption() { Name = "help", NameTemplate = "help|?",
                    Value=""+
                    ExecFileName+":\r\n"+
                    "\tStart "+AppName+" application\r\n"+
                    "Usage: \r\n"+
                    "\t"+ExecFileName+" [options]\r\n"+
                    "Options:\r\n"+
                    "\t/help - show this help\r\n"+
                    "\t/stop - to stop already running process\r\n"+
                    "\t/enable_task clean_db - to enable task\r\n",
                    getNextArg = false
                },
                new ArgOption() { Name = "stop", NameTemplate = "stop", Value = "0", getNextArg = false },
                new ArgOption() { Name = "enable_task", NameTemplate = "enable_task", Value = "", getNextArg = true }
            };
            List<ArgOption> argWorkOptions = new List<ArgOption>();

            for (int i = 0; i < args.Length; i++)
            {
                var a = args[i];
                if (a.ToLower().StartsWith("/")) {
                    var argName = a.ToLower().Substring(1);
                    var en = argOptions.Select((Value, Index) => new { Value, Index }).SingleOrDefault(
                        p => p.Value.Name.Split('|').Contains(argName)
                    );
                    if (en != null) {
                        var ao = new ArgOption();
                        if (en.Value.getNextArg && (i + 1 < args.Length) && (!args[i + 1].StartsWith("/"))) {
                            ao.Name = en.Value.Name;
                            ao.Value = args[i + 1];
                        } else {
                            ao.Name = en.Value.Name;
                            ao.Value = en.Value.Value;
                        }
                        argWorkOptions.Add(ao);
                    }
                }
            }

            if (argWorkOptions.Count() > 0) {
                foreach (var a in argWorkOptions)
                {
                    switch (a.Name) {
                        case "help":
                            Console.WriteLine(a.Value);
                            break;
                        case "stop":
                            Console.WriteLine("Try to stoping ... [" + ExecFileName + "]");
                            break;
                        case "enable_task":
                            if (String.IsNullOrEmpty(a.Value))
                                Console.WriteLine("To enable task, try usage: " + ExecFileName + " <task_name>");
                            else
                                Console.WriteLine("Enable task = <" + a.Value + ">");
                            break;
                    }
                }
            } else {
                //Show help
                Console.WriteLine(argOptions[0].Value);
            }

            return argWorkOptions;
        }



        static void Main(string[] args)
        {
            var AppName = "AutoExchange";
            var AppVersion = typeof(Program).Assembly.GetName().Version;
            var ExecFilePath = Assembly.GetExecutingAssembly().Location;
            var ExecFileName = Path.GetFileName(ExecFilePath);

            Console.WriteLine("[ "+AppName+" v"+AppVersion+": author mv-rom, source https://github.com/mv-rom/"+AppName+" ]");
            if (args.Length > 0) {
                var returnArg = ArgOptionStart(args, ExecFileName, AppName);
            }
            else
            {
                string directoryName = Path.GetDirectoryName(ExecFilePath);
                if (isAlreadyRunSameProcess(directoryName)) {
                    Console.WriteLine("The same process is running! Try again later.");
                    Thread.Sleep(3000);
                } else {
                    AppDomain currentDom = AppDomain.CurrentDomain;
                    currentDom.AssemblyResolve += new ResolveEventHandler(MyResolveDllEventHandler);

                    string dllPath = Path.Combine(directoryName, Program.libDir, "ae.dll");
                    if (File.Exists(dllPath)) {
                        //AppDomain domain = AppDomain.CreateDomain("dllProgram");
                        try
                        {
                            //domain.ExecuteAssembly(dllPath, new string[] { });

                            var asm = Assembly.LoadFile(dllPath);
                            //Type typeToExecute = asm.GetTypes()[0];
                            Type typeToExecute = asm.GetType("ae.dllProgram");
                            //var classInstance = Activator.CreateInstance(typeToExecute);
                            typeToExecute.GetMethod("Entry").Invoke(null, new object[] { });
                            //typeToExecute.GetMethod("Entry").Invoke(classInstance, new object[] { });
                        }
                        catch (System.Reflection.TargetInvocationException ex) {
                            Console.WriteLine("Error: "+ex.Message);
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine(ex.Message);
                        }
                        finally
                        {
                            //AppDomain.Unload(domain);
                        }
                    } else {
                        Console.WriteLine("Error:  not found Dll [ae.dll] in " + dllPath + "!");
                    }
                }
            }
        }
    }

}
