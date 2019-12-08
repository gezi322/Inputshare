using System;
using System.Collections.Generic;
using System.Configuration;
using System.Text;

namespace InputshareLib
{
    public static class Config
    {
        private static object configLock = new object();
        private static Configuration config;

        public static void LoadFile()
        {
            lock (configLock)
            {
                try
                {
                    config = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);
                }catch(Exception ex)
                {
                    ISLogger.Write("Failed to load config: " + ex.Message);
                }
            }
        }

        public static bool TryReadProperty(Enum prop, out string value)
        {
            lock (configLock)
            {
                try
                {
                    value = config.AppSettings.Settings[prop.ToString()].Value;
                    return true;
                }
                catch (Exception)
                {
                    value = "";
                    return false;
                }
            }
            
        }

        public static bool TryWrite(Enum prop, string value)
        {
            lock (configLock)
            {
                try
                {
                    if (config.AppSettings.Settings[prop.ToString()] == null)
                        config.AppSettings.Settings.Add(prop.ToString(), value);
                    else
                        config.AppSettings.Settings[prop.ToString()].Value = value;

                    config.Save();
                    return true;
                }catch(Exception ex)
                {
                    ISLogger.Write("Failed to save configuration: " + ex.Message);
                    return false;
                }
            }
        }
    }
}
