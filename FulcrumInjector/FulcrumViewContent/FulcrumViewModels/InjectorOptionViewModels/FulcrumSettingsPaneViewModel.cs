using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Windows.Controls;
using System.Windows.Documents;
using FulcrumInjector.FulcrumViewSupport;
using FulcrumInjector.FulcrumViewSupport.FulcrumDataConverters;
using FulcrumInjector.FulcrumViewSupport.FulcrumJsonSupport;
using FulcrumInjector.FulcrumViewSupport.FulcrumModels.SettingsModels;
using FulcrumJson;
using ICSharpCode.AvalonEdit;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using SharpLogging;

namespace FulcrumInjector.FulcrumViewContent.FulcrumViewModels.InjectorOptionViewModels
{
    /// <summary>
    /// ViewModel for settings pane binding values
    /// </summary>
    public class FulcrumSettingsPaneViewModel : FulcrumViewModelBase
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
            this.SettingsEntrySets = new(FulcrumConstants.FulcrumSettings.ReloadSettings());
        }

        /// <summary>
        /// Saves all settings for the injector application back into the settings file
        /// This method should only be called when all settings are force rewritten to our settings file
        /// </summary>
        public void SaveAllSettings()
        {
            // Pull the setting share object and write the values of it out to our JSON file
            if (!FulcrumConstants.FulcrumSettings.SaveSettings())
            {
                // If the save routine failed, just exit out and don't reload settings
                this.ViewModelLogger.WriteLog("ERROR! FAILED TO SAVE SETTINGS FOR THE INJECTOR APP!", LogType.ErrorLog);
                return;
            }

            // Build a new set of settings objects from our share instance
            this.ViewModelLogger.WriteLog("RELOADING SETTINGS FROM JSON SETTINGS FILE NOW...", LogType.WarnLog);
            this.SettingsEntrySets = new(FulcrumConstants.FulcrumSettings.ReloadSettings());
            this.ViewModelLogger.WriteLog("RELOADED SETTINGS FOR INJECTOR APPLICATION CORRECTLY!", LogType.InfoLog);
        }
        /// <summary>
        /// Saves a new setting object value onto the view model and settings share instance
        /// </summary>
        /// <param name="SenderContext"></param>
        public void SaveSettingValue(FulcrumSettingEntryModel SenderContext)
        {
            // Store the setting value back onto our view model content and save it's JSON Value.
            this.ViewModelLogger.WriteLog($"SETTING VALUE BEING WRITTEN OUT: {JsonConvert.SerializeObject(SenderContext, Formatting.None)}", LogType.TraceLog);
            if (!FulcrumConstants.FulcrumSettings.ContainsKey(SenderContext.SettingSection))
            {
                // If we failed to find our setting set, log this failure and exit out
                this.ViewModelLogger.WriteLog("FAILED TO FIND SETTING ENTRY SET WITH SETTING VALUE!", LogType.ErrorLog);
                return;
            }
        
            // Now write the new setting value to our JSON configuration and refresh values.
            var LocatedSettingSet = FulcrumConstants.FulcrumSettings[SenderContext.SettingSection];
            LocatedSettingSet.UpdateSetting(SenderContext);

            // Store the settings values updated here and exit out
            if (!FulcrumConstants.FulcrumSettings.SaveSettings())
            {
                // If the save routine failed, just exit out and don't reload settings
                this.ViewModelLogger.WriteLog("ERROR! FAILED TO SAVE SETTINGS FOR THE INJECTOR APP!", LogType.ErrorLog);
                return;
            }

            // Build a new set of settings objects from our share instance
            this.ViewModelLogger.WriteLog("RELOADING SETTINGS FROM JSON SETTINGS FILE NOW...", LogType.WarnLog);
            this.SettingsEntrySets = new(FulcrumConstants.FulcrumSettings.ReloadSettings());
            this.ViewModelLogger.WriteLog("RELOADED SETTINGS FOR INJECTOR APPLICATION CORRECTLY!", LogType.InfoLog);
        }
    }
}
