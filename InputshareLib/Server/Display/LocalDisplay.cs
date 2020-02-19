using System;
using System.Collections.Generic;
using System.Drawing;
using System.Text;
using System.Threading.Tasks;
using InputshareLib.Clipboard;
using InputshareLib.Input;
using InputshareLib.PlatformModules;
using InputshareLib.PlatformModules.Clipboard;
using InputshareLib.PlatformModules.Input;
using InputshareLib.PlatformModules.Output;

namespace InputshareLib.Server.Display
{
    public class LocalDisplay : DisplayBase
    {
        private readonly InputModuleBase _inputModule;
        private readonly OutputModuleBase _outputModule;
        private readonly ClipboardModuleBase _clipboardModule;

        internal LocalDisplay(ISServerDependencies deps) : base(deps.InputModule.VirtualDisplayBounds, "Localhost")
        {
            _inputModule = deps.InputModule;
            _outputModule = deps.OutputModule;
            _clipboardModule = deps.ClipboardModule;

            _clipboardModule.ClipboardChanged += (object o, ClipboardData cbData) => base.OnClipboardChanged(cbData);
            _inputModule.SideHit += (object o, SideHitArgs args) => base.OnSideHit(args.Side, args.PosX, args.PosY);
        }

        internal override void SendInput(ref InputData input)
        {
           _outputModule.SimulateInput(ref input);
        }

        internal override Task NotfyInputActiveAsync()
        {
            _inputModule.SetInputRedirected(false);
            _inputModule.SetMouseHidden(false);
            return Task.CompletedTask;
        }

        internal override Task NotifyClientInvactiveAsync()
        {
            _inputModule.SetInputRedirected(true);
            _inputModule.SetMouseHidden(true);
            return Task.CompletedTask;
        }

        internal override async Task SetClipboardAsync(ClipboardData cbData)
        {
            await _clipboardModule.SetClipboardAsync(cbData);
        }
    }
}
