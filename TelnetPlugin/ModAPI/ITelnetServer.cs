using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TelnetPlugin.ModAPI
{
    public interface ITelnetServer
    {
        /// <summary>
        /// The server IP address.
        /// </summary>
        string Address { get; }

        /// <summary>
        /// The server TCP port.
        /// </summary>
        int Port { get; }

        /// <summary>
        /// Is a client connected?
        /// </summary>
        bool IsConnected { get; }

        /// <summary>
        /// Is the server closed.
        /// </summary>
        bool IsClosed { get; }

        /// <summary>
        /// Read a line from the client.
        /// Returned line does not include EOL.
        /// Only one read operation can be active at a time.
        /// </summary>
        /// <param name="onLineRead">Called when a line has been received.</param>
        void ReadLine(Action<string> onLineRead);

        /// <summary>
        /// Cancel a previously initiated ReadLine().
        /// </summary>
        void CancelRead();

        /// <summary>
        /// Write to the client.
        /// </summary>
        /// <param name="msg"></param>
        void Write(string msg);

        /// <summary>
        /// Write to the client and append an EOL.
        /// </summary>
        /// <param name="msg"></param>
        void WriteLine(string msg);

        /// <summary>
        /// Accept a connection from the client.
        /// Can only be called once after the server is created.
        /// </summary>
        /// <param name="onConnected"></param>
        void Accept(Action onConnected);

        /// <summary>
        /// Close the server and any connection.
        /// The server cannot be restarted.
        /// </summary>
        void Close();        
    }
}
