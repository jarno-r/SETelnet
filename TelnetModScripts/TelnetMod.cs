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
using System.Collections.Generic;
using System.Linq;
using Sandbox.Game.Entities.Cube;
using TelnetPlugin.ModAPI;

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

            NeedsUpdate |= MyEntityUpdateEnum.EACH_10TH_FRAME;
        }

        private ITelnetServer server;
        private bool ServerActive = false;

        public void CreateUI()
        {
            Log.Info("Creating Terminal Control!");

            //Func<IMyTerminalBlock,bool> visible = b => b.BlockDefinition.TypeId == block.BlockDefinition.TypeId && b.BlockDefinition.SubtypeId == block.BlockDefinition.SubtypeId;
            Func<IMyTerminalBlock, bool> visible = b => b.GameLogic is TelnetMod;


            const string LABEL_ID = "Dummy_Label_ID";
            var label = MyAPIGateway.TerminalControls.CreateControl<IMyTerminalControlLabel, Sandbox.ModAPI.Ingame.IMyTerminalBlock>(LABEL_ID);
            if (label.Id == LABEL_ID)
            {
                Log.Info("Amazing! Label ID actually works!");
            }

            label.Enabled = _ => true;
            label.Visible = visible;
            Log.Info($"Created {label.Id} {LABEL_ID}");

            string title = TelnetPlugin.ModAPI.Telnet.Hello;

            label.Label = VRage.Utils.MyStringId.GetOrCompute(title);

            MyAPIGateway.TerminalControls.AddControl<Sandbox.ModAPI.Ingame.IMyTerminalBlock>(label);

            
            var toggle = MyAPIGateway.TerminalControls.CreateControl<IMyTerminalControlOnOffSwitch, Sandbox.ModAPI.Ingame.IMyTerminalBlock>("TelnedMod_ServerToggle");
            toggle.Visible = visible;
            toggle.Title= VRage.Utils.MyStringId.GetOrCompute("Telnet Server");
            toggle.OnText=VRage.Utils.MyStringId.GetOrCompute("On");
            toggle.OffText = VRage.Utils.MyStringId.GetOrCompute("Off");
            toggle.Setter = (b,v) => ((TelnetMod)b.GameLogic).ServerActive=v;
            toggle.Getter= (b) => ((TelnetMod)b.GameLogic).ServerActive;

            //block.GetProperty("ServerActive");

            MyAPIGateway.TerminalControls.AddControl<Sandbox.ModAPI.Ingame.IMyTerminalBlock>(toggle);
            
        }

        private void ServerLoop(ITelnetServer server)
        {
            Log.Info($"Server {server.Port} looping.");
            if (!server.IsConnected) return;
            server.Write(">");
            server.ReadLine(line =>
            {
                if (line.StartsWith("help"))
                {
                    server.WriteLine("\nAcceptable commands: help, power");
                }
                else if (line.StartsWith("power"))
                {
                    float power = ComputePower();
                    server.WriteLine($"\nPower consumption: {power} MW");
                }
                else
                {
                    server.WriteLine("\nAcceptable commands: help, power");
                }
                ServerLoop(server);
            });
        }

        private bool FirstStart = true;
        private static bool FirstStatic = true;
        public override void UpdateAfterSimulation10()
        {
            if (FirstStatic)
            {
                FirstStatic = false;

                Log.Info("First extatic 5!");

                CreateUI();
            }

            if (FirstStart)
            {
                FirstStart = false;

                block.AppendingCustomInfo += (_, sb) => sb.Append($"Power: {ComputePower()} MW");

                //block.AppendingCustomInfo += (_, sb) => sb.Append($"IP: {Telnet.Address}");
                block.AppendingCustomInfo += (_, sb) => sb.Append("Okey");

                //block.AppendingCustomInfo += (_, sb) => { if (server != null) sb.Append($"Port: {server.Port}");  };
            }

            block.RefreshCustomInfo();

            //float power = ComputePower();


            if (!ServerActive && server!=null)
            {
                Log.Info("Closing server");
                server.Close();
                server = null;
            }
            if (ServerActive && server==null)
            {
                Log.Info("Starting server");
                server = Telnet.CreateServer(57777);
                var s = server;
                s.Accept(() =>
                {
                    s.WriteLine("Connected to SEOS.");
                    ServerLoop(s);
                });
            }



            /*
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
            */
        }

        public float ComputePower()
        {
            var ts = MyAPIGateway.TerminalActionsHelper.GetTerminalSystemForGrid(block.CubeGrid);
            var list = new List<IMyTerminalBlock>();
            ts.GetBlocksOfType<IMyPowerProducer>(list);


            float power = 0, max = 0;
            foreach (var b in list)
            {
                var p = (IMyPowerProducer)b;
                power += p.CurrentOutput;
                max += p.MaxOutput;
            }

            return power;
        }
    }
}
