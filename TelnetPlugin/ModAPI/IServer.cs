using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TelnetPlugin.ModAPI
{
    public interface IServer
    {
        void ReadLine(Action<string> d);
        void Write(string msg);
        void WriteLine(string msg);

        string Address { get; }

        int Port { get; }

        void Close();
    }
}
