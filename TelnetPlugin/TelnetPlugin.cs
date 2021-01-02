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

        private TelnetServer server;

        private static MySynchronizationContext mySynchronizationContext;
        public static SynchronizationContext synchronizationContext => mySynchronizationContext;

        public static MySynchronizationContext.UsingSynchronizationContext UsingSC()
        {
            SynchronizationContext sc = synchronizationContext;
            if (sc == null) sc = SynchronizationContext.Current;
            return new MySynchronizationContext.UsingSynchronizationContext(sc);
        }

        public void Init(object gameInstance)
        {
            System.IO.File.WriteAllText(LOG_FILE, "Init!\n");

            RegisterPluginModAPI();

            mySynchronizationContext = new MySynchronizationContext();

            server = TelnetServer.Create(57888);
            server.Accept(() =>
            {
                server.Write("Type in your name: ");
                server.ReadLine(name =>
                {
                    server.WriteLine($"Hello {name}");
                    server.Close();
                });
            });
        }

        public void Update()
        {
            using (MySynchronizationContext.Using())
            {
                mySynchronizationContext.Run();
            }
        }
    }
}
