using System;
using System.Diagnostics;
using System.IO;


namespace DQM
{
    /// <summary>
    /// Creates EventLog object which interacts with Windows event logs
    /// </summary>
    public class ClassLog
    {
        const string LogName_ = "DQM"; // Log writer's Name
        const string Source_ = "DQM";
        const string SimpleLogPrefix_ = "{0:DQM_yyMMdd_HHmmss}.log";

        public enum enmEventId
        {
            Diag = 1,
            Start = 10,
            File = 20,
            Call = 100,
            Citect = 150,
            Bartender = 160,
            System = 200,
            Exception = 900,
            End = 999
        }

        private EventLog MyLogger1;
        private FileInfo SimpleLogFI;


        override public string ToString()
        {
            string s = string.Empty;
            if (MyLogger1 != null)
                s += string.Format("Logger Logname={0}, Source={1}", MyLogger1.LogDisplayName, MyLogger1.Source.ToString());
            if (SimpleLogFI != null)
                s += string.Format("\r\nSimpleLog filename={0}", SimpleLogFI.FullName);
            return string.IsNullOrEmpty(s) ? "Not set" : s;
        }
        /// <summary>
        /// creates EventLog object
        /// </summary>
        private ClassLog()
        {
            if (!EventLog.SourceExists(Source_))
                EventLog.CreateEventSource(Source_, LogName_);
            MyLogger1 = new EventLog(LogName_, ".", Source_);


            var tmpFolder = Path.GetTempPath();
            string fileName = System.IO.Path.Combine(tmpFolder, string.Format(SimpleLogPrefix_, DateTime.Now));
            SimpleLogFI = new FileInfo(fileName);


            var sApp = SimpleLogFI.CreateText();
            sApp.WriteLine(string.Format("{0}\t{1}\t{2}", "Timestamp", "Client", "Message"));
            sApp.Close();
        }

        private static ClassLog _instance = null;
        /// <summary>
        /// creates EventLog object ones
        /// </summary>
        /// <returns>ClassLog object</returns>
        public static ClassLog getInstance()
        {
            if(_instance == null)
            {
                _instance = new ClassLog();
            }
            return _instance;
        }

        public void Write(
                   string strMessage,
                   System.Diagnostics.EventLogEntryType EntType = EventLogEntryType.Information,
                   enmEventId EventID = enmEventId.Call)
        {
            enmEventId Id = EventID;

            try
            {
                MyLogger1.WriteEntry(strMessage, EntType, (int)EventID);
            }
            catch (Exception ex)
            {
                SimpleLog("EventLogger exception: " + ex.ToString() + "\r\nWriting message here: " + strMessage);
            }
        }

        public void SimpleLog(String msg, bool isSuccessSent = false)
        {
            var sApp = SimpleLogFI.AppendText();
            string outcomeMessage = isSuccessSent ? "message was sent successfully" : "sending message was failed   ";
            sApp.WriteLine(string.Format("{0:dd/MM/yy HH:mm:ss}\t{1}\t{2}", outcomeMessage, DateTime.Now, msg));
            sApp.Close();
        }
        public string GetSimpleLog()
        {
            var sapp = SimpleLogFI.OpenText();
            var t = sapp.ReadToEnd();
            sapp.Close();
            return t;
        }
    }
}
