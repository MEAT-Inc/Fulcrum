using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Windows.Controls;
using FulcrumInjector.FulcrumViewContent.FulcrumModels.SettingsModels;
using FulcrumInjector.FulcrumViewSupport.FulcrumDataConverters;
using FulcrumInjector.FulcrumViewSupport.FulcrumJsonSupport;
using ICSharpCode.AvalonEdit;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using SharpLogging;

namespace FulcrumInjector.FulcrumViewContent.FulcrumViewModels.InjectorOptionViewModels
{
    /// <summary>
    /// ViewModel for settings pane binding values
    /// </summary>
    internal class FulcrumSettingsPaneViewModel : FulcrumViewModelBase
    {
        #region Custom Events
        #endregion // Custom Events

        #region Fields

        // Private backing fields for our public properties
        private ObservableCollection<FulcrumSettingsCollection> _settingsEntrySets;

        #endregion // Fields

        #region Properties

        // Public properties for the view to bind onto  
        public ObservableCollection<FulcrumSettingsCollection> SettingsEntrySets
        {
            get => _settingsEntrySets;
            set => PropertyUpdated(value);
        }

        #endregion // Properties

        #region Structs and Classes
        #endregion // Structs and Classes

        // ------------------------------------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// Builds a new VM and generates a new logger object for it.
        /// </summary>
        /// <param name="SettingsViewUserControl">UserControl which holds the content for our settings view</param>
        public FulcrumSettingsPaneViewModel(UserControl SettingsViewUserControl) : base(SettingsViewUserControl)
        {
            // Spawn a new logger for this view model instance and log some information
            this.ViewModelLogger = new SharpLogger(LoggerActions.UniversalLogger);
            this.ViewModelLogger.WriteLog($"VIEWMODEL LOGGER FOR VM {this.GetType().Name} HAS BEEN STARTED OK!", LogType.InfoLog);

            // Store the settings sets for our instance on the view model
            this.SettingsEntrySets = new(FulcrumConstants.FulcrumSettings.Values.ToList());
            this.ViewModelLogger.WriteLog("IMPORTED AND STORED NEW SETTINGS CONFIGURATION VALUES OK!", LogType.InfoLog);
            this.ViewModelLogger.WriteLog($"VIEW MODEL TYPE {this.GetType().Name} HAS BEEN CONSTRUCTED CORRECTLY!", LogType.InfoLog);
        }

        // ------------------------------------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// Converts the current AppSettings file into a json string and shows it in the editor
        /// </summary>
        /// <param name="EditorDocument"></param>
        public void PopulateAppSettingJsonViewer(TextEditor EditorDocument)
        {
            // Log information and populate values
            this.ViewModelLogger.WriteLog("POPULATING JSON ON THE EDITOR CONTENT NOW...");
            EditorDocument.Text = JObject.Parse(File.ReadAllText(JsonConfigFile.AppConfigFile)).ToString(Formatting.Indented);
            this.ViewModelLogger.WriteLog("STORED NEW JSON CONTENT OK!", LogType.InfoLog);
        }
        /// <summary>
        /// Converts the current AppSettings file into a json string and shows it in the editor
        /// </summary>
        /// <param name="EditorDocument"></param>
        public void SaveAppSettingJsonAsConfig(TextEditor EditorDocument)
        {
            // Log information and populate values
            this.ViewModelLogger.WriteLog("SAVING JSON ON THE EDITOR INTO OUR APP CONFIG FILE NOW..."); 
            File.WriteAllText(JsonConfigFile.AppConfigFile, EditorDocument.Text);
            this.ViewModelLogger.WriteLog("WROTE NEW JSON CONTENT OK! PULLING IN CONTENTS TO REFRESH NOW...", LogType.InfoLog);
            
            // Reload the settings into our view model now 
            this.PopulateAppSettingJsonViewer(EditorDocument);
            this.SettingsEntrySets = new(FulcrumConstants.FulcrumSettings.GenerateSettingsModels());
        }

