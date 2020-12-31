using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace TelnetTestServer
{
    public class Server
    {
        public static int MAX_SERVERS = 10;
        public static int MIN_PORT = 57000;
        public static int MAX_PORT = 58000;

        private static int RETRIES = 10;
        private int BUFFER_SIZE = 1024;

        private static int nServers = 0;

        private byte[] buffer;

        private SynchronizationContext _synchronizationContext;
        internal SynchronizationContext synchronizationContext
        {
            get
            {
                if (_synchronizationContext == null)
                    _synchronizationContext = SynchronizationContext.Current;

                return _synchronizationContext;
            }
            set { _synchronizationContext = value; }
        }

        

        private TcpClient client;
        TcpListener listener;
        Stream stream;

        ~Server()
        {
            Close();
        }

        public void Close()
        {
            if (client != null || listener != null) nServers--;

            if (client != null)
            {
                client.Close();
                nServers--;
            }
            client = null;

            if (listener != null)
            {
                listener.Stop();
            }

            listener = null;

            if (stream != null) stream.Close();
        }

        public static string GetAddress()
        {
            foreach (NetworkInterface ni in NetworkInterface.GetAllNetworkInterfaces())
            {
                var gateway = ni.GetIPProperties().GatewayAddresses.FirstOrDefault();
                if (gateway != null && !gateway.Address.ToString().Equals("0.0.0.0"))
                {
                    foreach (var addr in ni.GetIPProperties().UnicastAddresses)
                    {
                        if (addr.Address.AddressFamily == AddressFamily.InterNetwork)
                            return addr.Address.ToString();
                    }
                }
            }
            return null;
        }

        private static Random random = new Random();

        private static int RandomPort()
        {
            return random.Next(MIN_PORT, MAX_PORT);
        }

        private Server(TcpListener listener, Action onConnected)
        {
            this.listener = listener;

            using (new UsingSynchronizationContext(synchronizationContext))
            {
                Start(onConnected);
            }
        }

        private async void Start(Action onConnected)
        {
            nServers++;

            Port = (listener.LocalEndpoint as IPEndPoint).Port;

            Debug.WriteLine("Waiting for connections on " + listener.LocalEndpoint.ToString());
            listener.Start();
            client = await listener.AcceptTcpClientAsync();

            Debug.WriteLine($"Accepted a connection on {listener.LocalEndpoint} from {client.Client.RemoteEndPoint}");

            listener.Stop();
            listener = null;

            stream = client.GetStream();

            onConnected();
        }

        public string Address => GetAddress();

        public int Port { get; private set; }

        public static Server Create(Action onConnected)
        {
            return Create(0, onConnected);
        }

        public static Server Create(int portHint, Action onConnected)        
        {
            if (portHint == 0) portHint = RandomPort();

            if (portHint < MIN_PORT || portHint>=MAX_PORT)
            {
                return null;
            }

            if (nServers >= MAX_SERVERS) return null;

            TcpListener listener;

            for (int t = 0; t < RETRIES; t++)
            {
                try
                {
                    listener = new TcpListener(IPAddress.Any, portHint);

                    return new Server(listener, onConnected);
                }
                catch (Exception e)
                {
                    Debug.WriteLine("Couldn't create TCPListener: " + e);
                }

                portHint = RandomPort();
            }

            return null;
        }

        public void Write(string msg)
        {
            Send(Encoding.ASCII.GetBytes(msg));
        }

        public void WriteLine(string msg)
        {
            Write(msg + "\r\n");
        }

        // Eliminate Telnet option negotiation.
        private static int StripOptions(byte[] buffer, int count)
        {
            for (int i = 0; i < count; i++)
            {
                // If Telnet IAC, skip three bytes.
                if (buffer[i] == 0xff)
                {
                    // If at the end of the buffer, skip remainder.
                    if (i + 3 >= count) return i;

                    count -= 3;

                    // Copy stuff backwards to remove option.
                    Array.Copy(buffer, i + 3, buffer, i, count-i);

                    i--;
                }
            }
            return count;
        }

        private async Task<int> Recv()
        {
            if (buffer == null) buffer = new byte[BUFFER_SIZE];
            var count = await stream.ReadAsync(buffer);

            return StripOptions(buffer, count);
        }

        private async void ReadLineAsync(Action<string> d)
        {
            if (stream!=null)
            {
                var count = await Recv();
                Debug.WriteLine("Got " + count);
                for(int i=0;i<count;i++)
                {
                    Debug.WriteLine(buffer[i]);
                }
                if (count>0)
                {
                    d(Encoding.ASCII.GetString(buffer, 0, count));
                } else
                {
                    ReadLineAsync(d);
                }
            }
        }

        public void ReadLine(Action<string> d)
        {
            if (client != null) ReadLineAsync(d);
        }

        private async void Send(byte[] data)
        {
            if (stream != null)
            {
                await stream.WriteAsync(data);
            }
        }
    }
}
