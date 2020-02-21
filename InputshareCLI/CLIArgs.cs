using System;
using System.Collections.Generic;
using System.Text;

namespace Inputshare
{
    public class CLIArgs
    {
        public CLIArgs(string[] args)
        {
            foreach(var arg in args)
            {

            }
        }

        public enum Arguments
        {
            StartServer,
            StartClient
        }
    }
}
