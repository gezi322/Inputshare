using System;
using System.Collections.Generic;
using System.Text;

namespace InputshareLib.Displays
{
    public class NullDisplayManager : DisplayManagerBase
    {
        public override void StartMonitoring()
        {
            Running = true;
        }

        public override void StopMonitoring()
        {
            Running = false;
        }

        public override void UpdateConfigManual()
        {

        }
    }
}
