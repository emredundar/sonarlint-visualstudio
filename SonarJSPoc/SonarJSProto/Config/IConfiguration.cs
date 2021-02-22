using System;
using System.Collections.Generic;
using System.Linq;

namespace SonarJsConfig.Config
{
    // Corresponds to the Java interface "Configuration"
    public interface IConfiguration
    {
        // Methods to fetch other value types skipped

        /// <summary>
        /// Returns the string setting with the specified key, or null if not available
        /// </summary>
        string get(string key);

        /// <summary>
        /// Returns the list of strings for the specified key, or null if not available.
        /// </summary>
        string[] getStringArray(string key);
    }

    public class Configuration : IConfiguration
    {
        public static IConfiguration CreateFromEnvVars()
        {
            var config = new Configuration();
            config.AddFromEnvVar(GlobalVariableNames.GLOBALS_PROPERTY_KEY, GlobalVariableNames.GLOBALS_DEFAULT_VALUE);
            config.AddFromEnvVar(GlobalVariableNames.ENVIRONMENTS_PROPERTY_KEY, GlobalVariableNames.ENVIRONMENTS_DEFAULT_VALUE);

            return config;
        }

        private readonly IDictionary<string, string> settings = new Dictionary<string, string>();

        /// <summary>
        /// Adds a setting to the configuration. If an environment variable with the name of the key
        /// exists then the value of the enviroment variable is used.
        /// </summary>
        public void AddFromEnvVar(string propertyKey, string defaultValue)
        {
            var settingValue = Environment.GetEnvironmentVariable(propertyKey) ?? defaultValue;

            settings.Add(propertyKey, settingValue);
        }

        #region IConfiguration methods

        string IConfiguration.get(string key)
        {
            settings.TryGetValue(key, out var setting);
            return setting;
        }

        string[] IConfiguration.getStringArray(string key)
        {
            if(settings.TryGetValue(key, out var settingArray))
            {
                return settingArray.Split(',').Select(x => x.Trim()).ToArray();
            }
            return null;
        }

        #endregion
    }
}
