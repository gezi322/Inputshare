using System;
using System.Collections.Generic;
using System.Configuration;
using System.Text;

namespace InputshareLib
{
    public static class ConfigFile
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

        public static bool TryRead(string prop, out string value)
        {
            lock (configLock)
            {
                if (config == null)
                    LoadFile();

                try
                {
                    value = config.AppSettings.Settings[prop].Value;
                    return true;
                }
                catch (Exception)
                {
                    value = "";
                    return false;
                }
            }
        }

        public static bool TryReadProperty(Enum prop, out string value)
        {
            return TryRead(prop.ToString(),out value);
        }

        public static bool TryWriteProperty(Enum prop, string value)
        {
            return TryWrite(prop.ToString(), value);
        }

        public static bool TryWrite(string prop, string value)
        {
            lock (configLock)
            {
                if (config == null)
                    LoadFile();


                try
                {
                    if (config.AppSettings.Settings[prop] == null)
                        config.AppSettings.Settings.Add(prop, value);
                    else
                        config.AppSettings.Settings[prop].Value = value;

                    config.Save();
                    return true;
                }
                catch (Exception ex)
                {
                    ISLogger.Write("Failed to save configuration: " + ex.Message);
                    return false;
                }
            }
        }
    }
}
