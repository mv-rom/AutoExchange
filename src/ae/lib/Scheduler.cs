using System;
using System.Collections.Generic;
using System.IO;
//using System.Threading.Tasks;


namespace ae.lib
{
    public class Scheduler
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
            this.DataFilePath =  Path.Combine(Base.RunDir, DataFileName);
            this.data =  new List<SchedulerData>();
            this.tasks = new List<SchedulerTaskData>();
        }

        public bool Init()
        {
            Base.Log(" > Scheduler() is initialized:");
            foreach (var t in Base.Config.ConfigSettings.SchedulerSetting.tasks)
            {
                if (t.Service.Length>0 && t.Action.Length > 0) {
                    this.tasks.Add(t);
                }
            }

            if (this.tasks.Count <= 0) {
                Base.Log1("Warning in Scheduler.Init(): There is no one task is configured.");
                return false;
            }
            return true;
        }

        public void DeInit()
        {
            this.data = null;
        }


        public void Run()
        {
            var tb = "|> ";
            Base.Log1("Run task:", tb);
            if (this.tasks.Count > 0) {
                this.loadData();
                foreach(var t in this.tasks)
                {
                    if (t.Service.Length > 0 && t.Action.Length > 0) {
                        string Name = t.Service + "." + t.Action;
                        Base.Log1("|--> task [" + Name + "]:");

                        byte lastStatus = 0;
                        int lastRunTime = 0;
                        SchedulerData taskData = this.getData(Name);
                        if (taskData != null) {
                            lastStatus = taskData.lastStatus;
                            lastRunTime = taskData.lastRunTime;
                        }

                        var stc = new SchedulerTask();
                        if (lastStatus == 0 || stc.checkStartTime(t.StartTime, lastRunTime)) {
                            lastStatus = RunAction(t.Service, t.Action);
                            lastRunTime = (int)Base.getCurentUnixDateTime();
                            this.setData(Name, lastRunTime, lastStatus);
                        } else {
                            Base.Log1(@"\__ passed..");
                        }
                        Base.Log1("");
                    } else {
                        Base.Log1("Warning in Scheduler.Run(): some field of service ["+t.Service+"] or action ["+t.Action+"] is empty.");
                    }
                }
                this.saveData();
            }
            else {
                Base.Log1(@"\__ There is no one task to execution.", tb);
            }
        }

        public byte RunAction(string ServiceName, string ActionName, string t = "")
        {
            byte result = 0;
            t = (t.Length > 0) ? t : "\t:: ";
            Base.Log1(" > Run action [" + ActionName + "] in service ["+ServiceName+"]:", t);
            try
            {
                Service service = null;
                if (Base.Services.TryGetValue(ServiceName, out service)) {
                    service.RunAction(ActionName);
                    result = 1;
                }
            }
            catch (Exception ex)
            {
                Base.LogError("Error in Scheduler.RunAction(): execution is unsuccessful!");
                Base.LogError("> " + ex.Message);
            }
            return result;
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
                    string msg = "Error in Scheduler.loadData(): there is the problem to load a data-file: " + this.DataFilePath + "!";
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
                string msg = "Error in Scheduler.saveData(): there is the problem to save a data-file: " + this.DataFilePath + "!";
                msg = msg + "> "+ex.Message;
                Base.LogError(msg);
                throw new Exception(msg);
            }
        }
    }

    [Serializable]
    public class SchedulerData
    {
        public string Name { get; set; }
        public int lastRunTime { get; set; }
        public byte lastStatus { get; set; }
    }

    [Serializable]
    public class SchedulerTaskData
    {
        public string StartTime { get; set; }
        public string StartDaysOfWeek { get; set; }
        public string Service { get; set; }
        public string Action { get; set; }
        public string Extra { get; set; }
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
