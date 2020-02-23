using System;
using System.Collections.Generic;
using System.Drawing;
using System.Text;
using System.Threading.Tasks;
using Inputshare.Common.Clipboard;
using Inputshare.Common.Input;
using Inputshare.Common.PlatformModules;
using Inputshare.Common.PlatformModules.Clipboard;
using Inputshare.Common.PlatformModules.Input;
using Inputshare.Common.PlatformModules.Output;
using Inputshare.Common.Server.Config;

namespace Inputshare.Common.Server.Display
{
    /// <summary>
    /// Represents the virtual display of the server machine
    /// </summary>
    public class LocalDisplay : DisplayBase
    {
        private readonly InputModuleBase _inputModule;
        private readonly OutputModuleBase _outputModule;
        private readonly ClipboardModuleBase _clipboardModule;

        internal LocalDisplay(ISServerDependencies deps, ObservableDisplayList displayList) : base(displayList, deps.InputModule.VirtualDisplayBounds, "Localhost")
        {
            _inputModule = deps.InputModule;
            _outputModule = deps.OutputModule;
            _clipboardModule = deps.ClipboardModule;

            _clipboardModule.ClipboardChanged += (object o, ClipboardData cbData) => base.OnClipboardChanged(cbData);
            _inputModule.SideHit += (object o, SideHitArgs args) => base.OnSideHit(args.Side, args.PosX, args.PosY);
            Logger.Information($"Created localhost display ({_inputModule.VirtualDisplayBounds}");
        }

        internal override void SendInput(ref InputData input)
        {
           _outputModule.SimulateInput(ref input);
        }

        internal override void NotfyInputActive()
        {
            Logger.Verbose($"Notifying local display active");

            _inputModule.SetInputRedirected(false);

            if (ServerConfig.HideCursor)
                _inputModule.SetMouseHidden(false);
        }

        internal override void NotifyClientInvactive()
        {
            Logger.Verbose($"Notifying local display inactive");

            _inputModule.SetInputRedirected(true);

            if(ServerConfig.HideCursor)
                _inputModule.SetMouseHidden(true);
        }

        internal override async Task SetClipboardAsync(ClipboardData cbData)
        {
            Logger.Verbose($"Setting local display clipboard");
            await _clipboardModule.SetClipboardAsync(cbData);
        }
    }
}
