using System;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;

//https://forum.infostart.ru/forum9/topic83477/
//https://forum.infostart.ru/forum9/topic148614/
//https://anatoly4xs.narod.ru/manual/lang/lang0073.htm


/*
 * case to use V77.Application
    regsvr32 "C:\Program Files (x86)\1Cv77\BIN\v7plus.dll"
    regsvr32 "C:\Program Files (x86)\1Cv77\BIN\v7chart.dll"
    regsvr32 "C:\Program Files (x86)\1Cv77\BIN\zlibeng.dll"
 */


namespace ae.lib
{
    internal class _1C
    {
        public static _1C Instance = null;

        private object instance1C = null;
        private Type   type1C = null;
        //private string WorkDir = "";
        private string ReportDirPath = "";
        //private string LogFileName = "log_1c.txt";
        private string LogFilePath = "";
        private string torg_sklad = "";


        public _1C(string reports1c_Dir)
        {
            this.type1C = null;
            this.instance1C = null;
            this.ReportDirPath = reports1c_Dir;
            //this.LogFilePath = Path.Combine(this.WorkDir, this.LogFileName);
        }

        public static _1C getInstance(string reports1c_Dir)
        {
            if (_1C.Instance == null)
            {
                _1C.Instance = new _1C(reports1c_Dir);
                if (_1C.Instance.Init() != true) {
                    _1C.Instance.deInit();
                }
            }
            return _1C.Instance;
        }

        public bool Init()
        {
            bool res = false;
            string connect_string = "";
            string className = this.GetType().Name;

            Base.Log("> Запускаем интерфейс 1C 7.7:");
            Base.Log1(className + "Init() ..");
            try
            {
                if (!Base.Config.ConfigSettings.BaseSetting.TryGetValue("torg_sklad", out this.torg_sklad)) {
                    throw new Exception("torg_sklad is not found!");
                }

                if (!Base.Config.ConfigSettings.App1cSetting.TryGetValue("connect_string", out connect_string)) {
                    throw new Exception("connect_string is not found!");
                }

                this.type1C = Type.GetTypeFromProgID("V77.Application", true);
                if (this.type1C != null) {
                    this.instance1C = Activator.CreateInstance(this.type1C);
                    if (this.instance1C != null) {
                        dynamic rmtrade = this.type1C.InvokeMember(
                            "RMTrade",
                            BindingFlags.GetProperty, null, this.instance1C, null
                        );

                        var args = new object[] { rmtrade, connect_string, @"NO_SPLASH_SHOW" };
                        object objInitializeRes = this.type1C.InvokeMember(
                            "Initialize",
                            BindingFlags.Public | BindingFlags.InvokeMethod | BindingFlags.Static, null, this.instance1C, args
                        );
                        res = (bool)objInitializeRes;
                    } else
                        Base.Log1("> Не удалось инициализировать объект 1С!");
                } else
                    Base.Log1("> Не найден тип [V77.Application] програми 1С!");
            }
            catch (Exception ex) {
                Base.Log1("Error on "+className+".Init(): "+ex.Message);
            }
            return res;
        }

        public void deInit()
        {
            Base.Log("deInit() of [" + this.GetType().Name + "].");
            this.instance1C = null;

            GC.Collect();
            GC.WaitForPendingFinalizers();
            _1C.Instance = null;
        }

        private object runInvokeMethod(string Name, object[] args)
        {
            object result = null;
            string className = this.GetType().Name;
            BindingFlags flagsM = BindingFlags.Public | BindingFlags.InvokeMethod | BindingFlags.Static;

            try
            {
                result = this.type1C.InvokeMember(Name, flagsM, null, this.instance1C, args);
            }
            catch (Exception ex) {
                Base.Log1("Error on " + className + ".RunInvokeMethod(): " + ex.Message);
            }
            return result;
        }

        public object runEval(string valString)
        {
            //valString = "ВосстановитьЗначение(\"НеЗапрашиватьПодтверждениеПриВыходе\")";
            //valString = "ПолучитьДатуТА()";
            return this.runInvokeMethod("EvalExpr", new object[] { valString });
        }

