using System.Drawing;

namespace InputshareLib.Displays
{
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
