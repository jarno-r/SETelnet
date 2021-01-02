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
using VRage.ModAPI;

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

            NeedsUpdate |= MyEntityUpdateEnum.EACH_10TH_FRAME;
        }

        private int go = 0;

        private TelnetPlugin.ModAPI.ITelnetServer server;

        public override void UpdateAfterSimulation10()
        {
            if (go == 0)
            {
                Log.Info("First update 10");
            }

            go++;
            if (go == 10)
            {
                Log.Info("Starting Server");

                server = TelnetPlugin.ModAPI.Telnet.CreateServer(57889);
                server.Accept(() =>
            {
                server.WriteLine("Jenny from the block here.");
                server.Write("Type in your name: ");
                server.ReadLine(name =>
                {
                    server.WriteLine($"Hello {name}");
                    server.Close();
                });
            });
            }
        }
    }
}
