using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;

namespace InputshareLib.Displays
{
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
                    vBounds = new Rectangle(l, b, Math.Abs(r - l), Math.Abs(t - b));

                    int count = br.ReadInt32();
                    for (int i = 0; i < count; i++)
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
                    foreach (var screen in Displays)
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

        public override bool Equals(object obj)
        {
            if (!(obj is DisplayConfig conf))
                return false;

            if (conf.VirtualBounds != VirtualBounds)
                return false;

            if (conf.Displays.Count != Displays.Count)
                return false;

            for (int i = 0; i < Displays.Count; i++)
            {

                if (Displays[i].Bounds != conf.Displays[i].Bounds)
                    return false;

                if (Displays[i].Index != conf.Displays[i].Index)
                    return false;

                if (Displays[i].Name != conf.Displays[i].Name)
                    return false;

                if (Displays[i].Primary != conf.Displays[i].Primary)
                    return false;

            }

            return true;
        }
    }
}
