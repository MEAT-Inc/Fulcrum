using System;
using System.Collections.Generic;
using System.Linq;
using FulcrumInjector.FulcrumViewSupport.FulcrumJsonSupport;
using SharpLogging;

namespace FulcrumInjector.FulcrumViewContent.FulcrumModels.SettingsModels
{
    /// <summary>
    /// Static share class object for settings entries.
    /// </summary>
    internal class FulcrumSettingsShare : List<FulcrumSettingsCollectionModel>
    {
        #region Custom Events
        #endregion //Custom Events

        #region Fields

        // Logger instance for our settings share
        private SharpLogger _settingsStoreLogger;

        #endregion //Fields

        #region Properties

        // Predefined settings collections for object values pulled in from our JSON Configuration file
        public FulcrumSettingsCollectionModel InjectorGeneralFulcrumSettings =>
            this.FirstOrDefault(SettingObj =>
                SettingObj.SettingSectionTitle.Contains("Hardware Configuration Settings")
            ) ?? new FulcrumSettingsCollectionModel("Hardware Configuration Settings", Array.Empty<FulcrumSettingsEntryModel>());
        public FulcrumSettingsCollectionModel DebugLogViewerFulcrumSettings =>
            this?.FirstOrDefault(SettingObj =>
                SettingObj.SettingSectionTitle.Contains("Debug Log Viewer Settings")
            ) ?? new FulcrumSettingsCollectionModel("Debug Log Viewer Settings", Array.Empty<FulcrumSettingsEntryModel>());
        public FulcrumSettingsCollectionModel InjectorPipeConfigFulcrumSettings =>
            this?.FirstOrDefault(SettingObj =>
                SettingObj.SettingSectionTitle.Contains("Injector Pipe Settings")
            ) ?? new FulcrumSettingsCollectionModel("Injector Pipe Settings", Array.Empty<FulcrumSettingsEntryModel>()); 
        public FulcrumSettingsCollectionModel InjectorRegexFulcrumSettings =>
            this?.FirstOrDefault(SettingObj =>
                SettingObj.SettingSectionTitle.Contains("PassThru DLL Output Regex Settings")
            ) ?? new FulcrumSettingsCollectionModel("PassThru Regex Settings", Array.Empty<FulcrumSettingsEntryModel>());
        public FulcrumSettingsCollectionModel InjectorDllSyntaxFulcrumSettings =>
            this?.FirstOrDefault(SettingObj =>
                SettingObj.SettingSectionTitle.Contains("PassThru DLL Output Syntax Settings")
            ) ?? new FulcrumSettingsCollectionModel("PassThru Syntax Settings", Array.Empty<FulcrumSettingsEntryModel>()); 
        public FulcrumSettingsCollectionModel InjectorDebugSyntaxFulcrumSettings =>
            this.FirstOrDefault(SettingObj =>
                SettingObj.SettingSectionTitle.Contains("Debug Log Viewer Syntax Settings")
            ) ?? new FulcrumSettingsCollectionModel("Debug Log Viewer Syntax Settings", Array.Empty<FulcrumSettingsEntryModel>());

        #endregion //Properties

        #region Structs and Classes
        #endregion //Structs and Classes

        // ------------------------------------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// Builds a list of settings model objects to use from our input json objects
        /// </summary>
        /// <returns>Settings entries built output</returns>
        public IEnumerable<FulcrumSettingsCollectionModel> GenerateSettingsModels()
        {
            // Configure our new logger instance
            this._settingsStoreLogger ??= new SharpLogger(LoggerActions.UniversalLogger);

            // Pull our settings objects out from the settings file.
            var SettingsLoaded = ValueLoaders.GetConfigValue<FulcrumSettingsCollectionModel[]>("FulcrumUserSettings");
            this._settingsStoreLogger.WriteLog($"PULLED IN {SettingsLoaded.Length} SETTINGS SEGMENTS OK!", LogType.InfoLog);
            this._settingsStoreLogger.WriteLog("SETTINGS ARE BEING LOGGED OUT TO THE DEBUG LOG FILE NOW...", LogType.InfoLog);

            // Loop all the setting objects loaded in from our configuration file and store them
            foreach (var SettingSet in SettingsLoaded)
            {
                // Add the newly loaded setting object into our collection instance and log it's been built 
                this.Add(SettingSet);
                this._settingsStoreLogger.WriteLog($"[SETTINGS COLLECTION] ::: {SettingSet.SettingSectionTitle} HAS BEEN IMPORTED");
            }

            // Log passed and return output
            this._settingsStoreLogger.WriteLog("IMPORTED SETTINGS OBJECTS CORRECTLY! READY TO GENERATE UI COMPONENTS FOR THEM NOW...");
            return this;
        }
    }
}
