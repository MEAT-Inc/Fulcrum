using System;
using System.Collections.Generic;
using System.IO;
using System.Windows.Documents;
using FulcrumInjector.FulcrumViewSupport.FulcrumDataConverters;
using Newtonsoft.Json.Linq;
using SharpLogging;

namespace FulcrumInjector.FulcrumViewSupport.FulcrumJsonSupport
{
    /// <summary>
    /// Contains methods for loading config values.
    /// </summary>
    internal static class ValueLoaders
    {
        #region Custom Events
        #endregion //Custom Events

        #region Fields

        // Backing logger object used to avoid configuration issues
        private static SharpLogger _backingLogger;
        private static List<string> _encryptedFields;

        #endregion //Fields

        #region Properties

        // Logging object used to write information out from this class
        private static SharpLogger _valueLoadersLogger => SharpLogBroker.LogBrokerInitialized
            ? _backingLogger ??= new SharpLogger(LoggerActions.UniversalLogger)
            : null;

        #endregion //Properties

        #region Structs and Classes
        #endregion //Structs and Classes

        // ------------------------------------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// Pulls a JSON Object value from the given query path and converts it to an object.
        /// </summary>
        /// <typeparam name="TValueType"></typeparam>
        /// <param name="JsonPath"></param>
        /// <returns></returns>
        public static TValueType GetConfigValue<TValueType>(string JsonPath)
        {
            // See if our config file is missing.
            if (!File.Exists(JsonConfigFile.AppConfigFile))
                throw new InvalidOperationException("CAN NOT PULL CONFIG VALUES SINCE THE CONFIG FILE IS NOT YET BUILT!");

            // Get the token first.
            _valueLoadersLogger?.WriteLog($"TRYING TO PULL VALUE AT: {JsonPath}", LogType.TraceLog);
            var ValueObject = JsonConfigFile.ApplicationConfig.SelectToken(JsonPath);
            if (ValueObject == null)
            {
                // If our output object is null, then just return a generic output of the type passed
                _valueLoadersLogger?.WriteLog($"ERROR! VALUE PULLED AT PATH GIVEN WAS NULL!", LogType.TraceLog);
                return Activator.CreateInstance<TValueType>();
            }

            // If not null, convert and return the object into the desired generic type
            TValueType ConvertedValue = ValueObject.ToObject<TValueType>();
            _valueLoadersLogger?.WriteLog($"PROPERTY: {JsonPath} WAS READ AND STORED AS TYPE {typeof(TValueType).Name} CORRECTLY!", LogType.TraceLog);

            // Check if the path given is an encrypted field or not. If it's not, then just return the value out
            if (!JsonConfigFile.EncryptedConfigKeys.Contains(JsonPath) || typeof(TValueType) != typeof(string))
                return ConvertedValue;

            // If we've got an encrypted field value and a string value, convert/decrypt it here 
            _valueLoadersLogger?.WriteLog("DECRYPTING VALUE FOR FIELD NOW...", LogType.TraceLog);
            string DecryptedValue = StringEncryptor.Decrypt(ConvertedValue.ToString());
            if (DecryptedValue is TValueType CastDecryption)
            {
                // Return the decrypted field value
                _valueLoadersLogger?.WriteLog($"DECRYPTION PASSED FOR FIELD {JsonPath}!", LogType.TraceLog);
                return CastDecryption;
            }

            // Return the built converted value here
            _valueLoadersLogger?.WriteLog($"DECRYPTION FAILED TO EXECUTE FOR FIELD {JsonPath}!", LogType.TraceLog);
            return ConvertedValue;
        }
        /// <summary>
        /// Tries to get a JObject from our master config file type
        /// </summary>
        /// <param name="JObjectKey">Base Type of a json key</param>
        /// <returns>A JObject built from our requested Key value</returns>
        public static JObject GetJObjectConfig(string JObjectKey)
        {
            // See if our config file is missing.
            if (!File.Exists(JsonConfigFile.AppConfigFile))
                throw new InvalidOperationException("CAN NOT PULL CONFIG VALUES SINCE THE CONFIG FILE IS NOT YET BUILT!");

            // Check for full config.
            _valueLoadersLogger?.WriteLog($"PULLING CONFIG VALUE FOR TYPE {JObjectKey}", LogType.TraceLog);
            try
            {
                // Try and get the current object. If failed, return null
                var PulledJObject = JsonConfigFile.ApplicationConfig[JObjectKey];
                _valueLoadersLogger?.WriteLog($"PULLED CONFIG OBJECT FOR VALUE: {JObjectKey} OK!", LogType.TraceLog);

                // Cast and return the requested object as an array if needed
                if (PulledJObject.Type != JTokenType.Array) return JObject.FromObject(PulledJObject);
                JObject OutputObject = new JObject { { JObjectKey, JArray.FromObject(PulledJObject) } };
                return OutputObject;
            }
            catch (Exception PullEx)
            {
                // Catch failure, log it, and return null
                _valueLoadersLogger?.WriteLog($"FAILED TO PULL CONFIG FOR SECTION {JObjectKey}!", LogType.TraceLog);
                _valueLoadersLogger?.WriteException("EXCEPTION THROWN DURING PULL!", PullEx, LogType.TraceLog);
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
            _valueLoadersLogger?.WriteLog($"PULLING CONFIG VALUE FILE PATH FOR TYPE {ConfigKeyString}", LogType.TraceLog);
            string OutputPath = Path.Combine(Directory.GetCurrentDirectory(), "JsonConfiguration", JObjectKey.ToString() + ".json");
            _valueLoadersLogger?.WriteLog($"GENERATED JSON CONFIG PATH: {OutputPath}", LogType.TraceLog);

            // Check if real.
            if (!File.Exists(OutputPath)) _valueLoadersLogger?.WriteLog("DESIRED JSON CONFIG FILE DOES NOT EXIST!", LogType.WarnLog);
            return File.Exists(OutputPath) ? OutputPath : "";
        }
    }
}
