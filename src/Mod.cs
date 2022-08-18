using System;


namespace DQM
{
    class Mod
    {
        /// <summary>
        /// shortcut to access Appsettingd
        /// </summary>
        /// <param name="settname"></param>
        /// <param name="defval"></param>
        /// <returns></returns>
        static public string AppSett(string settname, string defval = "")
        {
            string v = System.Configuration.ConfigurationManager.AppSettings[settname];
            if (string.IsNullOrEmpty(v))
                if (string.IsNullOrEmpty(defval))
                    throw new Exception(string.Format("", settname));
                else
                    return defval;
            else
                return v;
        }

        /// <summary>
        /// creates web servise that provide SMS sending method
        /// </summary>
        /// <returns></returns>
        static public WS.Service1 CreateWSMaaleSer1() {
            WS.Service1 ws = new WS.Service1();
            ws.Url = Properties.Settings.Default.GlobalSer1;
            ws.UseDefaultCredentials = true;
            return ws;
        }
    }
}
