using System;
using System.Configuration;

namespace InputshareLib
{
    public static class Config
    {
        private static Configuration config;
        private static object configLock = new object();

        static Config()
        {
            try
            {
                config = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);
                config.Save();
            }
            catch (ConfigurationErrorsException ex)
            {
                ISLogger.Write("Config error: " + ex.Message);
            }

        }

        public static string Read(ConfigProperty property)
        {
            lock (configLock)
            {
                try
                {
                    if (config.AppSettings.Settings[property.ToString()] == null)
                        return "";
                    else
                    {
                        string val = config.AppSettings.Settings[property.ToString()].Value;
                        return val;
                    }

                }
                catch (Exception ex)
                {
                    ISLogger.Write("Config: Failed to read property {0}: {1}", property, ex.Message);
                    return "";
                }
            }


        }

        public static void Write(ConfigProperty property, string value)
        {
            lock (configLock)
            {
                try
                {
                    if (config.AppSettings.Settings[property.ToString()] == null)
                        config.AppSettings.Settings.Add(property.ToString(), value);
                    else
                        config.AppSettings.Settings[property.ToString()].Value = value;
                    config.Save();
                }
                catch (Exception ex)
                {
                    ISLogger.Write("Config: Failed to write to config: " + ex.Message);
                }
            }


        }

        public enum ConfigProperty
        {
            LastConnectionState,
            LastConnectedAddress,
            LastClientName,
            LastClientGuid,
            AutoReconnectEnabled,
        }
    }
}
