using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FulcrumInjector.FulcrumLogic.JsonHelpers;
using SharpLogger;
using SharpLogger.LoggerObjects;
using SharpLogger.LoggerSupport;

namespace FulcrumInjector.FulcrumViewContent.Models.ModelShares
{
    /// <summary>
    /// Static share class object for settings entries.
    /// </summary>
    public static class FulcrumSettingsShare
    {
        // All Setting entries
        private static ObservableCollection<SettingsEntryCollectionModel> _settingsEntrySets;
        public static ObservableCollection<SettingsEntryCollectionModel> SettingsEntrySets
        {
            get
            {
                // If our temp private value is empty, fill it and then run.
                _settingsEntrySets ??= GenerateSettingsModels();
                return _settingsEntrySets;
            }
        }

        // Settings for Debug log viewing (Or an empty settings model if null)
        public static SettingsEntryCollectionModel DebugLogViewerSettings =>
            SettingsEntrySets?.FirstOrDefault(SettingObj =>
                SettingObj.SettingSectionTitle.Contains("Debug Log Viewer Settings")
            ) ?? new SettingsEntryCollectionModel("Debug Log Viewer Settings", Array.Empty<SettingsEntryModel>());

        // Settings for pipe configuration (Or an empty settings model if null)
        public static SettingsEntryCollectionModel InjectorPipeConfigSettings =>
            SettingsEntrySets?.FirstOrDefault(SettingObj =>
                SettingObj.SettingSectionTitle.Contains("Injector Pipe Settings")
            ) ?? new SettingsEntryCollectionModel("Injector Pipe Settings", Array.Empty<SettingsEntryModel>());

        // ---------------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// Builds a list of settings model objects to use from our input json objects
        /// </summary>
        /// <returns>Settings entries built output</returns>
        public static ObservableCollection<SettingsEntryCollectionModel> GenerateSettingsModels()
        {
            // Get Logger instance 
            var SettingStoreLogger = (SubServiceLogger)LogBroker.LoggerQueue.GetLoggers(LoggerActions.SubServiceLogger)
                .FirstOrDefault(LoggerObj => LoggerObj.LoggerName.StartsWith("SettingsModelStoreLogger")) ?? new SubServiceLogger("SettingsModelStoreLogger");

            // Pull our settings objects out from the settings file.
            var SettingsLoaded = ValueLoaders.GetConfigValue<SettingsEntryCollectionModel[]>("FulcrumUserSettings");
            SettingStoreLogger.WriteLog($"PULLED IN {SettingsLoaded.Length} SETTINGS SEGMENTS OK!", LogType.InfoLog);
            SettingStoreLogger.WriteLog("SETTINGS ARE BEING LOGGED OUT TO THE DEBUG LOG FILE NOW...", LogType.InfoLog);
            foreach (var SettingSet in SettingsLoaded) SettingStoreLogger.WriteLog($"[SETTINGS COLLECTION] ::: {SettingSet}");

            // Log passed and return output
            SettingStoreLogger.WriteLog("IMPORTED SETTINGS OBJECTS CORRECTLY! READY TO GENERATE UI COMPONENTS FOR THEM NOW...");
            _settingsEntrySets = new ObservableCollection<SettingsEntryCollectionModel>(SettingsLoaded);
            return _settingsEntrySets;
        }
    }
}
