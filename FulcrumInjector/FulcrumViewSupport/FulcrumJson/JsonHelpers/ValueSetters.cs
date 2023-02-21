using System;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using SharpLogging;

namespace FulcrumInjector.FulcrumViewSupport.FulcrumJson.JsonHelpers
{
    /// <summary>
    /// Class used for setting values on the JSON Configuration values.
    /// </summary>
    public static class ValueSetters
    {
        #region Custom Events
        #endregion //Custom Events

        #region Fields

        // Helper logging object for logging changes in this class
        private static readonly SharpLogger _valueSettersLogger = new SharpLogger(LoggerActions.FileLogger);

        #endregion //Fields

        #region Properties
        #endregion //Properties

        #region Structs and Classes
        #endregion //Structs and Classes

        // ------------------------------------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// Stores a new value into the JSON objects on the config
        /// </summary>
        /// <typeparam name="TValueType">Type of value being set</typeparam>
        /// <param name="PropertyKey">Key to set</param>
        /// <param name="ValueObject">Value to store</param>
        /// <returns>True if the value is updated, false if not</returns>
        public static bool SetValue<TValueType>(string PropertyKey, TValueType ValueObject, bool AppendMissing = false)
        {
            // See if our config file is missing.
            if (!File.Exists(JsonConfigFiles.AppConfigFile))
                throw new InvalidOperationException("CAN NOT PULL CONFIG VALUES SINCE THE CONFIG FILE IS NOT YET BUILT!");

            // Build content and log information.
            _valueSettersLogger?.WriteLog($"STORING NEW VALUE INTO JSON CONFIG KEY {PropertyKey} NOW...");

            // Select the config item first
            string TypeOfConfig; string OutputPath = JsonConfigFiles.AppConfigFile;
            try { TypeOfConfig = PropertyKey.Contains('.') ? PropertyKey.Split('.').FirstOrDefault() : PropertyKey; }
            catch (Exception SetEx)
            {
                // Log failure and return false.
                _valueSettersLogger?.WriteLog("FAILED TO GET CURRENT CONFIG OBJECT TYPE FOR OUR JSON CONFIG!", LogType.ErrorLog);
                _valueSettersLogger?.WriteException("EXCEPTION THROWN DURING PULL CONFIG ROUTINE: ", SetEx);
                return false;
            }

            // Store the new content value here.
            try
            {
                // For a key value item object
                string[] SplitContentPath = PropertyKey.Split('.').Skip(1).ToArray();
                JObject ConfigObjectLocated = ValueLoaders.GetJObjectConfig(TypeOfConfig);
                _valueSettersLogger?.WriteLog("PULLED CONTENT FOR CONFIG FILE TO MODIFY OK!", LogType.TraceLog);

                // Check missing value here now.
                if (ConfigObjectLocated[SplitContentPath.FirstOrDefault() ?? PropertyKey] == null && !AppendMissing)
                {
                    // Log missing and return false.
                    _valueSettersLogger?.WriteLog($"ERROR! MISSING CONFIG FILE VALUE FOR OBJECT KEY NAME: {PropertyKey}!", LogType.ErrorLog);
                    _valueSettersLogger?.WriteLog($"NEW JSON VALUE WILL NOT BE SET!", LogType.ErrorLog);
                    return false;
                }

                // Log info and loop values here.
                ConfigObjectLocated[SplitContentPath.FirstOrDefault() ?? PropertyKey] = JToken.FromObject(ValueObject);

                // Set value into config file now.
                JsonConfigFiles.ApplicationConfig.Remove(TypeOfConfig);
                if (TypeOfConfig != "FulcrumUserSettings") JsonConfigFiles.ApplicationConfig.Add(TypeOfConfig, ConfigObjectLocated); 
                else { JsonConfigFiles.ApplicationConfig["FulcrumUserSettings"] = JArray.FromObject(ConfigObjectLocated["FulcrumUserSettings"]); }

                // Write out our JSON values here
                File.WriteAllText(OutputPath, JsonConfigFiles.ApplicationConfig.ToString(Formatting.Indented));
                _valueSettersLogger?.WriteLog($"STORED JSON CONFIG VALUE FOR PROPERTY {PropertyKey} OK!", LogType.InfoLog);
                return true;
            }
            catch (Exception SetEx)
            {
                // Log failure and return false.
                _valueSettersLogger?.WriteLog("FAILED TO STORE NEW VALUE FOR OUR JSON CONFIG!", LogType.ErrorLog);
                _valueSettersLogger?.WriteException("EXCEPTION THROWN DURING SET ROUTINE: ", SetEx);
                return false;
            }
        }
    }
}
