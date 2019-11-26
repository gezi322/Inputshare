using InputshareLib;
using InputshareLibWindows.IPC.AnonIpc;
using InputshareLibWindows.Windows;
using System;
using System.Diagnostics;
using System.Security.Principal;
using System.Threading;

namespace InputshareSP
{
    public sealed class InputshareSP
    {
        static void Main(string[] args)
        {
            AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;
            ISLogger.SetLogFileName("InputshareSP_Main.log");
            ISLogger.EnableConsole = true;
            ISLogger.EnableLogFile = true;
            ISLogger.PrefixTime = true;

            if (args.Length != 3)
            {
                OnInvalidArgs();
                return;
            }

            string mode = args[0];
            string readPipe = args[1];
            string writePipe = args[2];


            PrintInfo();

            if (mode == "Default")
            {
                ISLogger.SetLogFileName("InputshareSP_DefaultHost.log");
                new SPDefaultHost(readPipe, writePipe);
            }
                
            else if (mode == "DragDrop")
            {
                ISLogger.SetLogFileName("InputshareSP_DragDropHost.log");
                new SPDragDropHost(readPipe, writePipe);
            }
                
            else
                OnInvalidArgs();

            return;
        }

        private static void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            Exception ex = e.ExceptionObject as Exception;
            ISLogger.Write("---------------------------------");
            ISLogger.Write("Unhandled exception!");
            ISLogger.Write(ex.Message);
            ISLogger.Write(ex.StackTrace);
            ISLogger.Write("---------------------------------");
            Thread.Sleep(5000);
            Process.GetCurrentProcess().Kill();
        }

        private static void PrintInfo()
        {
            IntPtr token = IntPtr.Zero;
            try
            {
                ISLogger.Write("InputshareSP running as user " + Environment.UserName);
                token = Token.GetCurrentProcessToken();
                ISLogger.Write("Elevation type: " + Token.QueryElevation(token));
                ISLogger.Write("Session ID: " + Session.ConsoleSessionId);
                ISLogger.Write("Session state: " + Session.ConsoleSessionState);
                ISLogger.Write(@"Desktop: Winsta0\\" + Desktop.CurrentDesktop);
            }
            finally
            {
                if(token != IntPtr.Zero)
                    Token.CloseToken(token);
            }
        }

        static void OnInvalidArgs()
        {
            ISLogger.Write("InputshareSP started with invalid args...");
            Thread.Sleep(2000);
        }
    }
}
