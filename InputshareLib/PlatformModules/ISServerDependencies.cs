using InputshareLib.PlatformModules.Input;
using InputshareLib.PlatformModules.Output;
using System;
using System.Collections.Generic;
using System.Text;

namespace InputshareLib.PlatformModules
{
    /// <summary>
    /// Contains modules required to run inputshare server
    /// </summary>
    public class ISServerDependencies
    {
        public InputModuleBase InputModule;
        public OutputModuleBase OutputModule;
    }
}
