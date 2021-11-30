using System;
using System.IO;
using FulcrumInjector.FulcrumLogging.LoggerSupport;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace FulcrumInjector.FulcrumJsonHelpers
{
    /// <summary>
    /// Contains methods for loading config values.
    /// </summary>
    public static class ValueLoaders
    {
        /// <summary>
        /// Pulls a JSON Object value from the given query path and converts it to an object.
        /// </summary>
        /// <typeparam name="TValueType"></typeparam>
        /// <param name="JsonPath"></param>
        /// <returns></returns>
        public static TValueType GetConfigValue<TValueType>(string JsonPath)
        {
            // Get the token first.
            JsonPath = JsonPath.Replace("FulcrumInjectorConfig", "");
            WatchdogJsonConfigFiles.ConfigLogger?.WriteLog($"TRYING TO PULL VALUE AT: {JsonPath}", LogType.TraceLog);
            var ValueObject = WatchdogJsonConfigFiles.ApplicationConfig.SelectToken(JsonPath);
            if (ValueObject == null)
            {
                WatchdogJsonConfigFiles.ConfigLogger?.WriteLog($"ERROR! VALUE PULLED AT PATH GIVEN WAS NULL!", LogType.TraceLog);
                return (TValueType)new object();
            }

            // If not null, convert and return.
            var ConvertedValue = ValueObject.ToObject<TValueType>();
            WatchdogJsonConfigFiles.ConfigLogger?.WriteLog($"PROPERTY: {JsonPath} | VALUE: {JsonConvert.SerializeObject(ConvertedValue, Formatting.None)}", LogType.TraceLog);
            return ConvertedValue;
        }

        /// <summary>
        /// Tries to get a jobject from our master config file type
        /// </summary>
        /// <param name="JObjectKey">Base Type of a json key</param>
        /// <returns></returns>
        public static JObject GetJObjectConfig(JConfigType JObjectKey)
        {
            // Check for full config.
            string ConfigKeyString = JObjectKey.ToString();
            WatchdogJsonConfigFiles.ConfigLogger?.WriteLog($"PULLING CONFIG VALUE FOR TYPE {ConfigKeyString}", LogType.TraceLog);
            try
            {
                // Try and get the current object. If failed, return null
                string ConfigSection = ConfigKeyString.ToString();
                var PulledObject = JObject.FromObject(WatchdogJsonConfigFiles.ApplicationConfig[ConfigSection]);
                WatchdogJsonConfigFiles.ConfigLogger?.WriteLog($"PULLED CONFIG OBJECT FOR VALUE: {ConfigKeyString} OK!", LogType.TraceLog);
                return PulledObject;
            }
            catch (Exception PullEx)
            {
                // Catch failure, log it, and return null
                WatchdogJsonConfigFiles.ConfigLogger?.WriteLog($"FAILED TO PULL CONFIG FOR SECTION {ConfigKeyString}!", LogType.TraceLog);
                WatchdogJsonConfigFiles.ConfigLogger?.WriteLog("EXCEPTION THROWN DURING PULL!", PullEx, new[] { LogType.TraceLog });
                return null;
            }
        }
        /// <summary>
        /// Builds the path for an output json config file.
        /// </summary>
        /// <param name="JObjectKey">Type of json file to pull</param>
        /// <returns>Path to new json file</returns>
        public static string GetJObjectConfigFile(JConfigType JObjectKey)
        {
            // Check for full config.
            string ConfigKeyString = JObjectKey.ToString();
            WatchdogJsonConfigFiles.ConfigLogger?.WriteLog($"PULLING CONFIG VALUE FILE PATH FOR TYPE {ConfigKeyString}", LogType.TraceLog);
            string OutputPath = Path.Combine(Directory.GetCurrentDirectory(), "JsonConfiguration", JObjectKey.ToString() + ".json");
            WatchdogJsonConfigFiles.ConfigLogger?.WriteLog($"GENERATED JSON CONFIG PATH: {OutputPath}", LogType.TraceLog);

            // Check if real.
            if (!File.Exists(OutputPath)) WatchdogJsonConfigFiles.ConfigLogger?.WriteLog("DESIRED JSON CONFIG FILE DOES NOT EXIST!", LogType.WarnLog);
            return File.Exists(OutputPath) ? OutputPath : "";
        }
    }
}
