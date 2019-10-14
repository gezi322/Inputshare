using System;
using System.Collections.Generic;
using System.Text;
using InputshareLib.Clipboard.DataTypes;

namespace InputshareLib.Clipboard
{
    public class NullClipboardManager : ClipboardManagerBase
    {
        public override void SetClipboardData(ClipboardDataBase data)
        {

        }

        public override void Start()
        {
            Running = true;
        }

        public override void Stop()
        {
            Running = false;
        }
    }
}
