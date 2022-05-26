using System;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using SharpLogger.LoggerSupport;

namespace FulcrumInjector.FulcrumLogic.JsonLogic.JsonHelpers
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
            // See if our config file is missing.
            if (!File.Exists(JsonConfigFiles.AppConfigFile))
                throw new InvalidOperationException("CAN NOT PULL CONFIG VALUES SINCE THE CONFIG FILE IS NOT YET BUILT!");

            // Build content and log information.
            string NewContent = JsonConvert.SerializeObject(ValueObject, Formatting.Indented);
            JsonConfigFiles.ConfigLogger?.WriteLog($"STORING NEW VALUE INTO JSON CONFIG KEY {PropertyKey} NOW...");

            // Select the config item first\
            string TypeOfConfig;
            string OutputPath = JsonConfigFiles.AppConfigFile;
            try
            {
                TypeOfConfig = PropertyKey.Contains('.') ?
                    PropertyKey.Split('.').FirstOrDefault() :
                    PropertyKey;
            }
            catch (Exception SetEx)
            {
                // Log failure and return false.
                JsonConfigFiles.ConfigLogger?.WriteLog("FAILED TO GET CURRENT CONFIG OBJECT TYPE FOR OUR JSON CONFIG!", LogType.ErrorLog);
                JsonConfigFiles.ConfigLogger?.WriteLog("EXCEPTION THROWN DURING PULL CONFIG ROUTINE: ", SetEx);
                return false;
            }

            // Store the new content value here.
            try
            {
                // For a key value item object
                string[] SplitContentPath = PropertyKey.Split('.').Skip(1).ToArray();
                JObject ConfigObjectLocated = ValueLoaders.GetJObjectConfig(TypeOfConfig);
                JsonConfigFiles.ConfigLogger?.WriteLog("PULLED CONTENT FOR CONFIG FILE TO MODIFY OK!", LogType.TraceLog);

                // Check missing value here now.
                if (ConfigObjectLocated[SplitContentPath.FirstOrDefault() ?? PropertyKey] == null && !AppendMissing)
                {
                    // Log missing and return false.
                    JsonConfigFiles.ConfigLogger?.WriteLog($"ERROR! MISSING CONFIG FILE VALUE FOR OBJECT KEY NAME: {PropertyKey}!", LogType.ErrorLog);
                    JsonConfigFiles.ConfigLogger?.WriteLog($"NEW JSON VALUE WILL NOT BE SET!", LogType.ErrorLog);
                    return false;
                }

                // Log info and loop values here.
                ConfigObjectLocated[SplitContentPath.FirstOrDefault() ?? PropertyKey] = JToken.FromObject(ValueObject);
                JsonConfigFiles.ConfigLogger?.WriteLog($"STORED JSON CONFIG VALUE FOR PROPERTY {PropertyKey} OK!");

                // Set value into config file now.
                JsonConfigFiles.ApplicationConfig.Remove(TypeOfConfig);
                JsonConfigFiles.ApplicationConfig.Add(TypeOfConfig, ConfigObjectLocated);
                File.WriteAllText(OutputPath, JsonConfigFiles.ApplicationConfig.ToString(Formatting.Indented));
                JsonConfigFiles.ConfigLogger?.WriteLog($"STORED JSON CONFIG VALUE FOR PROPERTY {PropertyKey} OK!", LogType.InfoLog);

                // Log wrote out ok
                string NewFileJson = ConfigObjectLocated.ToString(Formatting.Indented);
                JsonConfigFiles.ConfigLogger?.WriteLog($"NEW FILE JSON VALUE:\n{NewFileJson}", LogType.TraceLog);

                // Refresh the Config Main object now.
                _ = JsonConfigFiles.ApplicationConfig;
                return true;
            }
            catch (Exception SetEx)
            {
                // Log failure and return false.
                JsonConfigFiles.ConfigLogger?.WriteLog("FAILED TO STORE NEW VALUE FOR OUR JSON CONFIG!", LogType.ErrorLog);
                JsonConfigFiles.ConfigLogger?.WriteLog("EXCEPTION THROWN DURING SET ROUTINE: ", SetEx);
                return false;
            }
        }
    }
}
