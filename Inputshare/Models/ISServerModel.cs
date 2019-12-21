using Avalonia.Input;
using InputshareLib;
using InputshareLib.Input.Hotkeys;
using InputshareLib.Input.Keys;
using InputshareLib.Server;
#if WindowsBuild
using InputshareLibWindows.Output;
using InputshareLibWindows.PlatformModules.Clipboard;
using InputshareLibWindows.PlatformModules.Displays;
using InputshareLibWindows.PlatformModules.DragDrop;
using InputshareLibWindows.PlatformModules.Input;
#endif
using System;
using System.Collections.Generic;
using System.Text;
using System.Windows.Input;

namespace Inputshare.Models
{
    internal sealed class ISServerModel
    {
        public event EventHandler ServerStarted;
        public event EventHandler ServerStopped;
        public event Action<ISClientInfoModel> ClientConnected;
        public event Action<ISClientInfoModel> ClientDisconnected;
        public event Action<ISClientInfoModel> InputClientSwitched;

        private ISServer serverInstance;

        internal ISServerModel()
        {
            serverInstance = new ISServer();
            serverInstance.Started += ServerInstance_Started;
            serverInstance.Stopped += ServerInstance_Stopped;
            serverInstance.ClientConnected += (object o, ISClientInfoModel c) => ClientConnected?.Invoke(c);
            serverInstance.ClientDisconnected += (object o, ISClientInfoModel c) => ClientDisconnected?.Invoke(c);
            serverInstance.InputClientSwitched += (object o, ISClientInfoModel c) => InputClientSwitched?.Invoke(c);
        }

        public void StartServer(int port, ISServerStartOptionsModel options)
        {
            List<string> o = new List<string>();
            if (!options.EnableClipboard)
                o.Add("noclipboard");
            if (!options.EnableDragDrop)
                o.Add("nodragdrop");
            if (!options.EnableUdp)
                o.Add("noudp");

            serverInstance.Start(GetPlatformDependencies(), new StartOptions(o), port);
        }

        public void StopServer()
        {
            serverInstance.Stop();
        }

        public ISClientInfoModel GetLocalHost()
        {
            return serverInstance.GetLocalhost();
        }

        public void SetClientHotkey(ISClientInfoModel client, ISHotkeyModel cHk)
        {
            HotkeyModifiers mods = 0;

            mods = cHk.Alt ? mods |= HotkeyModifiers.Alt : mods;
            mods = cHk.Ctrl ? mods |= HotkeyModifiers.Ctrl : mods;
            mods = cHk.Shift ? mods |= HotkeyModifiers.Shift : mods;

            //Translate from avalonia to windows virtual key

            try
            {
#if WindowsBuild
            System.Windows.Input.Key a = (System.Windows.Input.Key)cHk.Key;
            Hotkey k = new Hotkey((WindowsVirtualKey)KeyInterop.VirtualKeyFromKey(a), mods);
#else
                //Translate from avalonia key to windows virtual key
                //This is a dirty method but should work for the majority of keys
                var a = (WindowsVirtualKey)Enum.Parse(typeof(WindowsVirtualKey), cHk.Key.ToString());
                Hotkey k = new Hotkey(a, mods);
#endif

                client.SetHotkey(k);
            }catch(Exception ex)
            {
                ISLogger.Write("Failed to set hotkey: " + ex.Message);
            }


        }

        private void ServerInstance_Stopped(object sender, EventArgs e)
        {
            ServerStopped?.Invoke(this, null);
        }

        private void ServerInstance_Started(object sender, EventArgs e)
        {
            ServerStarted?.Invoke(this, null);
        }

        private static ISServerDependencies GetPlatformDependencies()
        {
#if WindowsBuild
            return new ISServerDependencies
            {
                ClipboardManager = new WindowsClipboardManager(),
                DisplayManager = new WindowsDisplayManager(),
                DragDropManager = new WindowsDragDropManager(),
                InputManager = new WindowsInputManager(),
                OutputManager = new WindowsOutputManager()
            };
#elif LinuxBuild
            return ISServerDependencies.GetLinuxDependencies();
#endif
        }
    }
}
