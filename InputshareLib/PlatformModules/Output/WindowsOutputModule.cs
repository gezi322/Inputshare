using InputshareLib.Input;
using InputshareLib.PlatformModules.Windows;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using static InputshareLib.PlatformModules.Windows.Native.User32;


namespace InputshareLib.PlatformModules.Output
{
    /// <summary>
    /// Simulates user input on windows
    /// </summary>
    public class WindowsOutputModule : OutputModuleBase
    {
        public override void SimulateInput(ref InputData input)
        {
            WinInputSimulator.SendInput(ref input);
        }

        protected override Task OnStart()
        {
            return Task.CompletedTask;
        }

        protected override Task OnStop()
        {
            return Task.CompletedTask;
        }

    }
}
