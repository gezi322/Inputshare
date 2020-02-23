using System;
using System.Collections.Generic;
using System.Configuration;
using System.Text;

namespace Inputshare.Common
{
    /// <summary>
    /// Reads/Writes to/from the Inputshare.dll.config file
    /// </summary>
    internal static class DllConfig
    {
        private static object confLock = new object();
        private static Configuration configFile;

        private static bool loadConfig()
        {
            try
            {
                lock (confLock)
                {
                    configFile = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);
                    return true;
                }
            }
            catch (Exception ex)
            {
                //Logger.Write($"Failed to open configuration file: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// reads a display property from the .dll.config file
        /// </summary>
        /// <param name="display"></param>
        /// <param name="prop"></param>
        /// <param name="value"></param>
        /// <returns>Returns true if value is read</returns>
        internal static bool TryReadProperty(string prop, out string value)
        {
            lock (confLock)
            {
                value = "";
                if (configFile == null)
                    if (!loadConfig())
                        return false;

                try
                {
                    if (configFile.AppSettings.Settings[prop] != null)
                    {
                        value = configFile.AppSettings.Settings[prop].Value;
                        return true;
                    }
                    else
                    {
                        return false;
                    }
                }
                catch (Exception)
                {
                    return false;
                }
            }
        }

        /// <summary>
        /// Writes a display property to the .dll.config file
        /// </summary>
        /// <param name="display"></param>
        /// <param name="prop"></param>
        /// <param name="value"></param>
        /// <returns>Returns true if value is written successfully</returns>
        internal static bool TryWrite(string prop, string value)
        {
            lock (confLock)
            {
                if (configFile == null)
                    if (!loadConfig())
                        return false;

                try
                {
                    if (configFile.AppSettings.Settings[prop.ToString()] == null)
                        configFile.AppSettings.Settings.Add(prop.ToString(), value);
                    else
                        configFile.AppSettings.Settings[prop.ToString()].Value = value;

                    configFile.Save();
                    return true;
                }
                catch (Exception ex)
                {
                    Logger.Write($"Failed to save config property: {ex.Message}");
                    return false;
                }
            }
        }
    }
}
