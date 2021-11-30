using System;
using System.IO;
using System.Linq;
using FulcrumInjector.FulcrumLogging.LoggerSupport;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace FulcrumInjector.FulcrumJsonHelpers
{
    /// <summary>
    /// Class used for setting values on the JSON Configuration values.
    /// </summary>
    public static class ValueSetters
    {
        /// <summary>
        /// Stores a new value into the JSON objects on the config
        /// </summary>
        /// <typeparam name="TValueType">Type of value being set</typeparam>
        /// <param name="PropertyKey">Key to set</param>
        /// <param name="ValueObject">Value to store</param>
        /// <returns></returns>
        public static bool SetValue<TValueType>(string PropertyKey, TValueType ValueObject, bool AppendMissing = false)
        {
            // Build content and log information.
            string NewContent = JsonConvert.SerializeObject(ValueObject, Formatting.Indented);
            WatchdogJsonConfigFiles.ConfigLogger?.WriteLog($"STORING NEW VALUE INTO JSON CONFIG KEY {PropertyKey} NOW...");

            // Select the config item first.
            string OutputPath;
            JConfigType TypeOfConfig;
            try
            {
                TypeOfConfig = PropertyKey.Contains('.') ?
                    (JConfigType)Enum.Parse(typeof(JConfigType), PropertyKey.Split('.').FirstOrDefault()) :
                    (JConfigType)Enum.Parse(typeof(JConfigType), PropertyKey);

                // Pull the config value and set the value on it.
                OutputPath = Path.Combine(Directory.GetCurrentDirectory(), "JsonConfiguration", TypeOfConfig.ToString() + ".json");
            }
            catch (Exception SetEx)
            {
                // Log failure and return false.
                WatchdogJsonConfigFiles.ConfigLogger?.WriteLog("FAILED TO GET CURRENT CONFIG OBJECT TYPE FOR OUR JSON CONFIG!", LogType.ErrorLog);
                WatchdogJsonConfigFiles.ConfigLogger?.WriteLog("EXCEPTION THROWN DURING PULL CONFIG ROUTINE: ", SetEx);
                return false;
            }

            // Store the new content value here.
            try
            {
                // For a single item type/base config.
                if (!PropertyKey.Contains('.'))
                {
                    // Set the value here for full object.
                    File.WriteAllText(OutputPath, NewContent);
                    WatchdogJsonConfigFiles.ConfigLogger?.WriteLog($"STORED JSON CONFIG VALUE FOR PROPERTY {PropertyKey} OK!", LogType.InfoLog);
                    return true;
                }

                // For a key value item object
                string[] SplitContentPath = PropertyKey.Split('.').Skip(1).ToArray();
                JObject ConfigObjectLocated = ValueLoaders.GetJObjectConfig(TypeOfConfig);
                WatchdogJsonConfigFiles.ConfigLogger?.WriteLog("PULLED CONTENT FOR CONFIG FILE TO MODIFY OK!", LogType.TraceLog);

                // Check missing value here now.
                if (ConfigObjectLocated[SplitContentPath.FirstOrDefault()] == null && !AppendMissing)
                {
                    // Log missing and return false.
                    WatchdogJsonConfigFiles.ConfigLogger?.WriteLog($"ERROR! MISSING CONFIG FILE VALUE FOR OBJECT KEY NAME: {PropertyKey}!", LogType.ErrorLog);
                    WatchdogJsonConfigFiles.ConfigLogger?.WriteLog($"NEW JSON VALUE WILL NOT BE SET!", LogType.ErrorLog);
                    return false;
                }

                // Log info and loop values here.
                ConfigObjectLocated[SplitContentPath.FirstOrDefault()] = JToken.FromObject(ValueObject);
                string NewFileJson = ConfigObjectLocated.ToString(Formatting.Indented);
                WatchdogJsonConfigFiles.ConfigLogger?.WriteLog($"STORED JSON CONFIG VALUE FOR PROPERTY {PropertyKey} OK!");

                // Set value into config file now.
                File.WriteAllText(OutputPath, NewFileJson);
                WatchdogJsonConfigFiles.ConfigLogger?.WriteLog($"STORED JSON CONFIG VALUE FOR PROPERTY {PropertyKey} OK!", LogType.InfoLog);
                WatchdogJsonConfigFiles.ConfigLogger?.WriteLog($"NEW FILE JSON VALUE:\n{NewFileJson}", LogType.TraceLog);

                // Refresh the Config Main object now.
                _ = WatchdogJsonConfigFiles.ApplicationConfig;
                return true;
            }
            catch (Exception SetEx)
            {
                // Log failure and return false.
                WatchdogJsonConfigFiles.ConfigLogger?.WriteLog("FAILED TO STORE NEW VALUE FOR OUR JSON CONFIG!", LogType.ErrorLog);
                WatchdogJsonConfigFiles.ConfigLogger?.WriteLog("EXCEPTION THROWN DURING SET ROUTINE: ", SetEx);
                return false;
            }
        }
    }
}
