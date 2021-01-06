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

namespace TelnetPlugin
{
    public class TelnetServer : ModAPI.ITelnetServer
    {
        public static int MAX_SERVERS = 10;
        public static int MIN_PORT = 57000;
        public static int MAX_PORT = 58000;

        private static int RETRIES = 10;

        const int BUFFER_SIZE = 1024;
        const int READ_BUFFER_SIZE = BUFFER_SIZE;
        const int LINE_BUFFER_SIZE = BUFFER_SIZE;
        const int SEND_BUFFER_SIZE = BUFFER_SIZE;

        /*
         * Helper to manage buffered reading.
         * 
         * To avoid a call to an async function on each byte, the property NeedsMore
         * indicates if more bytes should be read from the source. GetsMore() is used to read the bytes.
         * Thus, each ReadByte() or PeekByte() should be preceded by 
         * if (rb.NeedsMore) await rb.GetsMore();
         */
        private class ReadBuffer
        {
            private int pos, len;
            private byte[] buffer;
            private TelnetServer server;

            public ReadBuffer(TelnetServer server)
            {
                this.server = server;
            }

            public bool NeedsMore => pos >= len;

            public async Task GetsMore()
            {
                if (pos < len) return;

                if (buffer == null) buffer = new byte[READ_BUFFER_SIZE];

                pos = 0;
                len = 0;    // Important to set len=0, because ReadAsync might fail.

                len = await server.ReadAsync(buffer);
            }

            public int PeekByte()
            {
                if (pos >= len) return -1;
                return buffer[pos];
            }

            public int ReadByte()
            {
                if (pos >= len) return -1;
                return buffer[pos++];
            }
        }

        private static int nServers = 0;

        private bool readPending;
        private bool writePending;
        private bool readReady = true;
        private Queue<string> writeQueue = new Queue<string>();
        private Action<string> waitingRead;

        private CancellationTokenSource source;
        private byte[] sendBuffer;
        private byte[] lineBuffer;
        ReadBuffer readBuffer;

        private TcpClient client;
        TcpListener listener;
        Stream stream;

        ~TelnetServer()
        {
            Close();
        }

        private static string _Address;
        public static string GetAddress()
        {
            if (_Address != null) return _Address;
            foreach (NetworkInterface ni in NetworkInterface.GetAllNetworkInterfaces())
            {
                var gateway = ni.GetIPProperties().GatewayAddresses.FirstOrDefault();
                if (gateway != null && !gateway.Address.ToString().Equals("0.0.0.0"))
                {
                    foreach (var addr in ni.GetIPProperties().UnicastAddresses)
                    {
                        if (addr.Address.AddressFamily == AddressFamily.InterNetwork)
                        {
                            _Address = addr.Address.ToString();
                            return _Address;
                        }
                    }
                }
            }
            _Address="<NoIP>";
            return _Address;
        }

        private static Random random = new Random();

        private static int RandomPort()
        {
            return random.Next(MIN_PORT, MAX_PORT);
        }

        private TelnetServer(TcpListener listener)
        {
            readBuffer = new ReadBuffer(this);
            this.listener = listener;
            Port = ((IPEndPoint)listener.LocalEndpoint).Port;
            nServers++;

            Log.Info($"TelnetServer created with port {Port}");
        }


        private async void Start(Action onConnected)
        {
            Log.Info($"TelnetServer starting with port {Port}");

            Port = (listener.LocalEndpoint as IPEndPoint).Port;

            Log.Info("Waiting for connections on " + listener.LocalEndpoint.ToString());

            try
            {
                client = await listener.AcceptTcpClientAsync();

                Log.Info($"Accepted a connection on {listener.LocalEndpoint} from {client.Client.RemoteEndPoint}");

                listener.Stop();
                listener = null;

                stream = client.GetStream();

                onConnected();
            } catch(Exception e)
            {
                Log.Error("Failed to accept a connetion on port {Port}. Server closed.\nException: {e}");
                Close();
            }
        }

        public static TelnetServer Create(int portHint)
        {
            if (portHint == 0) portHint = RandomPort();

            Log.Info($"Creating server with requested port {portHint}.");

            if (portHint < MIN_PORT || portHint >= MAX_PORT)         
            {
                Log.Error("Bad port.");
                return null;
            }

            if (nServers >= MAX_SERVERS) {
                Log.Error("Too many servers.");
                return null;
            }

            TcpListener listener;

            for (int t = 0; t < RETRIES; t++)
            {
                try
                {
                    listener = new TcpListener(IPAddress.Any, portHint);
                    listener.Start();

                    return new TelnetServer(listener);
                }
                catch (Exception e)
                {
                    Log.Info("Couldn't create TCPListener: " + e);
                }

                portHint = RandomPort();
            }

            Log.Error("Failed to create a server.");

            return null;
        }

