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

        public static IServer CreateServer(Action onConnected)
        {
            return CreateServer(0, onConnected);
        }

        public static IServer CreateServer(int portHint, Action onConnected)
        {
            return Server.Create(portHint, onConnected);
        }
    }
}
