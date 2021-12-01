using System.ComponentModel;
using System.IO;
using System.Linq;
using FulcrumInjector.FulcrumLogging;
using FulcrumInjector.FulcrumLogging.LoggerObjects;
using FulcrumInjector.FulcrumLogging.LoggerSupport;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace FulcrumInjector.FulcrumJsonHelpers
{
    /// <summary>
    /// All possible config types pulled from here.
    /// </summary>
    public enum JConfigType
    {
        // Possible config sections.
        [Description("FulcrumLogging")]     FulcrumLogging,   // Logging config
        [Description("FulcrumConsole")]     FulcrumConsole,   // Console config
    }

    /// <summary>
    /// Class which contains info about the possible json files to import.
    /// </summary>
    public static class WatchdogJsonConfigFiles
    {
        // Logger object for these methods.
        internal static SubServiceLogger ConfigLogger
        {
            get
            {
                // If the main output value is null, return anyway.
                if (FulcrumLogBroker.MainLogFileName == null) { return null; }
                var CurrentLogger = FulcrumLogBroker.LoggerQueue.GetLoggers(LoggerActions.SubServiceLogger)
                    .FirstOrDefault(LogObj => LogObj.LoggerName.StartsWith("JsonConfigLogger"));

                // Check logger
                if (CurrentLogger != null) return (SubServiceLogger)CurrentLogger;

                // Add new logger if all the found ones are null
                var NewLogger = new SubServiceLogger("JsonConfigLogger", FulcrumLogBroker.MainLogFileName);
                return NewLogger;
            }
        }

        // ---------------------------------- Input Located JSON Files ----------------------------------------

        // List of all the files found in the directory of this application
        // TODO: MAKE THIS LESS STATIC! RIGHT NOW THIS DEPENDS ON HARD CODED INSTALL LOCATIONS!
        public static string AppConfigFile = "C:\\Program Files (x86)\\MEAT Inc\\FulcrumShim\\FulcrumInjector\\FulcrumInjectorConfig.json";

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
