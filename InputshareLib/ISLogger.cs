﻿using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

namespace InputshareLib
{
    public static class ISLogger
    {
        public static bool EnableConsole { get; set; }
        public static bool EnableLogFile { get; set; }
        public static string LogFilePath { get; private set; }
        public static bool PrefixTime { get; set; }
        public static bool PrefixCaller { get; set; }
        public static int LogCount { get; set; }

        public static int BufferedMessages { get => logWriteQueue.Count; }

        public static event EventHandler<string> LogMessageOut;

        private readonly static CancellationTokenSource cancelSource;
        private readonly static Task logWriteTask;
        private readonly static BlockingCollection<LogMessage> logWriteQueue;
        private readonly static object queueLock = new object();
        public static string LogFolder = SetLogFolder();

        static ISLogger()
        {
            cancelSource = new CancellationTokenSource();
            logWriteTask = new Task(LogWriteLoop);
            logWriteQueue = new BlockingCollection<LogMessage>();
            logWriteTask.Start();
        }

        private static string  SetLogFolder()
        {
#if LinuxBuild
            return Environment.GetFolderPath(Environment.SpecialFolder.Personal) + @"/Inputshare";
#elif WindowsBuild
            return Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData) + @"/Inputshare"; 
#endif
        }

        public static void Exit()
        {
            cancelSource.Cancel();
        }

        public static void SetLogFileName(string fName)
        {
            try
            {
                Directory.CreateDirectory(LogFolder);

                if (!File.Exists(LogFolder + "/"+fName))
                {
                    File.Create(LogFolder +"/"+  fName).Dispose();
                }

                LogFilePath = LogFolder + "/" + fName;
                ISLogger.Write("Log location: " + LogFilePath);
            }
            catch (Exception ex)
            {
                Console.WriteLine("ISLogger: Failed to set log file path: " + ex.Message);
            }
        }

        public static void Write(object messageObj)
        {
            Write(messageObj.ToString());
        }

        public static void Write(string message, params object[] args)
        {
            if (message == null)
                return;

            try
            {
                lock (queueLock)
                {
                    if (PrefixCaller)
                    {
                        logWriteQueue.Add(new LogMessage(string.Format(message, args), new StackTrace()));
                    }
                    else
                    {
                        logWriteQueue.Add(new LogMessage(string.Format(message, args)));
                    }
                }

            }
            catch { logWriteQueue.Add(new LogMessage(message)); };

        }

        private static void LogWriteLoop()
        {
            while (!cancelSource.IsCancellationRequested)
            {
                try
                {
                    LogMessage msg = logWriteQueue.Take(cancelSource.Token);
                    string message = msg.Message;

                    if (PrefixTime)
                        message = DateTime.Now.ToShortTimeString() + ": " + message;

                    if (PrefixCaller && msg.Stack != null)
                    {
                        MethodBase method = msg.Stack.GetFrame(2).GetMethod();

                        message = method.DeclaringType.Name + "." + method.Name + GenerateParamaterString(method.GetParameters()) + ":\n" + message + "\n";
                    }

                    if (Debugger.IsAttached)
                        Debug.WriteLine(message);

                    if (EnableConsole)
                        Console.WriteLine("Verbose: " + message);

                    LogCount++;

                    if (EnableLogFile && LogFilePath != null)
                        File.AppendAllText(LogFilePath, message + "\n");

                    LogMessageOut?.Invoke(null, message);
                }
                catch (Exception ex)
                {
                    Console.WriteLine("ISLogger: Error writing message: " + ex.Message);
                }

            }
        }

        private static string GenerateParamaterString(ParameterInfo[] info)
        {
            if (info == null || info.Length == 0)
                return "()";

            string paramsStr = "(";
            for (int i = 0; i < info.Length; i++)
            {
                ParameterInfo current = info[i];

                if (i == info.Length - 1)
                {
                    paramsStr = paramsStr + current.ParameterType + " " + current.Name + ")";
                }
                else
                {
                    paramsStr = paramsStr + current.ParameterType + " " + current.Name + ", ";
                }
            }
            return paramsStr;
        }

        private struct LogMessage
        {
            public LogMessage(string message, StackTrace stack = null)
            {
                Message = message;
                Stack = stack;
            }

            public string Message { get; }
            public StackTrace Stack { get; }
        }
    }
}
