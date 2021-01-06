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
using VRage.Game.Entity.EntityComponents;

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
            base.Init(objectBuilder);

            this.objectBuilder = objectBuilder;
            block = (IMyTerminalBlock)Entity;

            Log.Info("Init");

            NeedsUpdate |= MyEntityUpdateEnum.EACH_FRAME;
        }

        private ITelnetServer server;
        private bool ServerActive = false;

        public TelnetMod()
        {
            Log.Info("Constructor TelnetMod()");
        }

        public void CreateUI()
        {
            Log.Info("Creating Terminal Controls!");

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
                    server.WriteLine("\nAcceptable commands: help, power, quit");
                }
                else if (line.StartsWith("power"))
                {
                    float power = ComputePower();
                    server.WriteLine($"\nPower consumption: {power} MW");
                }
                else if (line.StartsWith("quit"))
                {
                    server.Close();
                }
                else
                {
                    server.WriteLine("\nAcceptable commands: help, power, quit");
                }
                ServerLoop(server);
            });
        }

        private bool FirstStart = true;
        private static bool FirstStatic = true;

        public override void UpdateBeforeSimulation()
        {
            Update();
        }
        public override void UpdateAfterSimulation10()
        {
            Update();
        }

        public override void UpdateAfterSimulation()
        {
            Update();
        }

        private void Update()
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

                block.AppendingCustomInfo += (_, sb) => sb.Append($"UpdateModeToggle: {UpdateModeToggle}");
                block.AppendingCustomInfo += (_, sb) => sb.Append($"Power: {ComputePower()} MW\n");

                block.AppendingCustomInfo += (_, sb) =>
                {
                    try { sb.Append($"IP: {Telnet.Address}\n"); } catch (Exception e) { sb.Append($"MOMO {e}\n"); }
                };

                block.AppendingCustomInfo += (_, sb) => { if (server != null) sb.Append($"Port: {server.Port}\n");  };
            }

            block.RefreshCustomInfo();

            if (!ServerActive && server!=null)
            {
                Log.Info("Closing server");
                server.Close();
                server = null;

                ForceUIUpdate();
            }
            if (ServerActive && server==null)
            {
                Log.Info("Starting server");
                server = Telnet.CreateServer(57777);
                var s = server;
                if (s != null)
                {
                    s.Accept(() =>
                    {
                        s.WriteLine("Connected to SETMOS (Space Engineers Telnet Mod Operating System).");
                        ServerLoop(s);
                    });

                    ForceUIUpdate();
                } else
                {
                    Log.Info("Couldn't create a server");
                    ServerActive = false;
                }
            }
            if (ServerActive && server!=null && server.IsClosed)
            {
                ServerActive = false;
                server = null;
                ForceUIUpdate();
            }
        }

        private static bool UpdateModeToggle;

        /// <summary>
        /// Force terminal UI to update.
        /// Because it doesn't by itself, when calling RefreshCustomInfo or setting CustomName
        /// </summary>
        public void ForceUIUpdate()
        {
            // Inexplicably, Sandbox.Game.Entities is accessible with all the MyWhatnotsitBlocks in there,
            // but Sandbox.Game.Entities.Cube and Sandbox.Game.Entities.Block are their respective classes are not. 
            // Calling MyTerminalBlock.RaisePropertiesChanged should refresh the UI,
            // but MyTerminalBlock is in Sandbox.Game.Entities.Cube and not accessible.
            // But for example MyAdvancedDoor is a subclass of MyTerminalBlock and is in Sandbox.Game.Entities.

            // Two ways to hack it.
            if (UpdateModeToggle) ForceUIUpdateByOwnershipChange(block);
            else ForceUIUpdateByOnOffToggle(block);

            UpdateModeToggle = !UpdateModeToggle;
        }


        public static IMyTerminalControlOnOffSwitch refreshtoggle;

        public static void ForceUIUpdateByOnOffToggle(IMyTerminalBlock block)
        {
            if (refreshtoggle == null)
            {
                List<IMyTerminalControl> items;
                MyAPIGateway.TerminalControls.GetControls<IMyTerminalBlock>(out items);

                foreach (var item in items)
                {
                    if (item.Id == "ShowInToolbarConfig")
                    {
                        refreshtoggle = (IMyTerminalControlOnOffSwitch)item;
                        break;
                    }
                }
            }

            if (refreshtoggle != null)
            {
                var originalSetting = refreshtoggle.Getter(block);
                refreshtoggle.Setter(block, !originalSetting);
                refreshtoggle.Setter(block, originalSetting);

            }
        }

        public static void ForceUIUpdateByOwnershipChange(IMyTerminalBlock block)
        {
            var cb = ((MyCubeBlock)block);

            var owner = block.OwnerId;
            var shareMode = cb.IDModule.ShareMode;

            /*
             * if (cb.IDModule == null) {
             *   var sorter = block as IMyTerminalBlock;
             *   if (sorter != null)
             *   {
             *     sorter.ShowOnHUD = !sorter.ShowOnHUD;
             *     sorter.ShowOnHUD = !sorter.ShowOnHUD;
             *   }
             *   return;
             * }
             */

            cb.ChangeOwner(owner, MyOwnershipShareModeEnum.None == shareMode ? MyOwnershipShareModeEnum.Faction : MyOwnershipShareModeEnum.None);
            cb.ChangeOwner(owner, MyOwnershipShareModeEnum.None);
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
