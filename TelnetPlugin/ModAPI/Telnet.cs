using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TelnetPlugin.ModAPI
{
    public static class Telnet
    {
        public static string Hello = "Howdy";

        public static ITelnetServer CreateServer()
        {
            return CreateServer(0);
        }

        public static ITelnetServer CreateServer(int portHint)
        {
            return TelnetServer.Create(portHint);
        }

        public static string Address => TelnetServer.GetAddress();
    }
}
