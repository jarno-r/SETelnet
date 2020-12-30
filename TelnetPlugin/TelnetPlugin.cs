using VRage.Plugins;

namespace SETelnetPlugin
{
    public class TelnetPlugin : IPlugin
    {
        public void Dispose()
        {
        }

        public void Init(object gameInstance)
        {
            System.IO.File.WriteAllText("c:\\users\\jarno\\documents\\telnetplugin.txt", "Init!\n");
        }

        public void Update()
        {
        }
    }
}
