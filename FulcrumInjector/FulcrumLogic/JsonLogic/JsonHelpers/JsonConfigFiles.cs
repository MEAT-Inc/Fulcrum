using System.IO;
using System.Linq;
using System.Reflection;
using Microsoft.Win32;
using Newtonsoft.Json.Linq;
using SharpLogger;
using SharpLogger.LoggerObjects;
using SharpLogger.LoggerSupport;

namespace FulcrumInjector.FulcrumLogic.JsonLogic.JsonHelpers
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
                var CurrentLogger = LoggerQueue.SpawnLogger("JsonConfigLogger", LoggerActions.SubServiceLogger);

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
            // Pull location of the configuration application. If debugging is on, then try and set it using the working dir. 
            string FulcrumInjectorExe;
            ConfigLogger?.WriteLog($"PULLING IN NEW APP CONFIG FILE NAMED {NewConfigFileName} FROM PROGRAM FILES OR WORKING DIRECTORY NOW");
#if DEBUG
            ConfigLogger?.WriteLog("DEBUG BUILD FOUND! USING DEBUG CONFIGURATION FILE FROM CURRENT WORKING DIR", LogType.InfoLog);
            FulcrumInjectorExe = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
#else
            string FulcrumInjectorDir;
            var FulcrumKey = Registry.LocalMachine.OpenSubKey("SOFTWARE\\PassThruSupport.04.04\\MEAT Inc - FulcrumShim (v04.04)");
            if (FulcrumKey != null) { FulcrumInjectorExe = Path.GetDirectoryName(FulcrumKey.GetValue("ConfigApplication").ToString()); } 
            else 
            {
                ConfigLogger?.WriteLog("INJECTOR REGISTRY KEY WAS NULL! FALLING BACK NOW...", LogType.WarnLog);
                FulcrumInjectorExe = @"C:\Program Files (x86)\MEAT Inc\FulcrumShim\FulcrumInjector";
            }
#endif
            // List all the files in the directory we've located now and then find our settings file by name
            Directory.SetCurrentDirectory(FulcrumInjectorExe);
            ConfigLogger?.WriteLog($"INJECTOR DIR PULLED: {FulcrumInjectorExe}", LogType.InfoLog);
            string[] LocatedFilesInDirectory = Directory.GetFiles(FulcrumInjectorExe, "*.json", SearchOption.AllDirectories);
            ConfigLogger?.WriteLog($"LOCATED A TOTAL OF {LocatedFilesInDirectory.Length} FILES IN OUR APP FOLDER WITH A JSON EXTENSION");
            string MatchedConfigFile = LocatedFilesInDirectory
                .OrderBy(FileObj => FileObj.Length)
                .FirstOrDefault(FileObj => FileObj.Contains(NewConfigFileName));

            // Check if the file is null or not found first
            if (MatchedConfigFile == null) throw new FileNotFoundException($"FAILED TO FIND OUR JSON CONFIG FILE!\nFILE: {NewConfigFileName}");
            ConfigLogger?.WriteLog($"LOCATED CONFIG FILE NAME IS: {MatchedConfigFile}", LogType.InfoLog);

            // Log info. Set file state
            AppConfigFile = Path.GetFullPath(MatchedConfigFile);
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
                if (!FirstConfig) return _applicationConfig;
                ConfigLogger?.WriteLog($"BUILDING NEW JCONFIG OBJECT NOW...", LogType.TraceLog);
                _applicationConfig = JObject.Parse(File.ReadAllText(AppConfigFile));
                ConfigLogger?.WriteLog($"GENERATED JSON CONFIG FILE OBJECT OK AND WROTE CONTENT TO {AppConfigFile} OK! RETURNED CONTENTS NOW...", LogType.TraceLog);

                // Return the object.
                return _applicationConfig;
            }
        }
    }
}
