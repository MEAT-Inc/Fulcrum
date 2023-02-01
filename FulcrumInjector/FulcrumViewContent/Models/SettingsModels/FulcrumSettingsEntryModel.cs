using System;
using System.Linq;
using System.Windows.Forms;
using FulcrumInjector.FulcrumViewSupport.FulcrumJson.JsonConverters;
using Newtonsoft.Json;
using SharpLogger;
using SharpLogger.LoggerObjects;
using SharpLogger.LoggerSupport;

namespace FulcrumInjector.FulcrumViewContent.Models.SettingsModels
{
    /// <summary>
    /// Types of possible controls we can use here.
    /// </summary>
    public enum ControlTypes
    {
        NOT_DEFINED,        // Bad enum type parse
        CHECKBOX_CONTROL,   // Checkbox
        TEXTBOX_CONTROL,    // Textbox
        COMBOBOX_CONTROL,   // Combobox
    }

    // --------------------------------------------------------------------------------------------------------------------------

    /// <summary>
    /// Model object for our settings entries
    /// </summary>
    [JsonConverter(typeof(FulcrumSettingsCollectionJsonConverter))]
    public class FulcrumSettingsEntryModel
    {
        // Logger object.
        private static SubServiceLogger ModelLogger => (SubServiceLogger)LoggerQueue.SpawnLogger("SettingsEntryModelLogger", LoggerActions.SubServiceLogger);

        // Basic Setting configurations
        public string SettingName { get; set; }
        public object SettingValue { get; set; }
        public string SettingDescription { get; set; }

        // The type of control used to setup the settings entries
        public ControlTypes TypeOfControl { get; set; }
        public Type SettingControlType { get; set; }

        // --------------------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// Builds a new settings entry object.
        /// </summary>
        /// <param name="Name">Setting name</param>
        /// <param name="Value">Setting value</param>
        /// <param name="ControlType">Type of UI Control</param>
        /// <param name="Description">Description of setting</param>
        public FulcrumSettingsEntryModel(string Name, object Value, ControlTypes ControlType, string Description = "No Description")
        {
            // Store values for object onto class now.
            this.SettingName = Name;
            this.SettingValue = Value;
            this.SettingDescription = Description;

            // Set our control type
            this.TypeOfControl = ControlType;
            switch (this.TypeOfControl)
            {
                // Checkbox Control
                case ControlTypes.CHECKBOX_CONTROL: 
                    this.SettingControlType = typeof(CheckBox);
                    this.SettingValue = bool.Parse(Value.ToString());
                    break;

                // ComboBox
                case ControlTypes.COMBOBOX_CONTROL:
                    this.SettingControlType = typeof(ComboBox);
                    this.SettingValue = Value.ToString().Split(',');
                    break;

                // TextBox
                case ControlTypes.TEXTBOX_CONTROL:
                    this.SettingControlType = typeof(TextBox);
                    break;

                // Not Valid
                case ControlTypes.NOT_DEFINED:
                    this.SettingControlType = null;
                    ModelLogger.WriteLog($"FAILED TO BUILD NEW CONTROL INSTANCE FOR SETTING {SettingName}!", LogType.ErrorLog);
                    break;
            }

            // Log information about the built setting
            ModelLogger.WriteLog($"BUILT NEW SETTING OBJECT NAMED {this.SettingName} OK!");
            ModelLogger.WriteLog($"SETTING CONTROL TYPE IS {(this.SettingControlType == null ? "FAILED_BINDING" : this.SettingControlType.Name)}");
            ModelLogger.WriteLog($"SETTING DESCRIPTION: {this.SettingDescription}", LogType.TraceLog);
        }
    }
}
