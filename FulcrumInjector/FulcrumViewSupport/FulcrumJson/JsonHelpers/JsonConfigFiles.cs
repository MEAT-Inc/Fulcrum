using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using Microsoft.Win32;
using Newtonsoft.Json.Linq;
using SharpLogging;

namespace FulcrumInjector.FulcrumViewSupport.FulcrumJson.JsonHelpers
{
    /// <summary>
    /// Class which contains info about the possible json files to import.
    /// </summary>
    public static class JsonConfigFiles
    {
        #region Custom Events
        #endregion //Custom Events

        #region Fields

        // Private field used to hold our configuration object
        private static JObject _applicationConfig;

        #endregion //Fields

        #region Properties
        
        // Logger instance for our JSON configuration helpers
        private static SharpLogger _jsonConfigLogger => SharpLogBroker.LogBrokerInitialized
            ? new SharpLogger(LoggerActions.UniversalLogger)
            : null;

        // Currently loaded app configuration file and the JSON object built from that file
        public static string AppConfigFile { get; private set; }
        public static JObject ApplicationConfig
        {
            get
            {
                // Return existing object if needed here.
                bool FirstConfig = _applicationConfig == null;

                // Build new here for the desired input file object.
                if (!FirstConfig) return _applicationConfig;
                _jsonConfigLogger?.WriteLog($"BUILDING NEW JCONFIG OBJECT NOW...", LogType.TraceLog);
                _applicationConfig = JObject.Parse(File.ReadAllText(AppConfigFile));
                _jsonConfigLogger?.WriteLog($"GENERATED JSON CONFIG FILE OBJECT OK AND WROTE CONTENT TO {AppConfigFile} OK! RETURNED CONTENTS NOW...", LogType.TraceLog);

                // Return the object.
                return _applicationConfig;
            }
        }

        #endregion //Properties

        #region Structs and Classes
        #endregion //Structs and Classes

        // ------------------------------------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// Loads a new config file, sets the access bool to true if the file exists
        /// </summary>
        /// <param name="NewConfigFileName">Name of our configuration file to use</param>
        /// <param name="ForcedDirectory">The forced path to look in for our configuration file</param>
        public static void SetInjectorConfigFile(string NewConfigFileName, string ForcedDirectory = null)
        {
            // Pull location of the configuration application. If debugging is on, then try and set it using the working dir. 
            string FulcrumInjectorExe = ForcedDirectory ?? string.Empty;
            _jsonConfigLogger?.WriteLog($"PULLING IN NEW APP CONFIG FILE NAMED {NewConfigFileName} FROM PROGRAM FILES OR WORKING DIRECTORY NOW");
#if DEBUG
            _jsonConfigLogger?.WriteLog("DEBUG BUILD FOUND! USING DEBUG CONFIGURATION FILE FROM CURRENT WORKING DIR", LogType.InfoLog);
            FulcrumInjectorExe = ForcedDirectory ?? Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
#else
            string FulcrumInjectorDir;
            var FulcrumKey = Registry.LocalMachine.OpenSubKey("SOFTWARE\\PassThruSupport.04.04\\MEAT Inc - FulcrumShim (v04.04)");
            if (FulcrumKey != null) { FulcrumInjectorExe = Path.GetDirectoryName(FulcrumKey.GetValue("ConfigApplication").ToString()); } 
            else 
            {
                _jsonConfigLogger?.WriteLog("INJECTOR REGISTRY KEY WAS NULL! FALLING BACK NOW...", LogType.WarnLog);
                FulcrumInjectorExe = @"C:\Program Files (x86)\MEAT Inc\FulcrumShim\FulcrumInjector";
            }
#endif
            // List all the files in the directory we've located now and then find our settings file by name
            Directory.SetCurrentDirectory(FulcrumInjectorExe);
            _jsonConfigLogger?.WriteLog($"INJECTOR DIR PULLED: {FulcrumInjectorExe}", LogType.InfoLog);
            string[] LocatedFilesInDirectory = Directory.GetFiles(FulcrumInjectorExe, "*.json", SearchOption.AllDirectories);
            _jsonConfigLogger?.WriteLog($"LOCATED A TOTAL OF {LocatedFilesInDirectory.Length} FILES IN OUR APP FOLDER WITH A JSON EXTENSION");
            string MatchedConfigFile = LocatedFilesInDirectory
                .OrderBy(FileObj => FileObj.Length)
                .FirstOrDefault(FileObj => FileObj.Contains(NewConfigFileName));

            // Check if the file is null or not found first
            if (MatchedConfigFile == null) throw new FileNotFoundException($"FAILED TO FIND OUR JSON CONFIG FILE!\nFILE: {NewConfigFileName}");
            _jsonConfigLogger?.WriteLog($"LOCATED CONFIG FILE NAME IS: {MatchedConfigFile}", LogType.InfoLog);

            // Log info. Set file state
            AppConfigFile = Path.GetFullPath(MatchedConfigFile);
            _jsonConfigLogger?.WriteLog("STORING NEW JSON FILE NOW!", LogType.InfoLog);
            _jsonConfigLogger?.WriteLog($"EXPECTED TO LOAD JSON CONFIG FILE AT: {AppConfigFile}");

            // Check existing
            if (File.Exists(AppConfigFile)) _jsonConfigLogger?.WriteLog("CONFIG FILE LOADED OK!", LogType.InfoLog);
            else throw new FileNotFoundException($"FAILED TO FIND OUR JSON CONFIG FILE!\nFILE: {AppConfigFile}");
        }
    }
}