        /// <summary>
        /// Saves a new setting object value onto the view model and settings share instance
        /// </summary>
        /// <param name="SenderContext"></param>
        public void SaveSettingValue(FulcrumSettingEntryModel SenderContext)
        {
            // Store the setting value back onto our view model content and save it's JSON Value.
            this.ViewModelLogger.WriteLog($"SETTING VALUE BEING WRITTEN OUT: {JsonConvert.SerializeObject(SenderContext, Formatting.None)}", LogType.TraceLog);
            var LocatedSettingSet = FulcrumConstants.FulcrumSettings
                .FirstOrDefault(SettingSet => SettingSet.Value
                    .Any(SettingObj => SettingObj.SettingName == SenderContext.SettingName)).Value;

            // Find the location of the setting value.
            if (LocatedSettingSet == null) { this.ViewModelLogger.WriteLog("FAILED TO FIND SETTING ENTRY SET WITH SETTING VALUE!", LogType.ErrorLog); return; }
            LocatedSettingSet.UpdateSetting(new[] { SenderContext });

            // Now write the new setting value to our JSON configuration and refresh values.
            int SettingSetIndex = FulcrumConstants.FulcrumSettings.ToList()
                .FindIndex(ImportedSettingSet => ImportedSettingSet.Key == LocatedSettingSet.SectionType);

            // Store the settings value here.
            var SettingObjects = FulcrumConstants.FulcrumSettings.Select(SettingObj => JObject.FromObject(new {
                SettingSectionTitle = SettingObj.Key.ToDescriptionString(),
                SettingsEntries = SettingObj.Value.ToList()
            })).ToList();

            // Build the new setting value and store it on the list of settings
            SettingObjects[SettingSetIndex] = JObject.FromObject(new
            {
                LocatedSettingSet.SettingSectionTitle,
                SettingsEntries = LocatedSettingSet.ToList()
            });

            // Store our value in the JSON configuration files now.
            ValueSetters.SetValue("FulcrumUserSettings", SettingObjects);
            this.ViewModelLogger.WriteLog("STORED NEW VALUE SETTINGS CORRECTLY! JSON CONFIGURATION WAS UPDATED ACCORDINGLY!", LogType.InfoLog);
            this.SettingsEntrySets = new(FulcrumConstants.FulcrumSettings.GenerateSettingsModels());

            // If we've got a special setting value, then store it here.
            if (LocatedSettingSet.SectionType != FulcrumSettingsCollection.SettingSectionTypes.SHIM_DLL_SETTINGS) return;
            this.ViewModelLogger.WriteLog("STORING SETTINGS FOR SHIM CONFIGURATION IN A TEMP TEXT FILE NOW...", LogType.WarnLog);
            string ConfigFilePath = Path.GetDirectoryName(JsonConfigFile.AppConfigFile);
            ConfigFilePath = Path.Combine(ConfigFilePath, "FulcrumShimDLLConfig.txt");

            // Store the value of the settings and their names in here.
            string[] ValuesPulled = LocatedSettingSet
                .Select(SettingObj => SettingObj.SettingValue.ToString())
                .Prepend("FulcrumShimDLLConfig.txt")
                .ToArray();

            // Write final output values here.
            File.WriteAllText(ConfigFilePath, string.Join("|", ValuesPulled));
            this.ViewModelLogger.WriteLog("UPDATED SHIM SETTINGS CONFIGURATION FILE CORRECTLY!", LogType.InfoLog);
        }

        /// <summary>
        /// Saves all settings for the injector application back into the settings file
        /// This method should only be called when all settings are force rewritten to our settings file
        /// </summary>
        public void SaveAllSettings()
        {
            // Log that we're saving all settings values and build a new setting model for the JSON file
            this.ViewModelLogger.WriteLog("SAVING ALL USER SETTINGS TO APPLICATION JSON SETTINGS FILE NOW...", LogType.WarnLog);
            var SettingObjects = FulcrumConstants.FulcrumSettings.Select(SettingObj => JObject.FromObject(new
            {
                SettingSectionTitle = SettingObj.Key.ToDescriptionString(),
                SettingsEntries = SettingObj.Value.ToList()
            })).ToList();

            // Write the new values to our JSON configuration file
            ValueSetters.SetValue("FulcrumUserSettings", SettingObjects);
            this.SettingsEntrySets = new(FulcrumConstants.FulcrumSettings.GenerateSettingsModels());
            this.ViewModelLogger.WriteLog("UPDATED FULCRUM SETTINGS MODELS ON THE GLOBAL SHARE CORRECTLY!", LogType.InfoLog);

            // Find the settings set for the shim values here and store them
            var ShimSettingsSet = FulcrumConstants.FulcrumSettings[FulcrumSettingsCollection.SettingSectionTypes.SHIM_DLL_SETTINGS];
            string[] ValuesPulled = ShimSettingsSet
                .Select(SettingObj => SettingObj.ToString())
                .Prepend("FulcrumShimDLLConfig.txt")
                .ToArray();

            // Write final output values here.
            string ConfigFilePath = Path.GetDirectoryName(JsonConfigFile.AppConfigFile);
            ConfigFilePath = Path.Combine(ConfigFilePath, "FulcrumShimDLLConfig.txt");
            File.WriteAllText(ConfigFilePath, string.Join("|", ValuesPulled));
            this.ViewModelLogger.WriteLog("UPDATED SHIM SETTINGS CONFIGURATION FILE CORRECTLY!", LogType.InfoLog);
        }
    }
}
