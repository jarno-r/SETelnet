using System;
using System.Reflection;
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

        private static void Log(string msg)
        {
            System.IO.File.AppendAllText(LOG_FILE, msg);
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

        public void Init(object gameInstance)
        {
            System.IO.File.WriteAllText(LOG_FILE, "Init!\n");

            RegisterPluginModAPI();
        }

        public void Update()
        {
        }
    }
}
