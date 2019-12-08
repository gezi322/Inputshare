#if WindowsBuild


using System;
using System.Collections.Generic;
using System.Net;
using System.Text;
using InputshareLib;
using InputshareLibWindows.IPC.NetIpc;

namespace Inputshare
{
    class svc
    {
        public svc()
        {
            ISLogger.EnableConsole = true;
            Init();
        }

        NetIpcClient client;

        private void Init()
        {
            client = new NetIpcClient("Service");
            client.Connect(new IPEndPoint(IPAddress.Parse("192.168.0.8"), 4441));
            Console.ReadLine();
        }
    }
}

#endif
