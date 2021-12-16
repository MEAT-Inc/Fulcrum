using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using FulcrumInjector.AppLogic.JsonConverters;
using Newtonsoft.Json;
using SharpLogger;
using SharpLogger.LoggerObjects;
using SharpLogger.LoggerSupport;

namespace FulcrumInjector.ViewControl.Models
{
    /// <summary>
    /// Types of possible controls we can use here.
    /// </summary>
    public enum ControlTypes
    {
        NOT_DEFINED,        // Bad enum type parse
        CHECKBOX_CONTROL,   // Checkbox
    }

    // --------------------------------------------------------------------------------------------------------------------------

    /// <summary>
    /// Model object for our settings entries
    /// </summary>
    [JsonConverter(typeof(SettingsEntryModelJsonConverter))]
    public class SettingsEntryModel
    {
        // Logger object.
        private static SubServiceLogger ModelLogger => (SubServiceLogger)LogBroker.LoggerQueue.GetLoggers(LoggerActions.SubServiceLogger)
            .FirstOrDefault(LoggerObj => LoggerObj.LoggerName.StartsWith("SettingsEntryModelLogger")) ?? new SubServiceLogger("SettingsEntryModelLogger");

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
        public SettingsEntryModel(string Name, object Value, ControlTypes ControlType, string Description = "No Description")
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
                    break;

                // Not Valid
                case ControlTypes.NOT_DEFINED:
                    ModelLogger.WriteLog($"FAILED TO BUILD NEW CONTROL INSTANCE FOR SETTING {SettingName}!", LogType.ErrorLog);
                    break;
            }

            // Log information about the built setting
            ModelLogger.WriteLog($"BUILT NEW SETTING OBJECT NAMED {this.SettingName} OK!");
            ModelLogger.WriteLog($"SETTING DESCRIPTION: {this.SettingDescription}", LogType.TraceLog);
        }
    }
}
