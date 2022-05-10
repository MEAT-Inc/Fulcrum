using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using FulcrumInjector.FulcrumLogic.JsonLogic.JsonHelpers;
using FulcrumInjector.FulcrumViewContent.Models;
using FulcrumInjector.FulcrumViewContent.Models.SettingsModels;
using ICSharpCode.AvalonEdit;
using Newtonsoft.Json;
using SharpLogger;
using SharpLogger.LoggerObjects;
using SharpLogger.LoggerSupport;

namespace FulcrumInjector.FulcrumViewContent.ViewModels.InjectorOptionViewModels
{
    /// <summary>
    /// ViewModel for settings pane binding values
    /// </summary>
    public class FulcrumSettingsPaneViewModel : ViewModelControlBase
    {
        // Logger object.
        private static SubServiceLogger ViewModelLogger => (SubServiceLogger)LogBroker.LoggerQueue.GetLoggers(LoggerActions.SubServiceLogger)
            .FirstOrDefault(LoggerObj => LoggerObj.LoggerName.StartsWith("SettingsViewModelLogger")) ?? new SubServiceLogger("SettingsViewModelLogger");

        // Private control values
        private ObservableCollection<SettingsEntryCollectionModel> _settingsEntrySets;

        // Public values for our view to bind onto 
        public ObservableCollection<SettingsEntryCollectionModel> SettingsEntrySets
        {
            get => _settingsEntrySets;
            set => PropertyUpdated(value);
        }

        // --------------------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// Builds a new VM and generates a new logger object for it.
        /// </summary>
        public FulcrumSettingsPaneViewModel()
        {
            // Log information and store values 
            ViewModelLogger.WriteLog($"VIEWMODEL LOGGER FOR VM {this.GetType().Name} HAS BEEN STARTED OK!", LogType.InfoLog);
            ViewModelLogger.WriteLog("SETTING UP DEBUG LOG TARGETS FOR UI LOGGING NOW...", LogType.WarnLog);

            // Pull settings values in on startup
            this.SettingsEntrySets = FulcrumSettingsShare.SettingsEntrySets ?? FulcrumSettingsShare.GenerateSettingsModels();
            ViewModelLogger.WriteLog("GENERATED NEW SETTINGS FOR VIEW MODEL CORRECTLY! SETTINGS IMPORTED TO OUR VIEW CONTENT FROM SHARE!", LogType.InfoLog);

            // Log completed setup.
            // Store this instance onto our injector constants
            ViewModelLogger.WriteLog("SETUP NEW SETTINGS CONFIGURATION VALUES OK!", LogType.InfoLog);
            ViewModelLogger.WriteLog($"STORED NEW VIEW MODEL OBJECT FOR TYPE {this.GetType().Name} TO INJECTOR CONSTANTS OK!", LogType.InfoLog);
        }

        // --------------------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// Converts the current AppSettings file into a json string and shows it in the editor
        /// </summary>
        /// <param name="EditorDocument"></param>
        internal void PopulateAppSettingJsonViewer(TextEditor EditorDocument)
        {
            // Log information and populate values
            ViewModelLogger.WriteLog("POPULATING JSON ON THE EDITOR CONTENT NOW...");
            EditorDocument.Text = JsonConfigFiles.ApplicationConfig.ToString(Formatting.Indented);
            ViewModelLogger.WriteLog("STORED NEW JSON CONTENT OK!", LogType.InfoLog);
        }
        /// <summary>
        /// Converts the current AppSettings file into a json string and shows it in the editor
        /// </summary>
        /// <param name="EditorDocument"></param>
        internal void SaveAppSettingJsonAsConfig(TextEditor EditorDocument)
        {
            // Log information and populate values
            ViewModelLogger.WriteLog("SAVING JSON ON THE EDITOR INTO OUR APP CONFIG FILE NOW..."); 
            File.WriteAllText(JsonConfigFiles.AppConfigFile, EditorDocument.Text);
            ViewModelLogger.WriteLog("WROTE NEW JSON CONTENT OK! PULLING IN CONTENTS TO REFRESH NOW...", LogType.InfoLog);

            // Refresh content view now.
            this.PopulateAppSettingJsonViewer(EditorDocument);
        }

        /// <summary>
        /// Saves a new setting object value onto the view model and settings share instance
        /// </summary>
        /// <param name="SenderContext"></param>
        internal void SaveSettingValue(SettingsEntryModel SenderContext)
        {
            // Store the setting value back onto our view model content and save it's JSON Value.
            ViewModelLogger.WriteLog($"SETTING VALUE BEING WRITTEN OUT: {JsonConvert.SerializeObject(SenderContext, Formatting.None)}", LogType.TraceLog);
            var LocatedSettingSet = this.SettingsEntrySets
                .FirstOrDefault(SettingSet => SettingSet.SettingsEntries
                    .Any(SettingObj => SettingObj.SettingName == SenderContext.SettingName));

            // Find the location of the setting value.
            if (LocatedSettingSet == null) { ViewModelLogger.WriteLog("FAILED TO FIND SETTING ENTRY SET WITH SETTING VALUE!", LogType.ErrorLog); return; }
            LocatedSettingSet.UpdateSetting(new[] { SenderContext });

            // Now write the new setting value to our JSON configuration and refresh values.
            int SettingSetIndex = this.SettingsEntrySets
                .ToList()
                .FindIndex(ImportedSettingSet => ImportedSettingSet.SettingSectionTitle == LocatedSettingSet.SettingSectionTitle);

            // Store the settings value here.
            var SettingObjects = FulcrumSettingsShare.SettingsEntrySets;
            SettingObjects[SettingSetIndex] = LocatedSettingSet;

            // Store our value in the JSON configuration files now.
            ValueSetters.SetValue("FulcrumUserSettings", SettingObjects);
            FulcrumSettingsShare.GenerateSettingsModels(); this.SettingsEntrySets = FulcrumSettingsShare.SettingsEntrySets;
            ViewModelLogger.WriteLog("STORED NEW VALUE SETTINGS CORRECTLY! JSON CONFIGURATION WAS UPDATED ACCORDINGLY!", LogType.InfoLog);

            // If we've got a special setting value, then store it here.
            if (LocatedSettingSet.SettingSectionTitle != "FulcrumShim DLL Settings") return;
            ViewModelLogger.WriteLog("STORING SETTINGS FOR SHIM CONFIGURATION IN A TEMP TEXT FILE NOW...", LogType.WarnLog);
            string ConfigFilePath = Path.GetDirectoryName(JsonConfigFiles.AppConfigFile);
            ConfigFilePath = Path.Combine(ConfigFilePath, "FulcrumShimDLLConfig.txt");

            // Store the value of the settings and their names in here.
            string[] ValuesPulled = LocatedSettingSet.SettingsEntries
                .Select(SettingObj => SettingObj.SettingValue.ToString())
                .Prepend("FulcrumShimDLLConfig.txt")
                .ToArray();

            // Write final output values here.
            File.WriteAllText(ConfigFilePath, string.Join("|", ValuesPulled));
            ViewModelLogger.WriteLog($"WROTE OUT VALUES FOR SETTINGS TITLED {string.Join(" -- ", ValuesPulled)}");
        }
    }
}
