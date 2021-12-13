using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using Microsoft.Win32;
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
        public static string AppConfigFile;

        /// <summary>
        /// Loads a new config file, sets the access bool to true if the file exists
        /// </summary>
        /// <param name="NewConfigFileName"></param>
        public static void SetNewAppConfigFile(string NewConfigFileName)
        {
            // Pull location of the configuration application
            var FulcrumKey = Registry.LocalMachine.OpenSubKey("SOFTWARE\\PassThruSupport.04.04\\MEAT Inc - FulcrumShim");
            var PathToInjector = Path.GetDirectoryName(FulcrumKey.GetValue("ConfigApplication").ToString());

            // Log info. Set file state
            NewConfigFileName = Path.GetFileName(NewConfigFileName);
            AppConfigFile = Debugger.IsAttached ?
                NewConfigFileName : 
                Path.Combine(PathToInjector, NewConfigFileName);

            // Log info about the file object
            ConfigLogger?.WriteLog("STORING NEW JSON FILE NOW!", LogType.InfoLog);
            ConfigLogger?.WriteLog($"EXPECTED TO LOAD JSON CONFIG FILE AT: {AppConfigFile}");

            // Check existing
            if (File.Exists(AppConfigFile)) ConfigLogger?.WriteLog("CONFIG FILE LOADED OK!", LogType.InfoLog);
            else throw new FileNotFoundException($"FAILED TO FIND OUR JSON CONFIG FILE!\nFILE: {AppConfigFile}");
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
                if (FirstConfig)
                {
                    ConfigLogger?.WriteLog($"BUILDING NEW JCONFIG OBJECT NOW...", LogType.TraceLog);
                    _applicationConfig = JObject.Parse(File.ReadAllText(AppConfigFile));
                    ConfigLogger?.WriteLog($"GENERATED JSON CONFIG FILE OBJECT OK AND WROTE CONTENT TO {AppConfigFile} OK! RETURNED CONTENTS NOW...", LogType.TraceLog);
                }

                // Return the object.
                return _applicationConfig;
            }
        }
    }
}
