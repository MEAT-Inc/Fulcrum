using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using FulcrumInjector.FulcrumLogic.JsonHelpers;
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
            this.SettingsEntrySets = FulcrumSettingsShare.GenerateSettingsModels();
            ViewModelLogger.WriteLog("GENERATED NEW SETTINGS FOR VIEW MODEL CORRECTLY!", LogType.InfoLog);

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
    }
}
