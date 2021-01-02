using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TelnetPlugin.ModAPI
{
    public interface ITelnetServer
    {
        string Address { get; }

        int Port { get; }

        bool IsConnected { get; }

        void ReadLine(Action<string> d);

        void Write(string msg);

        void WriteLine(string msg);

        void Accept(Action onConnected);

        void Close();        
    }
}
