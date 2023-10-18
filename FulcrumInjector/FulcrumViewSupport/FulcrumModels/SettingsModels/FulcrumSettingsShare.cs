using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using FulcrumInjector.FulcrumViewSupport.FulcrumDataConverters;
using FulcrumInjector.FulcrumViewSupport.FulcrumJsonSupport;
using FulcrumJson;
using FulcrumSupport;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using SharpLogging;
using SettingSectionTypes = FulcrumInjector.FulcrumViewSupport.FulcrumModels.SettingsModels.FulcrumSettingsCollection.SettingSectionTypes;

namespace FulcrumInjector.FulcrumViewSupport.FulcrumModels.SettingsModels
{
    /// <summary>
    /// Static share class object for settings entries.
    /// </summary>
    public sealed class FulcrumSettingsShare : Dictionary<SettingSectionTypes, FulcrumSettingsCollection>
    {
        #region Custom Events
        #endregion //Custom Events

        #region Fields

        // Logger instance for our settings share
        private readonly SharpLogger _settingsStoreLogger;

        // Singleton configuration for our settings share 
        private static FulcrumSettingsShare _settingsShareInstance;
        private static readonly object _settingsShareLock = new object();

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
        public FulcrumSettingsCollection InjectorShimDllSettings => this[SettingSectionTypes.SHIM_DLL_SETTINGS];
        public FulcrumSettingsCollection InjectorEncryptionSettings => this[SettingSectionTypes.ENCRYPTION_SETTINGS];
        public FulcrumSettingsCollection InjectorRegexFulcrumSettings => this[SettingSectionTypes.DLL_OUTPUT_REGEX_SETTINGS];
        public FulcrumSettingsCollection DebugLogViewerFulcrumSettings => this[SettingSectionTypes.DEBUG_LOG_VIEWER_SETTINGS];
        public FulcrumSettingsCollection InjectorPipeConfigFulcrumSettings => this[SettingSectionTypes.INJECTOR_PIPE_SETTINGS];
        public FulcrumSettingsCollection InjectorHardwareSettings => this[SettingSectionTypes.HARDWARE_CONFIGURATION_SETTINGS];
        public FulcrumSettingsCollection InjectorLogConversionSettings => this[SettingSectionTypes.LOG_FILE_CONVERSION_SETTINGS];
        public FulcrumSettingsCollection InjectorDllSyntaxFulcrumSettings => this[SettingSectionTypes.DLL_OUTPUT_SYNTAX_SETTINGS];
        public FulcrumSettingsCollection InjectorDebugSyntaxFulcrumSettings => this[SettingSectionTypes.DEBUG_LOG_VIEWER_SYNTAX_SETTINGS];

        #endregion //Properties

        #region Structs and Classes
        #endregion //Structs and Classes

