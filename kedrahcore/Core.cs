using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Kedrah {
    public class Core {
        #region Objects/Variables

        static private System.Threading.Mutex kedrahMutex;

        public Tibia.Objects.Client Client = null;
        public Tibia.Objects.Screen Screen = null;
        public Tibia.Objects.Player Player = null;
        public Tibia.Objects.Map Map = null;
        public Tibia.Objects.BattleList BattleList = null;
        public Tibia.Objects.Inventory Inventory = null;
        public Tibia.Objects.Console Console = null;
        public Tibia.Packets.ProxyBase Proxy = null;

        public HModules Modules;

        #endregion

        #region Constructor

        public Core()
            : this("Kedrah Core", "", true, false) {
        }

        public Core(string clientChooserTitle, string mutexName, bool hookProxy, bool useWPF) {
            Tibia.KeyboardHook.Enable();

            do {
                Tibia.Util.ClientChooserOptions clientChooserOptions = new Tibia.Util.ClientChooserOptions();
                clientChooserOptions.Title = clientChooserTitle;
                clientChooserOptions.ShowOTOption = true;

                if (useWPF)
                    Client = Tibia.Util.ClientChooserWPF.ShowBox(clientChooserOptions);
                else
                    Client = Tibia.Util.ClientChooser.ShowBox(clientChooserOptions);

                if (Client != null) {
                    kedrahMutex = new System.Threading.Mutex(true, "Kedrah_" + mutexName + Client.Process.Id.ToString());

                    if (!kedrahMutex.WaitOne(0, false)) {
                        Client = null;
                        continue;
                    }

                    System.Diagnostics.Process.GetCurrentProcess().PriorityClass = System.Diagnostics.ProcessPriorityClass.AboveNormal;

                    if (hookProxy)
                        Proxy = new Tibia.Packets.HookProxy(Client);
                    else if (Client.LoggedIn) {
                        Client = null;
                        continue;
                    }
                    else {
                        Client.IO.StartProxy();
                        Proxy = Client.IO.Proxy;
                    }

                    Client.Process.Exited += new EventHandler(ClientClosed);
                    Proxy.ReceivedSelfAppearIncomingPacket += new Tibia.Packets.ProxyBase.IncomingPacketListener(OnLogin);
                    Proxy.ReceivedLogoutOutgoingPacket += new Tibia.Packets.ProxyBase.OutgoingPacketListener(OnLogout);

                    Modules = new HModules(this);

                    if (Client.LoggedIn) {
                        System.Threading.Thread.Sleep(500);
                        OnLogin(null);
                    }
                }

                break;
            } while (Client == null);
        }

        #endregion

        #region Core Functions

        private bool OnLogin(Tibia.Packets.IncomingPacket packet) {
            Map = Client.Map;
            Screen = Client.Screen;
            BattleList = Client.BattleList;
            Inventory = Client.Inventory;
            Console = Client.Console;
            System.Threading.Thread.Sleep(300);
            Player = Client.GetPlayer();
            Modules.Enable();

            return true;
        }

        private bool OnLogout(Tibia.Packets.OutgoingPacket packet) {
            Modules.Disable();

            if (Client.Window.WorldOnlyView)
                Client.Window.WorldOnlyView = false;

            return true;
        }

        void ClientClosed(object sender, EventArgs args) {
            Environment.Exit(0);
        }

        #endregion
    }
}
