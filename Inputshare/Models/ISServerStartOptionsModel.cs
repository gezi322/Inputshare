using System;
using System.Collections.Generic;
using System.Text;

namespace Inputshare.Models
{
    internal sealed class ISServerStartOptionsModel
    {
        public bool EnableUdp { get; set; } = true;
        public bool EnableClipboard { get; set; } = true;
        public bool EnableDragDrop { get; set; } = true;

    }
}
