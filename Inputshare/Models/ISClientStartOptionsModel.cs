using System;
using System.Collections.Generic;
using System.Text;

namespace Inputshare.Models
{
    internal class ISClientStartOptionsModel
    {
        public bool AutoReconnect { get; set; } = false;
        public bool EnableUdp { get; set; } = true;
        public bool EnableClipboard { get; set; } = true;
        public bool EnableDragDrop { get; set; } = true;
    }
}
