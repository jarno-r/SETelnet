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
using Sandbox.ModAPI.Interfaces.Terminal;

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

            Log.Info("Init");

            var red = MyAPIGateway.TerminalControls.CreateControl<IMyTerminalControlLabel, Sandbox.ModAPI.Ingame.IMyTerminalBlock>("bobo");
            red.Enabled = _ => true;
            red.Visible = b => b.BlockDefinition.TypeId == block.BlockDefinition.TypeId && b.BlockDefinition.SubtypeId == block.BlockDefinition.SubtypeId;

            string title = TelnetPlugin.ModAPI.Telnet.Hello;

            red.Label = VRage.Utils.MyStringId.GetOrCompute(title);
            MyAPIGateway.TerminalControls.AddControl<Sandbox.ModAPI.Ingame.IMyTerminalBlock>(red);
        }
    }
}
