using System;
using System.Collections.Generic;
using System.IO;
//using System.Threading.Tasks;


namespace ae.lib
{
    internal class Scheduler
    {
        private static Scheduler Instance = null;
        private string DataFilePath = "";
        private string DataFileName = "ae.scheduler_data";
        private List<SchedulerData> data;
        private List<SchedulerTaskData> tasks;


        public static Scheduler getInstance()
        {
            if (Scheduler.Instance == null)
            {
                Scheduler.Instance = new Scheduler();
                if (Scheduler.Instance.Init() != true) Scheduler.Instance.DeInit();
            }
            return Scheduler.Instance;
        }

        public Scheduler()
        {
            DataFilePath =  Path.Combine(Base.RunDir, DataFileName);
            data =  new List<SchedulerData>();
            tasks = new List<SchedulerTaskData>();
        }

        public bool Init()
        {
            bool result = false;

            this.data = new List<SchedulerData>();
            foreach (var t in Base.Config.ConfigSettings.SchedulerSetting.tasks)
            {
                this.tasks.Add(t);
            }

            return result;
        }

        public void DeInit()
        {
            this.data = null;
        }

        public int RunAction(string ActionName, string t="")
        {
            int result = 0;
            t = (t.Length > 0) ? t : "\t:: ";
            Base.Log1(" > Запуск активности - файл [" + ActionName + "]:", t);

            try
            {
                Action act = new Action(ActionName);
                if (act.Init()) {
                    if (act.Run()) {
                        Base.Log1("\\ активность выполнена.");
                        result = 1;
                    }
                    else
                        Base.Log1("\\ проблема с виполнением активности!");
                }
            }
            catch (Exception ex)
            {
                string msg = "Error in Scheduler.RunAction(): Активность выполнилась не успешно!";
                msg = msg + "> " + ex.Message;
                Base.LogError(msg);
            }
            return result;
        }

        public void Run()
        {
            var tb = "|> ";
            Base.Log1("Запуск задач:", tb);
            if (this.tasks.Count > 0) {
                this.loadData();
                foreach(var t in this.tasks)
                {
                    Base.Log1("|--> задача [" + t.Name + "]:");
                    SchedulerData taskData = this.getData(t.Name);
                    if (taskData != null) {
                        int lastRunTime = taskData.lastRunTime;
                        byte lastStatus = taskData.lastStatus;

                        var stc = new SchedulerTask();
                        if (lastStatus == 0 || stc.checkStartTime(t.Start, lastRunTime)) {
                            lastRunTime = (int)Base.getCurentUnixDateTime();

                            if (t.Action.Length > 0)
                                lastStatus = (byte)RunAction(t.Action);
                            else
                                Base.Log1("\\__ не выполнена! Поле action - не найдено!");

                            if (lastStatus == 1 && t.PosAction.Length > 0)
                                lastStatus = (byte)RunAction(t.PosAction);
                            else
                                Base.Log1("\\__ не выполнена! Поле post_action - не найдено!");

                            this.setData(t.Name, lastRunTime, lastStatus);
                        }
                        else
                            Base.Log1("\\__ пропущена..");
                        Base.Log1("");
                    }
                }
                this.saveData();
            }
            else {
                Base.Log1(@"\_ нет ни одной задачи на выполнение!", tb);
            }
        }

        public SchedulerData getData(string Name)
        {
            SchedulerData res = null;
            for (var i = 0; i < this.data.Count; i++)
            {
                if (this.data[i].Name.Equals(Name)) {
                    res = this.data[i];
                    break;
                }
            }
            return res;
        }

