using System;
using System.Net;
using System.Net.Sockets;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using VRage.Plugins;
using VRage.Scripting;

namespace TelnetPlugin
{
    public class TelnetPlugin : IPlugin
    {
        public void Dispose()
        {
        }

        private static string LOG_FILE = "c:\\users\\jarno\\documents\\telnetplugin.txt";

        internal static void Log(string msg)
        {
            System.IO.File.AppendAllText(LOG_FILE, msg + "\n");
        }

        private static void RegisterPluginModAPI()
        {
            var name = Assembly.GetExecutingAssembly().GetName().Name;
            var path = new Uri(Assembly.GetExecutingAssembly().CodeBase).LocalPath;

            Log($"Registering assembly {name} @ {path}");

            MyScriptCompiler.Static.AddReferencedAssemblies(path);
            using (var handle = MyScriptCompiler.Static.Whitelist.OpenBatch())
            {
                handle.AllowNamespaceOfTypes(MyWhitelistTarget.ModApi, typeof(ModAPI.Telnet));
            }

            AppDomain.CurrentDomain.AssemblyResolve += (object sender, ResolveEventArgs args) =>
            {
                if (args.Name.StartsWith(name))
                {
                    return Assembly.GetExecutingAssembly();
                }
                else return null;
            };
        }

        private Server server;

        public void Init(object gameInstance)
        {
            Server.synchronizationContext = MySynchronizationContext.Instance;

            System.IO.File.WriteAllText(LOG_FILE, "Init!\n");

            RegisterPluginModAPI();

            server = Server.Create(57888, () =>
            {
                server.Write("Type in your name: ");
                server.ReadLine(name =>
                {
                    server.WriteLine($"Hello {name}");
                    server.Close();
                });
            });

            using (new UsingSynchronizationContext(MySynchronizationContext.Instance))
            {
                

                new Server2().Start();
            }
        }

        private int go = 0;
        public void Update()
        {
            using (new UsingSynchronizationContext(MySynchronizationContext.Instance))
            {
                MySynchronizationContext.Run();
            }
        }
    }

    public class Server2
    {
        public async void Start()
        {
            TcpListener server = new TcpListener(IPAddress.Any, 9999);

            TelnetPlugin.Log("Local Endpoint: " + server.LocalEndpoint);

            server.Start();

            var task = server.AcceptTcpClientAsync();
            TelnetPlugin.Log("Server started, waiting for connections...");

            var b = task.Wait(500);
            TelnetPlugin.Log($"{b} {task.IsCompleted} {task.IsFaulted} {task.Status}");

            try
            {
                TcpClient client = await task; //.ConfigureAwait(false);

                TelnetPlugin.Log("Got connected!");

                var data = Encoding.UTF8.GetBytes("Goop");
                client.GetStream().Write(data, 0, data.Length);
                client.Close();

                TelnetPlugin.Log("Done with it!");
            }
            catch (Exception e)
            {
                TelnetPlugin.Log("Exceptional: " + e);
            }
        }
    }
}
