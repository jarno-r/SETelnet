using System;
using Sandbox.ModAPI;
using VRage.Game.Components;
using Sandbox.Definitions;
using VRage.Game;
using VRage.Game.ModAPI;
using Sandbox.Game.GameSystems;
using Sandbox.Game.Entities;
using Sandbox.Common.ObjectBuilders;
using VRage.ObjectBuilders;

namespace TelnetMod
{
    [MyEntityComponentDescriptor(typeof(MyObjectBuilder_TerminalBlock), true, "ControlPanel", "SmallControlPanel")]
    public class TelnetMod : MyGameLogicComponent
    {
        private MyObjectBuilder_EntityBase objectBuilder;
        private IMyTerminalBlock block;

        public override MyObjectBuilder_EntityBase GetObjectBuilder(bool copy = false)
        {
            return copy ? (MyObjectBuilder_EntityBase)objectBuilder.Clone() : objectBuilder;
        }

        public override void Init(MyObjectBuilder_EntityBase objectBuilder)
        {
            this.objectBuilder = objectBuilder;
            block = (IMyTerminalBlock)Entity;

            using (var writer = MyAPIGateway.Utilities.WriteFileInLocalStorage("tellus.log", typeof(TelnetMod)))
            {
                writer.WriteLine("Init " + TelnetPlugin.ModAPI.Telnet.Hello);
                writer.Flush();
            }            
        }
    }
}
