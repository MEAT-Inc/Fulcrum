using System;
using System.IO;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using SharpLogger.LoggerSupport;
using ErrorEventArgs = Newtonsoft.Json.Serialization.ErrorEventArgs;

namespace FulcrumInjector.FulcrumLogic.JsonLogic.JsonHelpers
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
            // See if our config file is missing.
            if (!File.Exists(JsonConfigFiles.AppConfigFile))
                throw new InvalidOperationException("CAN NOT PULL CONFIG VALUES SINCE THE CONFIG FILE IS NOT YET BUILT!");

            // Get the token first.
            JsonConfigFiles.ConfigLogger?.WriteLog($"TRYING TO PULL VALUE AT: {JsonPath}", LogType.TraceLog);
            var ValueObject = JsonConfigFiles.ApplicationConfig.SelectToken(JsonPath);
            if (ValueObject == null)
            {
                // If our output object is null, then just return a generic output of the type passed
                JsonConfigFiles.ConfigLogger?.WriteLog($"ERROR! VALUE PULLED AT PATH GIVEN WAS NULL!", LogType.TraceLog);
                return (TValueType)new object();
            }

            // If not null, convert and return.
            var ConvertedValue = ValueObject.ToObject<TValueType>();
            JsonConfigFiles.ConfigLogger?.WriteLog($"PROPERTY: {JsonPath} | VALUE: {JsonConvert.SerializeObject(ConvertedValue, Formatting.None)}", LogType.TraceLog);
            return ConvertedValue;
        }

        /// <summary>
        /// Tries to get a jobject from our master config file type
        /// </summary>
        /// <param name="JObjectKey">Base Type of a json key</param>
        /// <returns></returns>
        public static JObject GetJObjectConfig(string JObjectKey)
        {
            // See if our config file is missing.
            if (!File.Exists(JsonConfigFiles.AppConfigFile))
                throw new InvalidOperationException("CAN NOT PULL CONFIG VALUES SINCE THE CONFIG FILE IS NOT YET BUILT!");

            // Check for full config.
            JsonConfigFiles.ConfigLogger?.WriteLog($"PULLING CONFIG VALUE FOR TYPE {JObjectKey}", LogType.TraceLog);
            try
            {
                // Try and get the current object. If failed, return null
                var PulledJObject = JsonConfigFiles.ApplicationConfig[JObjectKey];
                JsonConfigFiles.ConfigLogger?.WriteLog($"PULLED CONFIG OBJECT FOR VALUE: {JObjectKey} OK!", LogType.TraceLog);

                // Cast and return if needed
                if (PulledJObject.Type != JTokenType.Array) return JObject.FromObject(PulledJObject);
                {
                    // Build new object
                    JObject OutputObject = new JObject();
                    OutputObject.Add(JObjectKey, JArray.FromObject(PulledJObject));
                    return OutputObject;
                }
            }
            catch (Exception PullEx)
            {
                // Catch failure, log it, and return null
                JsonConfigFiles.ConfigLogger?.WriteLog($"FAILED TO PULL CONFIG FOR SECTION {JObjectKey}!", LogType.TraceLog);
                JsonConfigFiles.ConfigLogger?.WriteLog("EXCEPTION THROWN DURING PULL!", PullEx, new[] { LogType.TraceLog });
                return null;
            }
        }
        /// <summary>
        /// Builds the path for an output json config file.
        /// </summary>
        /// <param name="JObjectKey">Type of json file to pull</param>
        /// <returns>Path to new json file</returns>
        public static string GetJObjectConfigFile(string JObjectKey)
        {
            // Check for full config.
            string ConfigKeyString = JObjectKey.ToString();
            JsonConfigFiles.ConfigLogger?.WriteLog($"PULLING CONFIG VALUE FILE PATH FOR TYPE {ConfigKeyString}", LogType.TraceLog);
            string OutputPath = Path.Combine(Directory.GetCurrentDirectory(), "JsonConfiguration", JObjectKey.ToString() + ".json");
            JsonConfigFiles.ConfigLogger?.WriteLog($"GENERATED JSON CONFIG PATH: {OutputPath}", LogType.TraceLog);

            // Check if real.
            if (!File.Exists(OutputPath)) JsonConfigFiles.ConfigLogger?.WriteLog("DESIRED JSON CONFIG FILE DOES NOT EXIST!", LogType.WarnLog);
            return File.Exists(OutputPath) ? OutputPath : "";
        }
    }
}
