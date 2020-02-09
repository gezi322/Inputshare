using InputshareLib.Input;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace InputshareLib.PlatformModules.Output
{
    /// <summary>
    /// Base class for output modules. Used to simulate user mouse/keyboard input
    /// </summary>
    public abstract class OutputModuleBase : PlatformModuleBase
    {
        /// <summary>
        /// Simulates the specified user input
        /// </summary>
        /// <param name="input"></param>
        public abstract void SimulateInput(ref InputData input);
    }
}
