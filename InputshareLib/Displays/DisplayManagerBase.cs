using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;

namespace InputshareLib.Displays
{
    public abstract class DisplayManagerBase
    {
        public bool Running { get; protected set; }
        public DisplayConfig CurrentConfig { get; protected set; } = new DisplayConfig(DisplayManagerBase.NullConfig);
        public abstract void StartMonitoring();
        public abstract void StopMonitoring();

        public abstract void UpdateConfigManual();

        public event EventHandler<DisplayConfig> DisplayConfigChanged;

        protected void OnConfigUpdated(DisplayConfig newConfig)
        {
            ISLogger.Write("DisplayManagerBase: Display config updated. ({0})", newConfig.VirtualBounds);
            DisplayConfigChanged?.Invoke(this, newConfig);
        }

        public static byte[] NullConfig { get
            {
                return new DisplayConfig(new Rectangle(0, 0, 1024, 768), new List<Display>()).ToBytes();
            }
        }

        
       
    }
}
