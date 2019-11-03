using System;
using System.Collections.Generic;
using System.Text;

namespace InputshareLib.Net.Udp
{
    public enum UdpMessageType
    {
        UNKOWN = 0,
        ClientOK = 1,
        HostOK = 2,
        Input = 3
    }
}