        public async Task<string> ReadLineAsync()
        {
            if (lineBuffer == null) lineBuffer = new byte[LINE_BUFFER_SIZE];

            int p = 0;

            using (source = new CancellationTokenSource())
            {
                try
                {
                    while (p < lineBuffer.Length)
                    {
                        if (readBuffer.NeedsMore) await readBuffer.GetsMore();
                        int b = readBuffer.ReadByte();

                        // Convert CRLFs and lone CRs to LFs.
                        if (b == '\r')
                        {
                            if (readBuffer.NeedsMore) await readBuffer.GetsMore();
                            if (readBuffer.PeekByte() == '\n') readBuffer.ReadByte();
                            b = '\n';
                        }

                        if (b == '\n' || b < 0) break;

                        if (b == 0xff) // IAC (Interpret as Command)
                        {
                            if (readBuffer.NeedsMore) await readBuffer.GetsMore();
                            b = readBuffer.ReadByte();

                            if (b == 251 || b == 252 || b == 253 || b == 254)
                            {
                                // DO, DON'T, WILL, WON'T
                                // Skip the option code
                                if (readBuffer.NeedsMore) await readBuffer.GetsMore();
                                readBuffer.ReadByte();
                            }
                            else if (b == 250)
                            {
                                // SB
                                // Skip until SE.
                                do
                                {
                                    if (readBuffer.NeedsMore) await readBuffer.GetsMore();
                                    b = readBuffer.ReadByte();
                                } while (b != 240 && b >= 0);
                            }
                            else
                            {
                                // One of the other codes, or transmission error.
                                // Just skip it.
                            }
                        }
                        else
                        {
                            lineBuffer[p++] = (byte)b;
                        }
                    }
                }
                catch (OperationCanceledException)
                {
                    Log.Info($"Read cancelled on port {Port}");

                    readReady = true;
                    source = null;
                    
                    return null;
                }
                catch(Exception e)
                {
                    Log.Error($"Read failed on port {Port}. Server Closed.\nException: {e}");
                    Close();
                    return null;
                }
            }

            readReady = true;
            source = null;

            if (p > 0)
                return Encoding.ASCII.GetString(lineBuffer, 0, p);
            else return "";
        }

        private async Task<int> ReadAsync(byte[] data)
        {
            if (stream == null) return 0;
            try
            {
                return await stream.ReadAsync(data, 0, data.Length, source.Token);
            }
            catch (IOException ex)
            {
                Close();
                Log.Info($"IOException while reading @ port {Port}. Server closed.\n{ex}");
                return 0;
            }
        }

        public async Task DequeueReadLineAsync()
        {
            readPending = true;
            while (IsConnected && waitingRead != null)
            {
                readReady = false;
                var d = waitingRead;
                waitingRead = null;
                var line = await ReadLineAsync();
                if (line != null) d(line);
            }
            readPending = false;
        }

        public async void QueueReadLineAsync(Action<string> d)
        {
            if (readReady && waitingRead==null)
            {
                waitingRead = d;
                if (!readPending) await DequeueReadLineAsync();
            } else
            {
                Log.Error($"Read pending on {Port}");
            }
        }

        private async Task SendAsync(byte[] data, int length)
        {
            if (stream != null)
            {
                try
                {
                    await stream.WriteAsync(data, 0, length);
                } catch(IOException ex)
                {
                    Close();
                    Log.Info($"IOException while writing @ port {Port}. Server closed.\n{ex}");
                }
            }
        }

        // Send bytes, converting LFs to CRLFs.
        private async Task SendWithCRLFAsync(byte[] str)
        {
            if (sendBuffer == null) sendBuffer = new byte[SEND_BUFFER_SIZE];

            int p = 0;
            for (int i = 0; i < str.Length; i++)
            {
                if (p > sendBuffer.Length - 2)
                {
                    await SendAsync(sendBuffer, p);
                    p = 0;
                }

                if (str[i] == '\n' && (i == 0 || str[i - 1] != '\r'))
                {
                    sendBuffer[p++] = (byte)'\r';
                }
                sendBuffer[p++] = str[i];
            }

            await SendAsync(sendBuffer, p);
        }

        private async void QueueWriteAsync(string msg)
        {
            writeQueue.Enqueue(msg);
            if (!writePending)
            {
                writePending = true;
                while (writeQueue.Count > 0)
                {
                    var m = writeQueue.Dequeue();
                    var bytes = Encoding.UTF8.GetBytes(m);
                    await SendWithCRLFAsync(bytes);
                }
                writePending = false;
            }
        }

        // --------------------------------
        // Implementation of ITelnetServer
        // --------------------------------
        public string Address => GetAddress();

        public int Port { get; private set; }

        public bool IsConnected => client != null;

        public bool IsClosed => client == null && listener == null;

        public void ReadLine(Action<string> d)
        {
            if (client == null) return;

            using (MySynchronizationContext.Using())
                QueueReadLineAsync(d);
        }

        public void Write(string msg)
        {
            if (client == null) return;

            using (MySynchronizationContext.Using())
                QueueWriteAsync(msg);
        }

        public void WriteLine(string msg)
        {
            Write(msg + "\n");
        }

        public void Accept(Action onConnected)
        {
            if (listener == null)
            {
                Log.Error($"Tried to restart server {Port}.");
                return;
            }
            using (MySynchronizationContext.Using())
            {
                Start(onConnected);
            }
        }

        public void Close()
        {
            if (client != null || listener != null) nServers--;

            client?.Close();
            client = null;

            listener?.Stop();
            listener = null;

            stream?.Close();
            stream = null;
        }

        public void CancelRead()
        {
            if (source!=null)
            {
                source.Cancel();
                readReady = true;
            }
        }
    }
}
