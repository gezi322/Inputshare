using System.Diagnostics;

namespace InputshareLib
{
    public static class DebugUtils
    {
        public static void PrintMemoryUsage()
        {

            using (Process proc = Process.GetCurrentProcess())
            {
                using (PerformanceCounter pc = new PerformanceCounter())
                {
                    pc.CategoryName = "Process";
                    pc.CounterName = "Working Set - Private";
                    pc.InstanceName = proc.ProcessName;
                    ISLogger.Write("Memory usage: " + proc.WorkingSet64 / 1024 / 1024 + "MB");
                }
            }
        }
    }
}
