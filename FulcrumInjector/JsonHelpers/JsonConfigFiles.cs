using System.IO;
using System.Linq;
using System.Reflection;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using SharpLogger;
using SharpLogger.LoggerObjects;
using SharpLogger.LoggerSupport;

namespace FulcrumInjector.JsonHelpers
{
    /// <summary>
    /// Class which contains info about the possible json files to import.
    /// </summary>
    public static class JsonConfigFiles
    {
        // Logger object for these methods.
        internal static SubServiceLogger ConfigLogger
        {
            get
            {
                // If the main output value is null, return anyway.
                if (LogBroker.MainLogFileName == null) { return null; }
                var CurrentLogger = LogBroker.LoggerQueue.GetLoggers(LoggerActions.SubServiceLogger)
                    .FirstOrDefault(LogObj => LogObj.LoggerName.StartsWith("JsonConfigLogger"));

                // Check logger
                if (CurrentLogger != null) return (SubServiceLogger)CurrentLogger;

                // Add new logger if all the found ones are null
                var NewLogger = new SubServiceLogger("JsonConfigLogger", LogBroker.MainLogFileName);
                return NewLogger;
            }
        }

        // ---------------------------------- Input Located JSON Files ----------------------------------------

        // List of all the files found in the directory of this application
        private static string _appConfigFile;
        public static string AppConfigFile
        {
            // Set value based on config entry or by the previously defined value.
            get => $"{_appConfigFile ??= Assembly.GetExecutingAssembly().GetName().Name}Settings.json"; 
            private set => _appConfigFile = value;
        }

        /// <summary>
        /// Loads a new config file, sets the access bool to true if the file exists
        /// </summary>
        /// <param name="NewConfigFile"></param>
        public static void SetNewAppConfigFile(string NewConfigFile)
        {
            // Log info. Set file state
            AppConfigFile = NewConfigFile;
            ConfigLogger?.WriteLog("STORING NEW JSON FILE NOW!", LogType.InfoLog);
            ConfigLogger?.WriteLog($"JSON CONFIG FILE LOADED: {NewConfigFile}", LogType.TraceLog);

            // Check existing
            bool ConfigFileReady = File.Exists(AppConfigFile);
            if (ConfigFileReady) ConfigLogger?.WriteLog("CONFIG FILE LOADED OK!", LogType.InfoLog);
            else { ConfigLogger?.WriteLog("CONFIG FILE COULD NOT BE FOUND!", LogType.ErrorLog); }
        }

        // ------------------------------------ Combined Output JSON ------------------------------------------

        // Desired combined output JSON File
        private static JObject _applicationConfig;
        public static JObject ApplicationConfig
        {
            get
            {
                // Return existing object if needed here.
                bool FirstConfig = _applicationConfig == null;

                // Build new here for the desired input file object.
                _applicationConfig = new JObject();
                if (FirstConfig) ConfigLogger?.WriteLog($"BUILDING NEW JCONFIG OBJECT NOW...", LogType.TraceLog);
                _applicationConfig = JObject.Parse(File.ReadAllText(AppConfigFile));

                // Write content.
                File.WriteAllText(AppConfigFile, JsonConvert.SerializeObject(_applicationConfig, Formatting.Indented));
                ConfigLogger?.WriteLog($"GENERATED JSON CONFIG FILE OBJECT OK AND WROTE CONTENT TO {AppConfigFile} OK! RETURNED CONTENTS NOW...", LogType.TraceLog);

                // Return the object.
                return _applicationConfig;
            }
        }
    }
}