        // ------------------------------------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// Private CTOR for the settings generation routines. Used to load settings objects when needed
        /// </summary>
        private FulcrumSettingsShare()
        {
            // Setup our local dictionary for all settings objects
            this.Add(SettingSectionTypes.SHIM_DLL_SETTINGS, new FulcrumSettingsCollection(
                SettingSectionTypes.SHIM_DLL_SETTINGS,
                Array.Empty<FulcrumSettingEntryModel>()));
            this.Add(SettingSectionTypes.INJECTOR_PIPE_SETTINGS, new FulcrumSettingsCollection(
                SettingSectionTypes.INJECTOR_PIPE_SETTINGS,
                Array.Empty<FulcrumSettingEntryModel>()));
            this.Add(SettingSectionTypes.HARDWARE_CONFIGURATION_SETTINGS, new FulcrumSettingsCollection(
                SettingSectionTypes.HARDWARE_CONFIGURATION_SETTINGS,
                Array.Empty<FulcrumSettingEntryModel>()));
            this.Add(SettingSectionTypes.LOG_FILE_CONVERSION_SETTINGS, new FulcrumSettingsCollection(
                SettingSectionTypes.LOG_FILE_CONVERSION_SETTINGS,
                Array.Empty<FulcrumSettingEntryModel>()));
            this.Add(SettingSectionTypes.ENCRYPTION_SETTINGS, new FulcrumSettingsCollection(
                SettingSectionTypes.ENCRYPTION_SETTINGS,
                Array.Empty<FulcrumSettingEntryModel>()));
            this.Add(SettingSectionTypes.DLL_OUTPUT_REGEX_SETTINGS, new FulcrumSettingsCollection(
                SettingSectionTypes.DLL_OUTPUT_REGEX_SETTINGS,
                Array.Empty<FulcrumSettingEntryModel>()));
            this.Add(SettingSectionTypes.DLL_OUTPUT_SYNTAX_SETTINGS, new FulcrumSettingsCollection(
                SettingSectionTypes.DLL_OUTPUT_SYNTAX_SETTINGS,
                Array.Empty<FulcrumSettingEntryModel>()));
            this.Add(SettingSectionTypes.DEBUG_LOG_VIEWER_SETTINGS, new FulcrumSettingsCollection(
                SettingSectionTypes.DEBUG_LOG_VIEWER_SETTINGS,
                Array.Empty<FulcrumSettingEntryModel>()));
            this.Add(SettingSectionTypes.DEBUG_LOG_VIEWER_SYNTAX_SETTINGS, new FulcrumSettingsCollection(
                SettingSectionTypes.DEBUG_LOG_VIEWER_SYNTAX_SETTINGS,
                Array.Empty<FulcrumSettingEntryModel>()));

            // Configure our new logger instance and the backing collection of settings, then import all needed values
            this._settingsStoreLogger = new SharpLogger(LoggerActions.UniversalLogger);
            this.ReloadSettings();
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
        public IEnumerable<FulcrumSettingsCollection> ReloadSettings()
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
                .Select(JsonObject => new Tuple<SettingSectionTypes, FulcrumSettingEntryModel[]>(
                    JsonObject["SettingSectionTitle"].Value<string>().ToEnumValue<SettingSectionTypes>(),
                    JsonConvert.DeserializeObject<FulcrumSettingEntryModel[]>(JsonObject["SettingsEntries"].ToString())))
                .ToArray();

            // Store the section type on each setting instance found
            foreach (var LoadedSettingSet in SettingsLoaded)
            {
                // Store the section type for the parent object on each setting if needed
                var SectionType = LoadedSettingSet.Item1;
                foreach (var SettingEntry in LoadedSettingSet.Item2) 
                    if (SettingEntry.SettingSection == SettingSectionTypes.NO_SECTION_TYPE)
                        SettingEntry.SettingSection = SectionType;
            }

            // Log out how many setting values were loaded in this routine
            this._settingsStoreLogger.WriteLog($"PULLED IN {SettingsLoaded.Length} SETTINGS SEGMENTS OK!", LogType.InfoLog);
            this._settingsStoreLogger.WriteLog("SETTINGS ARE BEING LOGGED OUT TO THE DEBUG LOG FILE NOW...", LogType.InfoLog);

            // Loop all the setting objects loaded in from our configuration file and store them
            foreach (var SettingSet in SettingsLoaded)
            {
                // Add the newly loaded setting object into our collection instance and log it's been built 
                this[SettingSet.Item1] = new FulcrumSettingsCollection(SettingSet.Item1, SettingSet.Item2);
                this._settingsStoreLogger.WriteLog($"[SETTINGS COLLECTION] ::: {SettingSet.Item1.ToDescriptionString()} HAS BEEN IMPORTED");
            }

            // Save our settings to the JSON file to update our regex values loaded in 
            this._settingsStoreLogger.WriteLog("SAVING IMPORTED SETTINGS FOR CONSISTENCY ACROSS SHARE AND JSON FILE...");
            this.SaveSettings();

            // Log passed and return output
            this._settingsStoreLogger.WriteLog("IMPORTED SETTINGS OBJECTS CORRECTLY! READY TO GENERATE UI COMPONENTS FOR THEM NOW...");
            return this.Values.ToList();
        }
        /// <summary>
        /// Saves the current share instance of settings onto our app settings json file.
        /// This routine also includes logic for formatting the written JSON so it's still sanitized
        /// </summary>
        /// <returns>True if the settings file is saved, false if it is not</returns>
        public bool SaveSettings()
        {
            // Pull all of our settings objects and build our JSON output for them here
            this._settingsStoreLogger.WriteLog("SAVING ALL USER SETTINGS TO APPLICATION JSON SETTINGS FILE NOW...", LogType.WarnLog);
            var SettingObjects = this.Select(SettingObj => JObject.FromObject(new
            {
                // Configure a title and entry value for each setting object
                SettingSectionTitle = SettingObj.Key.ToDescriptionString(),
                SettingsEntries = SettingObj.Value.ToList()
            })).ToList();

            // Write the new values to our JSON configuration file
            bool SetValues = ValueSetters.SetValue("FulcrumUserSettings", SettingObjects);
            if (SetValues) this._settingsStoreLogger.WriteLog("WROTE OUT NEW SETTINGS VALUES FOR INJECTOR APP CORRECTLY!", LogType.InfoLog);
            else
            {
                // Log out that we failed to set our new values for the settings file and exit out
                this._settingsStoreLogger.WriteLog("ERROR! FAILED TO SET SETTINGS VALUES FOR THE INJECTOR APPLICATION!", LogType.ErrorLog);
                return false;
            }

            try
            {
                // Find the settings set for the shim values here and store them
                var ShimSettingsSet = this[SettingSectionTypes.SHIM_DLL_SETTINGS];
                string[] ValuesPulled = ShimSettingsSet
                    .Select(SettingObj => SettingObj.ToString())
                    .Prepend("FulcrumShimDLLConfig.txt")
                    .ToArray();

                // Write final output values here.
                string ConfigFilePath = Path.GetDirectoryName(JsonConfigFile.AppConfigFile);
                ConfigFilePath = Path.Combine(ConfigFilePath, "FulcrumShimDLLConfig.txt");
                File.WriteAllText(ConfigFilePath, string.Join("|", ValuesPulled));
                this._settingsStoreLogger.WriteLog("UPDATED SHIM SETTINGS CONFIGURATION FILE CORRECTLY!", LogType.InfoLog);

                // Return true once all settings are updated
                return true;
            }
            catch (Exception UpdateShimSettingsEx)
            {
                // If we failed to update the shim settings, exit out failed
                this._settingsStoreLogger.WriteLog("ERROR! FAILED TO SAVE SHIM DLL CONFIGURATION!", LogType.ErrorLog);
                this._settingsStoreLogger.WriteException("EXCEPTION THROWN DURING SAVE ROUTINE: ", UpdateShimSettingsEx);

                // Return out false if we failed to set these values 
                return false; 
            }
        }
    }
}
