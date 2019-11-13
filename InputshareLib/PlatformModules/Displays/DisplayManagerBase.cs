using InputshareLib.Displays;
using System;
using System.Collections.Generic;
using System.Drawing;

namespace InputshareLib.PlatformModules.Displays
{
    public abstract class DisplayManagerBase : PlatformModuleBase
    {
        public DisplayConfig CurrentConfig { get; protected set; } = new DisplayConfig(DisplayManagerBase.NullConfig);
        public abstract void UpdateConfigManual();

        public event EventHandler<DisplayConfig> DisplayConfigChanged;

        public event EventHandler<Edge> EdgeHit;

        protected void OnConfigUpdated(DisplayConfig newConfig)
        {
            ISLogger.Write("DisplayManagerBase: Display config updated. ({0})", newConfig.VirtualBounds);
            DisplayConfigChanged?.Invoke(this, newConfig);
        }

        protected void OnEdgeHit(Edge edge)
        {
            EdgeHit?.Invoke(this, edge);
        }

        public static byte[] NullConfig
        {
            get
            {
                return new DisplayConfig(new Rectangle(0, 0, 1024, 768), new List<Display>()).ToBytes();
            }
        }



    }
}
