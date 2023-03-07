using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using FulcrumInjector.FulcrumViewSupport.FulcrumJsonSupport;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using SharpLogging;

namespace FulcrumInjector.FulcrumViewContent.FulcrumModels.SettingsModels
{
    /// <summary>
    /// Static share class object for settings entries.
    /// </summary>
    internal sealed class FulcrumSettingsShare : IEnumerable<FulcrumSettingsCollection>
    {
        #region Custom Events
        #endregion //Custom Events

        #region Fields

        // Singleton configuration for our settings share 
        private static FulcrumSettingsShare _settingsShareInstance;
        private static readonly object _settingsShareLock = new object();

        // Logger instance for our settings share
        private readonly SharpLogger _settingsStoreLogger;
        private readonly List<FulcrumSettingsCollection> _settingsCollections;

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
        public FulcrumSettingsCollection InjectorGeneralFulcrumSettings =>
            this.FirstOrDefault(SettingObj =>
                SettingObj.SettingSectionTitle.Contains("Hardware Configuration Settings")
            ) ?? new FulcrumSettingsCollection("Hardware Configuration Settings", Array.Empty<FulcrumSettingEntryModel>());
        public FulcrumSettingsCollection DebugLogViewerFulcrumSettings =>
            this?.FirstOrDefault(SettingObj =>
                SettingObj.SettingSectionTitle.Contains("Debug Log Viewer Settings")
            ) ?? new FulcrumSettingsCollection("Debug Log Viewer Settings", Array.Empty<FulcrumSettingEntryModel>());
        public FulcrumSettingsCollection InjectorPipeConfigFulcrumSettings =>
            this?.FirstOrDefault(SettingObj =>
                SettingObj.SettingSectionTitle.Contains("Injector Pipe Settings")
            ) ?? new FulcrumSettingsCollection("Injector Pipe Settings", Array.Empty<FulcrumSettingEntryModel>()); 
        public FulcrumSettingsCollection InjectorRegexFulcrumSettings =>
            this?.FirstOrDefault(SettingObj =>
                SettingObj.SettingSectionTitle.Contains("PassThru DLL Output Regex Settings")
            ) ?? new FulcrumSettingsCollection("PassThru Regex Settings", Array.Empty<FulcrumSettingEntryModel>());
        public FulcrumSettingsCollection InjectorDllSyntaxFulcrumSettings =>
            this?.FirstOrDefault(SettingObj =>
                SettingObj.SettingSectionTitle.Contains("PassThru DLL Output Syntax Settings")
            ) ?? new FulcrumSettingsCollection("PassThru Syntax Settings", Array.Empty<FulcrumSettingEntryModel>()); 
        public FulcrumSettingsCollection InjectorDebugSyntaxFulcrumSettings =>
            this.FirstOrDefault(SettingObj =>
                SettingObj.SettingSectionTitle.Contains("Debug Log Viewer Syntax Settings")
            ) ?? new FulcrumSettingsCollection("Debug Log Viewer Syntax Settings", Array.Empty<FulcrumSettingEntryModel>());

        #endregion //Properties

        #region Structs and Classes
        #endregion //Structs and Classes

        // ------------------------------------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// Returns our generic enumerator for this collection
        /// </summary>
        /// <returns>An IEnumerator collection of objects holding our settings</returns>
        IEnumerator IEnumerable.GetEnumerator()
        {
            // Return the enumerator object
            return GetEnumerator();
        }
        /// <summary>
        /// Returns our enumerator using the cast collection of settings on this class instance
        /// </summary>
        /// <returns></returns>
        public IEnumerator<FulcrumSettingsCollection> GetEnumerator()
        {
            // Return our settings share object ordered by their names as a collection
            return this._settingsCollections.GetEnumerator();
        }

        // ------------------------------------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// Private CTOR for the settings generation routines. Used to load settings objects when needed
        /// </summary>
        private FulcrumSettingsShare()
        {
            // Configure our new logger instance and the backing collection of settings
            this._settingsCollections = new List<FulcrumSettingsCollection>();
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
                .Select(JsonObject => new Tuple<string, FulcrumSettingEntryModel[]>(
                    JsonObject["SettingSectionTitle"].Value<string>(),
                    JsonObject["SettingsEntries"].ToObject<FulcrumSettingEntryModel[]>()))
                .Select(SettingCollection => new FulcrumSettingsCollection(SettingCollection.Item1, SettingCollection.Item2))
                .ToArray();

            // Log out how many setting values were loaded in this routine
            this._settingsStoreLogger.WriteLog($"PULLED IN {SettingsLoaded.Length} SETTINGS SEGMENTS OK!", LogType.InfoLog);
            this._settingsStoreLogger.WriteLog("SETTINGS ARE BEING LOGGED OUT TO THE DEBUG LOG FILE NOW...", LogType.InfoLog);

            // Loop all the setting objects loaded in from our configuration file and store them
            foreach (var SettingSet in SettingsLoaded)
            {
                // Add the newly loaded setting object into our collection instance and log it's been built 
                this._settingsCollections.Add(SettingSet);
                this._settingsStoreLogger.WriteLog($"[SETTINGS COLLECTION] ::: {SettingSet.SettingSectionTitle} HAS BEEN IMPORTED");
            }

            // Log passed and return output
            this._settingsStoreLogger.WriteLog("IMPORTED SETTINGS OBJECTS CORRECTLY! READY TO GENERATE UI COMPONENTS FOR THEM NOW...");
            return this;
        }
    }
}