        public bool runExternalReport(string workDir, string Name)
        {
            bool result = false;

            Base.Log1(">> Запуск отчета -> [" + Name + "]:");
            if (File.Exists(this.LogFilePath))
                File.Delete(this.LogFilePath);

            var param = new object[] {
                @"Отчет",
                this.torg_sklad + ";" + workDir,
                Path.Combine(this.ReportDirPath, Name+".ert")
            };
            Base.Log1("| Параметры запуска отчета:");
            foreach (var p in param)
            {
                Base.Log1((String)p, "\t> ");
            }
            Base.Log1("");

            object obj = this.runInvokeMethod(@"OpenForm", param);
            if (Convert.ToInt32(obj) == 1)
                result = true;
            return result;
        }

        public void runInternalReport()
        {
            //?
        }

        public void runExit()
        {
            this.runInvokeMethod("ExitSystem", new object[] { 0 });
            Base.Log1("> Выход из интерфейс 1C - ОК.");
        }

        public bool doReportFileInput(string workDir, string data)
        {
            bool result = false;
            string className = this.GetType().Name;

            if (data.Length > 0) {
                var InputFilePath = Path.Combine(workDir, "1CInput.xml");
                try
                {
                    if (File.Exists(InputFilePath)) {
                        File.Delete(InputFilePath);
                    }
                    File.WriteAllText(InputFilePath, data, Encoding.ASCII); //Encoding.GetEncoding("windows-1251"));
                    result = true;
                }
                catch (Exception ex)
                {
                    Base.Log1("Error on " + className + ".doReportFileInput(): " + ex.Message);
                }
            }
            return result;
        }

        public bool doReportFileOutput(string workDir, out string data)
        {
            bool result = false;
            string className = this.GetType().Name;
            data = "";

            var OutputFilePath = Path.Combine(workDir, "1COutput.xml");
            try
            {
                if (File.Exists(OutputFilePath)) {
                    data = File.ReadAllText(OutputFilePath, Encoding.GetEncoding("windows-1251"));
                    //File.Delete(OutputFilePath);
                    result = true;
                } else {
//                    var ErrorFilePath = Path.Combine(workDir, "1CError.xml");
//                    if (File.Exists(ErrorFilePath)) {
//                        data = File.ReadAllText(ErrorFilePath, Encoding.ASCII);
//                        File.Delete(ErrorFilePath);
//                    }
                }
            }
            catch (Exception ex)
            {
                Base.Log1("Error on " + className + ".doReportFileInput(): " + ex.Message);
            }
            return result;
        }

        public static T runReportProcessingData<T>(string workDir, string reports1c_Dir, string report1c_Name, T inputObjectClass)
        {
            var result = default(T);

            var inst1C = _1C.getInstance(reports1c_Dir);
            if (inst1C != null) {
                var stringInput = XML.ConvertClassToXMLText(inputObjectClass);
                if (inst1C.doReportFileInput(
                    workDir,
                    stringInput
                )) {
                    Base.DumpToFile(workDir, "(input-1C).xml", stringInput);

                    if (inst1C.runExternalReport(workDir, report1c_Name)) {
                        string stringOutput = "";
                        if (inst1C.doReportFileOutput(workDir, out stringOutput)) {
                            Base.DumpToFile(workDir, "(output-1C).xml", stringOutput);
                            result = XML.ConvertXMLTextToClass<T>(stringOutput);
                        }
                    } else {
                        Base.Log1("Error of run 1c report [" + report1c_Name + "]!");
                    }
                } else {
                    Base.Log1("Error before run 1c report [" + report1c_Name + "]: the problem with initialization of instance 1c!");
                }
            }

            return result;
        }


        public string getLogFile()
        {
            string result = "";
            var t = "\t:: ";
            Base.Log1("Загрузка 1c лог.файла [" + this.LogFilePath + "]:", t);

            if (File.Exists(this.LogFilePath)) {
                try
                {
                    result = File.ReadAllText(this.LogFilePath, Encoding.ASCII);
                }
                catch (Exception ex)
                {
                    Base.LogError("Error in 1C.getLogFile(): " + ex.Message);
                }
            } else
                Base.Log1("> Файл лога 1с не найден!", t);
            return result;
        }
    }
}