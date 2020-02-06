using System;
using System.Collections.Generic;
using System.Text;

namespace InputshareLib
{
    public class Logger
    {
        public static void Write(string message, params object[] args)
        {
            Write(string.Format(message, args));
        }

        public static void Write(string message)
        {
            Console.WriteLine(string.Format(message));
        }
    }
}
