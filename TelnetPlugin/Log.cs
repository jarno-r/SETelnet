using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TelnetPlugin
{
    static class Log
    {
        public static void Info(string msg)
        {
            //TelnetPlugin.Log(msg);
            Debug.WriteLine("INFO: "+msg);
        }

        public static void Error(string msg)
        {
            Info("ERROR: "+msg);
        }
    }
}
