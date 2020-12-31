using Sandbox.ModAPI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VRage.Game.Components;

namespace TelnetMod
{
    [MySessionComponentDescriptor(MyUpdateOrder.NoUpdate)]
    public class Log : MySessionComponentBase
    {
        public static string LOG_FILE = "telnet.log";

        private static StringBuilder cache = new StringBuilder();
        private static System.IO.TextWriter writer = null;

        public static void Info(string msg)
        {
            cache.Append(DateTime.Now.ToString("[yyyy-MM-dd HH:mm:ss] "));
            cache.Append(msg+"\n");
            if (writer == null)
            {
                writer = MyAPIGateway.Utilities.WriteFileInLocalStorage(LOG_FILE, typeof(Log));
            }
            if (writer != null)
            {
                writer.Write(cache);
                writer.Flush();
                cache.Clear();
            }
        }

        protected override void UnloadData()
        {
            if (writer != null)
            {
                writer.Flush();
                writer.Close();
                writer = null;
            }
        }

        internal static void Error(string msg)
        {
            Log.Info(msg);
        }
    }
}


