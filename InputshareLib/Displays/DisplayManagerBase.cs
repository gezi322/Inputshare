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

        public class DisplayConfig
        {
            public DisplayConfig(Rectangle virtualBounds, List<Display> displays)
            {
                VirtualBounds = virtualBounds;
                Displays = displays;

                foreach (var display in displays.Where(i => i.Primary))
                    PrimaryDisplay = display;
            }
            public Rectangle VirtualBounds { get; }
            public List<Display> Displays { get; }
            public Display PrimaryDisplay { get; }

            public DisplayConfig(byte[] data)
            {
                List<Display> displays = new List<Display>();
                Rectangle vBounds = new Rectangle();

                using (MemoryStream ms = new MemoryStream(data))
                {
                    using (BinaryReader br = new BinaryReader(ms))
                    {
                        int l = br.ReadInt32();
                        int t = br.ReadInt32();
                        int r = br.ReadInt32();
                        int b = br.ReadInt32();
                        vBounds = new Rectangle(l, b,Math.Abs(r - l), Math.Abs(t - b));

                        int count = br.ReadInt32();
                        for(int i =0; i < count; i++)
                        {
                            bool primary = br.ReadBoolean();
                            string name = br.ReadString();
                            int index = br.ReadInt32();
                            l = br.ReadInt32();
                            t = br.ReadInt32();
                            r = br.ReadInt32();
                            b = br.ReadInt32();
                            Rectangle bounds = new Rectangle(l, b, Math.Abs(r - l), Math.Abs(t - b));
                            displays.Add(new Display(bounds, index, name, primary));
                        }
                    }
                }

                VirtualBounds = vBounds;
                Displays = displays;
                foreach (var display in displays.Where(i => i.Primary))
                    PrimaryDisplay = display;
            }

            public byte[] ToBytes()
            {
                using (MemoryStream ms = new MemoryStream())
                {
                    using (BinaryWriter bw = new BinaryWriter(ms))
                    {
                        bw.Write(VirtualBounds.Left);
                        bw.Write(VirtualBounds.Top);
                        bw.Write(VirtualBounds.Right);
                        bw.Write(VirtualBounds.Bottom);
                        bw.Write(Displays.Count);
                        foreach(var screen in Displays)
                        {
                            bw.Write(screen.Primary);
                            bw.Write(screen.Name);
                            bw.Write(screen.Index);
                            bw.Write(screen.Bounds.Left);
                            bw.Write(screen.Bounds.Top);
                            bw.Write(screen.Bounds.Right);
                            bw.Write(screen.Bounds.Bottom);
                        }
                    }
                    return ms.ToArray();
                }
            }
        }
        public class Display
        {
            public Display(Rectangle bounds, int index, string displayName, bool primary)
            {
                Bounds = bounds;
                Index = index;
                Name = displayName;
                Primary = primary;
            }

            public Rectangle Bounds { get; }
            public int Index { get; }
            public string Name { get; }
            public bool Primary { get; }
        }
    }
}
