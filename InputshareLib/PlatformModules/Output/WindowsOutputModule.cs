using InputshareLib.Input;
using InputshareLib.PlatformModules.Windows;
using System.Threading.Tasks;


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