        public void setData(string Name, int intDateTime, byte Status)
        {
            bool t_existed = false;
            intDateTime = (intDateTime > 0) ? intDateTime : 0;

            for (var i = 0; i < this.data.Count; i++)
            {
                var d = this.data[i];
                if (d.Name.Equals(Name)) {
                    this.data[i].Name = Name;
				    this.data[i].lastRunTime = intDateTime;
				    this.data[i].lastStatus = Status;
                    t_existed = true;
                    break;
                }
            }

            if (t_existed == false) {
                this.data.Add(new SchedulerData() {
                    Name = Name,
			        lastRunTime = intDateTime,
			        lastStatus = Status
                });
            }
        }

        public void loadData()
        {
            if (File.Exists(this.DataFilePath)) {
                try
                {
                    using (var file = new System.IO.StreamReader(this.DataFilePath))
                    {
                        string line;
                        string[] arr;
                        while ((line = file.ReadLine()) != null)
                        {
                            //Console.WriteLine(line);
                            arr = line.Split(':');
                            if (arr.Length > 2) {
                                this.data.Add(new SchedulerData() {
                                    Name = arr[0],
                                    lastRunTime = int.Parse(arr[1]),
                                    lastStatus =  byte.Parse(arr[2])
                                });
                            }
                        }
                    }
                } catch (Exception ex) {
                    string msg = "Error in Scheduler.loadData(): Нет возможности или прав для открытия data-файла: " + this.DataFilePath + "!";
                    msg = msg + "> " + ex.Message;
                    Base.LogError(msg);
                    throw new Exception(msg);
                }
            }
        }

        public void saveData()
        {
            try
            {
                string buff = "";
                foreach (SchedulerData d in this.data) {
                    buff += d.Name+":"+d.lastRunTime + ":"+d.lastStatus + "\r\n";
                }
                File.WriteAllText(this.DataFilePath, buff);
            }
            catch (Exception ex)
            {
                string msg = "Error in Scheduler.saveData(): Нет возможности или прав для создания data-файла: " + this.DataFilePath + "!";
                msg = msg + "> "+ex.Message;
                Base.LogError(msg);
                throw new Exception(msg);
            }
        }
    }

    internal class SchedulerData
    {
        public string Name { get; set; }
        public int lastRunTime { get; set; }
        public byte lastStatus { get; set; }
    }
    internal class SchedulerTaskData
    {
        public string Start { get; set; }
        public string Name { get; set; }
        public string Report { get; set; }
        public string Action { get; set; }
        public string PosAction { get; set; }
    }


    internal class SchedulerTask
    {
        private string getDateTime(int unixDT)
        {
            var dt = new DateTime(unixDT); // * 1000
            return String.Format("{0:d2}:{1:d2}:{2:d2}", dt.Hour, dt.Minute, dt.Second);
        }

        public DateTime convertToDateTime(string strDateTime)
        {
            var arrTime = strDateTime.Trim().Split(':');
            if (arrTime.Length < 2)
                return DateTime.Now;
            var timestamp = DateTime.Now;
            var year = timestamp.Year;
            var mon = timestamp.Month;
            var day = timestamp.Day;

            int h = int.Parse(arrTime[0]);
            int m = int.Parse(arrTime[1]);
            int s = 0;

            return new DateTime(year, mon, day, h, m, s);
        }

        public bool checkStartTime(string strPlannedDT, int ExecutedDateTime)
        {
            var current_unixDT = Base.getCurentUnixDateTime();
            string[] arrPDT = strPlannedDT.Split(',');
            
	        foreach (var a in arrPDT)
            {
                var a_DT = a.Trim();
                if (a_DT.Length > 0) {
                    var planned_uDT = Base.getUnixDateTime(this.convertToDateTime(a_DT));
                    var executed_uDT = ExecutedDateTime;
                    if (current_unixDT > planned_uDT) {
                        if (current_unixDT > executed_uDT && executed_uDT < planned_uDT) return true;
                        /*
                            08:00 - planned_uDT
                                08:30 - executed_uDT
                                12:50 - current_uDT

                            13:00 - planned_uDT
                                08:30 - executed_uDT
                                13:10 - current_uDT
                        */
                    }
                }
            }
            return false;
        }
    }

}
