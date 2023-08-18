using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using FulcrumInjector.FulcrumViewSupport.FulcrumJsonSupport;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using SharpLogging;

// Static using for settings collection types
using SectionType = FulcrumInjector.FulcrumViewContent.FulcrumModels.SettingsModels.FulcrumSettingsCollection.SettingSectionTypes;

namespace FulcrumInjector.FulcrumViewContent.FulcrumModels.SettingsModels
{
    /// <summary>
    /// Static share class object for settings entries.
    /// </summary>
    internal sealed class FulcrumSettingsShare : Dictionary<SectionType, FulcrumSettingsCollection>
    {
        #region Custom Events
        #endregion //Custom Events

        #region Fields

        // Singleton configuration for our settings share 
        private static FulcrumSettingsShare _settingsShareInstance;
        private static readonly object _settingsShareLock = new object();

        // Logger instance for our settings share
        private readonly SharpLogger _settingsStoreLogger;

        #endregion //Fields

        #region Properties

        // Singleton instance of our settings share object
        public static FulcrumSettingsShare ShareInstance
        {
            get
            {
                // Lock our share object to avoid threading issues
                lock (_settingsShareLock)
                {
                    // Spawn a new settings share if needed and return it out
                    _settingsShareInstance ??= new FulcrumSettingsShare();
                    return _settingsShareInstance;
                }
            }
        }

        // Predefined settings collections for object values pulled in from our JSON Configuration file
        public FulcrumSettingsCollection InjectorShimDllSettings =>
            this[SectionType.SHIM_DLL_SETTINGS] ??= new FulcrumSettingsCollection("FulcrumShim DLL Settings", SectionType.SHIM_DLL_SETTINGS);
        public FulcrumSettingsCollection InjectorHardwareSettings =>
            this[SectionType.HARDWARE_SETTINGS] ??= new FulcrumSettingsCollection("Hardware Configuration Settings", SectionType.HARDWARE_SETTINGS);
        public FulcrumSettingsCollection InjectorPipeServerSettings =>
            this[SectionType.PIPE_SERVER_SETTINGS] ??= new FulcrumSettingsCollection("Injector Pipe Settings", SectionType.PIPE_SERVER_SETTINGS);
        public FulcrumSettingsCollection InjectorWatchdogSettings =>
            this[SectionType.FILE_WATCHDOG_SETTINGS] ??= new FulcrumSettingsCollection("Fulcrum Watchdog Settings", SectionType.FILE_WATCHDOG_SETTINGS);
        public FulcrumSettingsCollection InjectorDllOutputRegexSettings =>
            this[SectionType.DLL_OUTPUT_REGEX_SETTINGS] ??= new FulcrumSettingsCollection("PassThru DLL Output Regex Settings", SectionType.DLL_OUTPUT_REGEX_SETTINGS);
        public FulcrumSettingsCollection InjectorDllSyntaxSettings =>
            this[SectionType.DLL_OUTPUT_SYNTAX_SETTINGS] ??= new FulcrumSettingsCollection("PassThru DLL Output Syntax Settings", SectionType.DLL_OUTPUT_SYNTAX_SETTINGS);
        public FulcrumSettingsCollection InjectorDebugLogViewerSettings =>
            this[SectionType.DEBUG_VIEWER_SETTINGS] ??= new FulcrumSettingsCollection("Debug Log Viewer Settings", SectionType.DEBUG_VIEWER_SETTINGS);
        public FulcrumSettingsCollection InjectorDebugSyntaxSettings =>
            this[SectionType.DEBUG_VIEWER_SYNTAX] ??= new FulcrumSettingsCollection("Debug Log Viewer Syntax Settings", SectionType.DEBUG_VIEWER_SYNTAX);

        #endregion //Properties

        #region Structs and Classes
        #endregion //Structs and Classes

        // ------------------------------------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// Private CTOR for the settings generation routines. Used to load settings objects when needed
        /// </summary>
        private FulcrumSettingsShare()
        {
            // Configure our new logger instance and the backing collection of settings
            this._settingsStoreLogger = new SharpLogger(LoggerActions.UniversalLogger);

            // Finally import all of our settings values and exit out
            this.GenerateSettingsModels();
        }
        /// <summary>
        /// Static construction routine used to pull in a new fulcrum settings share instance if needed
        /// </summary>
        /// <returns>A spawned or existing instance of our fulcrum settings share</returns>
        public static FulcrumSettingsShare GenerateSettingsShare()
        {
            // Lock our share object to avoid threading issues
            lock (_settingsShareLock)
            {
                // Spawn a new settings share if needed and return it out
                _settingsShareInstance ??= new FulcrumSettingsShare();
                return _settingsShareInstance;
            }
        }

        // ------------------------------------------------------------------------------------------------------------------------------------------
  
        /// <summary>
        /// Builds a list of settings model objects to use from our input json objects
        /// </summary>
        /// <returns>Settings entries built output</returns>
        public IEnumerable<FulcrumSettingsCollection> GenerateSettingsModels()
        {
            // If our application configuration file is not defined, then return an empty collection
            if (string.IsNullOrWhiteSpace(JsonConfigFile.AppConfigFile))
            {
                // Log there's no settings file to load and exit out
                this._settingsStoreLogger.WriteLog("ERROR! NO SETTINGS FILE WAS LOADED! UNABLE TO LOAD SETTINGS VALUES!", LogType.ErrorLog);
                return new List<FulcrumSettingsCollection>();
            }

            // Pull our settings objects out from the settings file.
            var SettingsLoaded = ValueLoaders.GetConfigValue<JObject[]>("FulcrumUserSettings")
                .Select(JsonObject => new Tuple<string, SectionType, FulcrumSettingEntryModel[]>(
                    JsonObject["SettingSectionTitle"].Value<string>(), 
                    (SectionType)Enum.Parse(typeof(SectionType), JsonObject["SettingSectionType"].Value<string>()),
                    JsonObject["SettingsEntries"].ToObject<FulcrumSettingEntryModel[]>()))
                .Select(SettingCollection => new FulcrumSettingsCollection(
                    SettingCollection.Item1, 
                    SettingCollection.Item2,
                    SettingCollection.Item3))
                .ToArray();

            // Log out how many setting values were loaded in this routine
            this._settingsStoreLogger.WriteLog($"PULLED IN {SettingsLoaded.Length} SETTINGS SEGMENTS OK!", LogType.InfoLog);
            this._settingsStoreLogger.WriteLog("SETTINGS ARE BEING LOGGED OUT TO THE DEBUG LOG FILE NOW...", LogType.InfoLog);

            // Clear out old settings values and then load the new ones in 
            this.Clear();
            this._settingsStoreLogger.WriteLog("CLEARED OUT OLD SETTINGS STORE VALUES OK! STORING NEW VALUES NOW...");

            // Loop all the setting objects loaded in from our configuration file and store them
            foreach (var SettingSet in SettingsLoaded)
            {
                // Add the newly loaded setting object into our collection instance and log it's been built 
                this.Add(SettingSet.SettingSectionType, SettingSet);
                this._settingsStoreLogger.WriteLog($"[SETTINGS COLLECTION] ::: {SettingSet.SettingSectionTitle} HAS BEEN IMPORTED");
            }

            // Log passed and return output
            this._settingsStoreLogger.WriteLog("IMPORTED SETTINGS OBJECTS CORRECTLY! READY TO GENERATE UI COMPONENTS FOR THEM NOW...");
            return SettingsLoaded;
        }
    }
}
